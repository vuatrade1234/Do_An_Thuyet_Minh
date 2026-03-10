namespace VinhKhanhTour.Services;

public enum AppLanguage { Vietnamese, English }

public class TtsService
{
    public static readonly TtsService Instance = new();

    private AppLanguage _currentLanguage = AppLanguage.Vietnamese;
    private bool _isSpeaking;
    private CancellationTokenSource? _speechCts;

    public AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set => _currentLanguage = value;
    }

    public bool IsSpeaking => _isSpeaking;
    public event Action? SpeechCompleted;
    public event Action? SpeechStarted;

    public async Task SpeakAsync(string textVi, string textEn = "")
    {
        if (_isSpeaking)
        {
            _speechCts?.Cancel();
            _isSpeaking = false;
        }

        var text = _currentLanguage == AppLanguage.Vietnamese
            ? textVi
            : (string.IsNullOrEmpty(textEn) ? textVi : textEn);

        if (string.IsNullOrEmpty(text)) return;

        var locale = await GetLocaleAsync();
        var options = new SpeechOptions { Locale = locale, Volume = 1.0f, Pitch = 1.0f };

        _speechCts = new CancellationTokenSource();
        _isSpeaking = true;
        SpeechStarted?.Invoke();

        try
        {
            await TextToSpeech.Default.SpeakAsync(text, options, _speechCts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isSpeaking = false;
            SpeechCompleted?.Invoke();
        }
    }

    public Task StopAsync()
    {
        _speechCts?.Cancel();
        _isSpeaking = false;
        return Task.CompletedTask;
    }

    private async Task<Locale?> GetLocaleAsync()
    {
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        if (_currentLanguage == AppLanguage.Vietnamese)
            return locales.FirstOrDefault(l =>
                l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault();
        else
            return locales.FirstOrDefault(l =>
                l.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase) && l.Country == "US")
                ?? locales.FirstOrDefault(l =>
                    l.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault();
    }
}