using System.Text.RegularExpressions;
using HtmlAgilityPack;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Server.Services;

public class RecipeImportService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RecipeImportService> _logger;

    public RecipeImportService(IHttpClientFactory httpClientFactory, ILogger<RecipeImportService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _logger = logger;
    }

    public async Task<ImportRecipeResponse> ImportFromUrlAsync(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLower();

            _logger.LogInformation("Starting import from {Host}: {Url}", host, url);

            // Only Chefkoch.de is supported
            if (!host.Contains("chefkoch.de"))
            {
                return new ImportRecipeResponse
                {
                    Success = false,
                    Message = "Only Chefkoch.de is currently supported. Please use a Chefkoch.de URL."
                };
            }

            var preview = await ImportFromChefkochAsync(url);

            if (preview == null || string.IsNullOrWhiteSpace(preview.Name))
            {
                _logger.LogWarning("Import failed: No recipe name found for {Url}", url);
                return new ImportRecipeResponse
                {
                    Success = false,
                    Message = "Recipe could not be imported. Please check the URL."
                };
            }

            _logger.LogInformation("Successfully imported recipe: {Name} with {Count} ingredients", preview.Name, preview.Ingredients.Count);

            var recipe = new CreateRecipeRequest
            {
                Name = preview.Name,
                Description = preview.Description,
                Instructions = preview.Instructions,
                PrepTimeMinutes = preview.PrepTimeMinutes,
                CookTimeMinutes = preview.CookTimeMinutes,
                Servings = preview.Servings,
                Category = preview.Category,
                Ingredients = preview.Ingredients
            };

            return new ImportRecipeResponse
            {
                Success = true,
                Message = $"Recipe successfully imported from {preview.Source}",
                Recipe = recipe
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing recipe from {Url}", url);
            return new ImportRecipeResponse
            {
                Success = false,
                Message = $"Import error: {ex.Message}"
            };
        }
    }

    private async Task<RecipeImportPreviewDto?> ImportFromChefkochAsync(string url)
    {
        var html = await _httpClient.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var recipe = new RecipeImportPreviewDto
        {
            SourceUrl = url,
            Source = "Chefkoch.de"
        };

        // === NAME ===
        var nameNode = doc.DocumentNode.SelectSingleNode("//h1[@class='ds-h2']");
        if (nameNode == null) nameNode = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'ds-h')]");
        if (nameNode == null) nameNode = doc.DocumentNode.SelectSingleNode("//h1");
        recipe.Name = nameNode?.InnerText.Trim() ?? "";

        // === DESCRIPTION ===
        var descNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
        var fullDescription = descNode?.GetAttributeValue("content", "") ?? "";

        // Clean description of Chefkoch-specific marketing text
        if (!string.IsNullOrEmpty(fullDescription))
        {
            // Remove everything after "Über X Bewertungen" or "Mit ►"
            var cleanedDesc = Regex.Replace(fullDescription, @"\s*Über\s+\d+\s+Bewertungen.*$", "", RegexOptions.IgnoreCase);
            cleanedDesc = Regex.Replace(cleanedDesc, @"\s*Mit\s*►.*$", "", RegexOptions.IgnoreCase);

            // Remove "Jetzt ausprobieren" etc.
            cleanedDesc = Regex.Replace(cleanedDesc, @"\s*Jetzt\s+ausprobieren.*$", "", RegexOptions.IgnoreCase);

            recipe.Description = cleanedDesc.Trim();
        }

        // === EXTRACT JSON-LD DATA (PRIMARY) ===
        var schemaNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (schemaNodes != null)
        {
            foreach (var schemaNode in schemaNodes)
            {
                try
                {
                    var json = schemaNode.InnerText;
                    if (json.Contains("\"@type\":\"Recipe\"") || json.Contains("\"@type\": \"Recipe\""))
                    {
                        // === INGREDIENTS FROM JSON ===
                        var ingredientsMatch = Regex.Match(json, @"""recipeIngredient""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);
                        if (ingredientsMatch.Success)
                        {
                            var ingredientsArray = ingredientsMatch.Groups[1].Value;
                            var matches = Regex.Matches(ingredientsArray, @"""([^""]+)""");

                            int sortOrder = 0;
                            foreach (Match match in matches)
                            {
                                var ingredientText = match.Groups[1].Value;
                                var (amount, unit, name) = ParseIngredientLine(ingredientText);

                                recipe.Ingredients.Add(new CreateRecipeIngredientRequest
                                {
                                    Name = name,
                                    Amount = amount,
                                    Unit = unit,
                                    SortOrder = sortOrder++
                                });
                            }
                        }

                        // === INSTRUCTIONS FROM JSON ===
                        // Variant 1: Simple string
                        var instructionsMatch = Regex.Match(json, @"""recipeInstructions""\s*:\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Singleline);
                        if (instructionsMatch.Success)
                        {
                            var rawInstructions = instructionsMatch.Groups[1].Value;
                            // Unescape JSON string (\\n -> \n, \\" -> ", etc.)
                            rawInstructions = Regex.Unescape(rawInstructions);
                            recipe.Instructions = System.Net.WebUtility.HtmlDecode(rawInstructions);
                        }

                        // Variant 2: Array of HowToStep objects
                        if (string.IsNullOrWhiteSpace(recipe.Instructions))
                        {
                            var instructionsArrayMatch = Regex.Match(json, @"""recipeInstructions""\s*:\s*\[(.*?)\](?=\s*,\s*""|\s*\})", RegexOptions.Singleline);
                            if (instructionsArrayMatch.Success)
                            {
                                var instructionsArray = instructionsArrayMatch.Groups[1].Value;
                                var steps = new List<string>();

                                // Suche nach "text" Feldern in den Steps
                                var stepMatches = Regex.Matches(instructionsArray, @"""text""\s*:\s*""((?:[^""\\]|\\.)*?)""");

                                int stepNumber = 1;
                                foreach (Match stepMatch in stepMatches)
                                {
                                    var stepText = Regex.Unescape(stepMatch.Groups[1].Value);
                                    stepText = System.Net.WebUtility.HtmlDecode(stepText);

                                    if (!string.IsNullOrWhiteSpace(stepText))
                                    {
                                        steps.Add($"{stepNumber}. {stepText}");
                                        stepNumber++;
                                    }
                                }

                                if (steps.Count > 0)
                                {
                                    recipe.Instructions = string.Join("\n\n", steps);
                                }
                            }
                        }

                        // === TIMES FROM JSON ===
                        // Chefkoch.de often uses totalTime instead of separate prep/cook times
                        // Try totalTime first
                        var totalTimeMatch = Regex.Match(json, @"""totalTime""\s*:\s*""PT(?:(\d+)H)?(?:(\d+)M)?""");
                        if (totalTimeMatch.Success)
                        {
                            int hours = totalTimeMatch.Groups[1].Success ? int.Parse(totalTimeMatch.Groups[1].Value) : 0;
                            int mins = totalTimeMatch.Groups[2].Success ? int.Parse(totalTimeMatch.Groups[2].Value) : 0;
                            int totalMinutes = hours * 60 + mins;

                            if (totalMinutes > 0)
                            {
                                // For Chefkoch.de, totalTime is often the active preparation time
                                // Set it as PrepTime since it represents the active time
                                if (!recipe.PrepTimeMinutes.HasValue)
                                {
                                    recipe.PrepTimeMinutes = totalMinutes;
                                }
                                if (!recipe.CookTimeMinutes.HasValue)
                                {
                                    recipe.CookTimeMinutes = 0; // Chefkoch often has no separate cook time
                                }
                            }
                        }

                        // Then specific prepTime
                        if (!recipe.PrepTimeMinutes.HasValue)
                        {
                            var prepMatch = Regex.Match(json, @"""prepTime""\s*:\s*""PT(?:(\d+)H)?(?:(\d+)M)?""");
                            if (prepMatch.Success)
                            {
                                int hours = prepMatch.Groups[1].Success ? int.Parse(prepMatch.Groups[1].Value) : 0;
                                int mins = prepMatch.Groups[2].Success ? int.Parse(prepMatch.Groups[2].Value) : 0;
                                int prepMinutes = hours * 60 + mins;
                                if (prepMinutes > 0)
                                {
                                    recipe.PrepTimeMinutes = prepMinutes;
                                }
                            }
                        }

                        // Then cookTime
                        if (!recipe.CookTimeMinutes.HasValue)
                        {
                            var cookMatch = Regex.Match(json, @"""cookTime""\s*:\s*""PT(?:(\d+)H)?(?:(\d+)M)?""");
                            if (cookMatch.Success)
                            {
                                int hours = cookMatch.Groups[1].Success ? int.Parse(cookMatch.Groups[1].Value) : 0;
                                int mins = cookMatch.Groups[2].Success ? int.Parse(cookMatch.Groups[2].Value) : 0;
                                int cookMinutes = hours * 60 + mins;
                                if (cookMinutes > 0)
                                {
                                    recipe.CookTimeMinutes = cookMinutes;
                                }
                            }
                        }

                        // Fallback: if only prepTime is available, CookTime remains empty -
                        // Chefkoch often only provides totalTime/prepTime.

                        // === SERVINGS FROM JSON ===
                        if (!recipe.Servings.HasValue)
                        {
                            var yieldMatch = Regex.Match(json, @"""recipeYield""\s*:\s*""?(\d+)""?");
                            if (yieldMatch.Success)
                            {
                                recipe.Servings = int.Parse(yieldMatch.Groups[1].Value);
                            }
                        }

                        // === CATEGORY FROM JSON ===
                        if (string.IsNullOrEmpty(recipe.Category))
                        {
                            var categoryMatch = Regex.Match(json, @"""recipeCategory""\s*:\s*""([^""]+)""");
                            if (categoryMatch.Success)
                            {
                                recipe.Category = categoryMatch.Groups[1].Value;
                            }
                        }

                        break; // We found the data
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing JSON-LD for recipe");
                }
            }
        }

        // === FALLBACK: SERVINGS FROM HTML ===
        if (!recipe.Servings.HasValue)
        {
            var servingsNode = doc.DocumentNode.SelectSingleNode("//input[@name='servingCount']");
            if (servingsNode == null)
                servingsNode = doc.DocumentNode.SelectSingleNode("//input[@id='servingCount']");

            if (servingsNode != null)
            {
                var servingsStr = servingsNode.GetAttributeValue("value", "");
                if (int.TryParse(servingsStr, out var servings))
                    recipe.Servings = servings;
            }
        }

        // === FALLBACK: INSTRUCTIONS FROM HTML (if JSON was empty) ===
        if (string.IsNullOrWhiteSpace(recipe.Instructions))
        {
            _logger.LogWarning("Instructions not found in JSON, trying HTML selectors");

            // Try different HTML selectors
            var instructionsNode = doc.DocumentNode.SelectSingleNode("//div[@class='ds-recipe-instructions']");
            if (instructionsNode == null)
                instructionsNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'instructions')]");
            if (instructionsNode == null)
                instructionsNode = doc.DocumentNode.SelectSingleNode("//div[@id='rezept-zubereitung']");
            if (instructionsNode == null)
                instructionsNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'preparation')]");

            if (instructionsNode != null)
            {
                recipe.Instructions = CleanHtmlText(instructionsNode.InnerText);
            }
        }

        // === FALLBACK: CATEGORY FROM HTML ===
        if (string.IsNullOrEmpty(recipe.Category))
        {
            var categoryNode = doc.DocumentNode.SelectSingleNode("//span[@class='ds-recipe-meta__category']");
            if (categoryNode == null)
                categoryNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'category')]");
            recipe.Category = categoryNode?.InnerText.Trim();
        }

        // === IMAGE URL ===
        var imageNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
        if (imageNode == null)
            imageNode = doc.DocumentNode.SelectSingleNode("//img[@class='ds-photo-wrapper__img']");
        if (imageNode == null)
            imageNode = doc.DocumentNode.SelectSingleNode("//img[contains(@class, 'recipe-image')]");

        if (imageNode != null)
        {
            recipe.ImageUrl = imageNode.GetAttributeValue("content", "") ??
                  imageNode.GetAttributeValue("src", "");
        }

        return recipe;
    }

    private (decimal? amount, string? unit, string name) ParseIngredientLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return (null, null, line);

        // Pattern: "250 ml Milch" or "2 EL Butter" or "1,5 kg Mehl"
        var match = Regex.Match(line, @"^([\d,\.]+)\s*([a-zA-ZäöüÄÖÜß]+)?\s+(.+)");
        if (match.Success)
        {
            var amountStr = match.Groups[1].Value.Replace(",", ".");
            if (decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any,
             System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                var unit = match.Groups[2].Value;
                var name = match.Groups[3].Value.Trim();
                return (amount, string.IsNullOrWhiteSpace(unit) ? null : unit, name);
            }
        }

        // If no pattern matches, use the entire line as the name
        return (null, null, line.Trim());
    }

    private string CleanHtmlText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        // Remove HTML tags
        html = Regex.Replace(html, @"<[^>]+>", " ");

        // Decode HTML entities
        html = System.Net.WebUtility.HtmlDecode(html);

        // Clean up whitespace but preserve line breaks
        html = Regex.Replace(html, @"[ \t]+", " ");
        html = Regex.Replace(html, @"\r\n|\r|\n", "\n");
        html = Regex.Replace(html, @"\n{3,}", "\n\n");

        return html.Trim();
    }
}
