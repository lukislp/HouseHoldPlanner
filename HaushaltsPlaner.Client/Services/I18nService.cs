namespace HaushaltsPlaner.Client.Services;

/// <summary>
/// Scoped i18n service. Language detection happens via navigator.language (JS interop)
/// in LanguageProvider.razor after the WASM runtime is ready.
/// Translations are loaded from wwwroot/i18n/*.json at startup by the singleton
/// TranslationStore — add a new language by dropping a new JSON file.
/// </summary>
public sealed class I18nService
{
    private readonly TranslationStore _store;

    public I18nService(TranslationStore store)
    {
        _store = store;
    }

    /// <summary>Fired after the active language changes so components can call StateHasChanged().</summary>
    public event Action? OnLanguageChanged;

    public string Language { get; private set; } = "en";

    /// <summary>
    /// Called from LanguageProvider after JS interop delivers navigator.language.
    /// Resolves to the best supported language and fires OnLanguageChanged.
    /// </summary>
    public void SetLanguageFromBrowser(string? browserLanguage)
    {
        if (string.IsNullOrWhiteSpace(browserLanguage))
            return;

        var primary = browserLanguage.Split('-')[0].ToLowerInvariant();
        var resolved = _store.SupportsLanguage(primary) ? primary : "en";

        if (resolved == Language)
            return;

        Language = resolved;
        OnLanguageChanged?.Invoke();
    }

    /// <summary>Returns the translated string for the given key, with English fallback.</summary>
    public string Get(string key) => _store.Get(Language, key);

    /// <summary>Formats a translated string with string.Format arguments.</summary>
    public string Format(string key, params object[] args)
        => string.Format(Get(key), args);
}
