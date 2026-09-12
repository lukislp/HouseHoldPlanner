using System.Globalization;
using FsCheck;
using FsCheck.Xunit;
using HaushaltsPlaner.Server.Services;

namespace HaushaltsPlaner.Tests;

/// <summary>
/// Property-based tests (FsCheck) for the two pure text parsers behind the recipe import:
/// the ingredient-line splitter and the HTML-to-text cleaner. Both are fed scraped, untrusted
/// text, so each invariant runs against hundreds of generated lines rather than a few examples.
/// </summary>
public class RecipeImportParsingPropertyTests
{
    private static readonly string[] Units = ["g", "kg", "ml", "l", "EL", "TL", "Stück", "Prise", "Bund"];

    [Property(MaxTest = 500)]
    public bool Parsing_an_ingredient_line_never_throws_and_a_non_blank_line_always_keeps_a_name(string? line)
    {
        var (_, _, name) = RecipeImportService.ParseIngredientLine(line!);
        if (string.IsNullOrWhiteSpace(line))
            return true;
        return !string.IsNullOrWhiteSpace(name) && name == name.Trim();
    }

    // A corner case the generator practically never lands on by itself, pinned as an example.
    [Theory]
    [InlineData("1  ")]
    [InlineData("1,5   ")]
    public void A_line_that_is_only_a_quantity_keeps_the_line_as_its_name(string line)
    {
        var (_, _, name) = RecipeImportService.ParseIngredientLine(line);
        Assert.Equal(line.Trim(), name);
    }

    [Property(MaxTest = 500)]
    public bool A_quantity_unit_name_line_is_split_back_into_exactly_those_parts(
        PositiveInt whole, byte hundredths, byte unitSeed, NonEmptyString rawName, bool germanDecimal)
    {
        var name = rawName.Get.Trim();
        if (name.Length == 0 || char.IsDigit(name[0]) || name.Any(c => c is '\n' or '\r'))
            return true; // a name that starts with a digit is a second quantity, not a name

        var amount = whole.Get + (hundredths % 100 / 100m);
        var amountText = amount.ToString("0.##", CultureInfo.InvariantCulture);
        if (germanDecimal)
            amountText = amountText.Replace('.', ',');
        var unit = Units[unitSeed % Units.Length];

        var (parsedAmount, parsedUnit, parsedName) = RecipeImportService.ParseIngredientLine($"{amountText} {unit} {name}");

        return parsedAmount == amount && parsedUnit == unit && parsedName == name;
    }

    [Property(MaxTest = 500)]
    public bool Cleaned_html_is_trimmed_and_never_contains_tabs_carriage_returns_space_runs_or_triple_newlines(string? html)
    {
        var cleaned = RecipeImportService.CleanHtmlText(html!);
        return cleaned == cleaned.Trim()
            && !cleaned.Contains('\t')
            && !cleaned.Contains('\r')
            && !cleaned.Contains("  ")
            && !cleaned.Contains("\n\n\n");
    }

    [Property(MaxTest = 300)]
    public bool Markup_free_text_only_loses_whitespace_when_cleaned(NonNull<string> text)
    {
        var raw = text.Get;
        if (raw.IndexOfAny(['<', '>', '&']) >= 0)
            return true;

        var cleaned = RecipeImportService.CleanHtmlText(raw);

        var lettersIn = new string(raw.Where(c => !char.IsWhiteSpace(c)).ToArray());
        var lettersOut = new string(cleaned.Where(c => !char.IsWhiteSpace(c)).ToArray());
        return lettersIn == lettersOut;
    }
}
