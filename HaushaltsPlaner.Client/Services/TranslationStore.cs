using System.Net.Http.Json;

namespace HaushaltsPlaner.Client.Services;

/// <summary>
/// Singleton that loads all i18n/*.json files once at startup from wwwroot/i18n/.
/// Adding a new language only requires dropping a new JSON file — no C# changes needed.
/// </summary>
public sealed class TranslationStore
{
    private Dictionary<string, Dictionary<string, string>> _translations = new();

    public async Task LoadAsync(HttpClient http)
    {
        var languages = new[] { "en", "de" };

        foreach (var lang in languages)
        {
            try
            {
                var dict = await http.GetFromJsonAsync<Dictionary<string, string>>($"i18n/{lang}.json");
                if (dict is not null)
                {
                    _translations[lang] = dict;
                    Console.WriteLine($"[i18n] Loaded '{lang}': {dict.Count} keys from {http.BaseAddress}i18n/{lang}.json");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[i18n] Failed to load '{lang}': {ex.Message} (BaseAddress: {http.BaseAddress})");
            }
        }
    }

    public bool SupportsLanguage(string lang) => _translations.ContainsKey(lang);

    public string Get(string lang, string key)
    {
        if (_translations.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
            return value;
        if (_translations.TryGetValue("en", out dict) && dict.TryGetValue(key, out value))
            return value;
        return key;
    }
}
