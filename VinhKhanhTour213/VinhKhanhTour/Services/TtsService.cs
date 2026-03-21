namespace VinhKhanhTour.Services;

public enum AppLanguage { Vietnamese, English, Chinese, Japanese, Korean }

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

    public async Task SpeakAsync(string textVi, string textEn = "",
                                  string textZh = "", string textJa = "",
                                  string textKo = "")
    {
        if (_isSpeaking)
        {
            _speechCts?.Cancel();
            _isSpeaking = false;
        }

        var text = _currentLanguage switch
        {
            AppLanguage.Vietnamese => textVi,
            AppLanguage.English => string.IsNullOrEmpty(textEn) ? textVi : textEn,
            AppLanguage.Chinese => string.IsNullOrEmpty(textZh) ? textVi : textZh,
            AppLanguage.Japanese => string.IsNullOrEmpty(textJa) ? textVi : textJa,
            AppLanguage.Korean => string.IsNullOrEmpty(textKo) ? textVi : textKo,
            _ => textVi
        };

        if (string.IsNullOrEmpty(text)) return;

        var locale = await GetLocaleAsync();
        var options = new SpeechOptions
        {
            Locale = locale,
            Volume = 1.0f,
            Pitch = Views.SettingsPage.SpeechRate
        };

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

    // Thêm method mới này vào TtsService
 
    private async Task<Locale?> GetLocaleAsync()
    {
        var locales = await TextToSpeech.Default.GetLocalesAsync();
       
        return _currentLanguage switch
        {
            AppLanguage.Vietnamese =>
                locales.FirstOrDefault(l => l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault(),

            AppLanguage.English =>
                locales.FirstOrDefault(l => l.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase) && l.Country == "US")
                ?? locales.FirstOrDefault(l => l.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault(),

            AppLanguage.Chinese =>
                locales.FirstOrDefault(l => l.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) && l.Country == "CN")
                ?? locales.FirstOrDefault(l => l.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault(),

            AppLanguage.Japanese =>
                locales.FirstOrDefault(l => l.Language.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault(),

            AppLanguage.Korean =>
                locales.FirstOrDefault(l => l.Language.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
                ?? locales.FirstOrDefault(),

            _ => locales.FirstOrDefault()
        };
    }
}