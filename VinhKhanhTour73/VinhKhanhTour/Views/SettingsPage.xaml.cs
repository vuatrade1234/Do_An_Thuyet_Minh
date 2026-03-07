using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views;

public partial class SettingsPage : ContentPage
{
    private readonly TtsService _tts = TtsService.Instance;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    // ── Global Settings ───────────────────────────────────────────────────────
    public static double GlobalRadius { get; private set; } = 30;
    public static int RepeatCount { get; private set; } = 1;
    public static int CooldownSeconds { get; private set; } = 10;
    public static float SpeechRate { get; private set; } = 1.0f;

    private List<Button> _repeatBtns = new();
    private List<Button> _cooldownBtns = new();
    private List<Button> _radiusBtns = new();
    private List<Button> _rateBtns = new();
    private List<Button> _langBtns = new();

    // Màu nền mỗi ngôn ngữ (giống MapPage)
    private static readonly Dictionary<AppLocale, string> _langColors = new()
    {
        [AppLocale.Vietnamese] = "#DA251D",
        [AppLocale.English] = "#012169",
        [AppLocale.Chinese] = "#DE2910",
        [AppLocale.Japanese] = "#BC002D",
        [AppLocale.Korean] = "#003478",
    };

    public SettingsPage()
    {
        InitializeComponent();

        _repeatBtns = new() { BtnR1, BtnR2, BtnR3, BtnR4, BtnR5 };
        _cooldownBtns = new() { BtnC3, BtnC10, BtnC30, BtnC60, BtnC120 };
        _radiusBtns = new() { BtnRad10, BtnRad30, BtnRad50 };
        _rateBtns = new() { BtnSlow, BtnNormal, BtnFast, BtnVeryFast };
        _langBtns = new() { BtnVi, BtnEn, BtnZh, BtnJa, BtnKo };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshLocalizedText();
        HighlightCurrentLanguage();
        _loc.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loc.LanguageChanged -= OnLanguageChanged;
    }

    // ── Localization ──────────────────────────────────────────────────────────

    private void OnLanguageChanged()
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshLocalizedText();
            HighlightCurrentLanguage();
        });

    private void RefreshLocalizedText()
    {
        LblPageTitle.Text = _loc.Get("settings_title");

        LblTtsSection.Text = _loc.Get("settings_tts");
        LblAutoSpeak.Text = _loc.Get("settings_auto_speak");
        LblVolumeTitle.Text = _loc.Get("settings_volume");
        BtnTestTts.Text = _loc.Get("settings_test_tts");

        LblSpeedSection.Text = _loc.Get("settings_speed");
        LblSpeedTitle.Text = _loc.Get("settings_speed_label");

        LblRepeatSection.Text = _loc.Get("settings_repeat");
        LblRepeatTitle.Text = _loc.Get("settings_repeat_label");

        LblCooldownSection.Text = _loc.Get("settings_cooldown");
        LblCooldownTitle.Text = _loc.Get("settings_cooldown_label");

        LblRadiusSection.Text = _loc.Get("settings_radius");
        LblRadiusTitle.Text = _loc.Get("settings_radius_label");
        LblHint.Text = _loc.Get("settings_hint");

        LblLangSection.Text = _loc.Get("settings_language");

        // Refresh value labels theo ngôn ngữ
        LblRepeat.Text = FormatRepeat(RepeatCount);
        LblCooldown.Text = $"{CooldownSeconds}s";
    }

    private string FormatRepeat(int n) => _loc.CurrentLocale switch
    {
        AppLocale.Vietnamese => $"{n} lần",
        AppLocale.English => $"{n} time{(n > 1 ? "s" : "")}",
        AppLocale.Chinese => $"{n} 次",
        AppLocale.Japanese => $"{n} 回",
        AppLocale.Korean => $"{n} 번",
        _ => $"{n}x"
    };

    private void HighlightCurrentLanguage()
    {
        var locale = _loc.CurrentLocale;

        // Reset tất cả về xám
        foreach (var b in _langBtns)
        {
            b.BackgroundColor = Color.FromArgb("#E8EAF6");
            b.TextColor = Color.FromArgb("#1A237E");
        }

        // Highlight nút đang chọn = màu cờ
        var (active, color) = locale switch
        {
            AppLocale.Vietnamese => (BtnVi, "#DA251D"),
            AppLocale.English => (BtnEn, "#012169"),
            AppLocale.Chinese => (BtnZh, "#DE2910"),
            AppLocale.Japanese => (BtnJa, "#BC002D"),
            AppLocale.Korean => (BtnKo, "#003478"),
            _ => (BtnVi, "#DA251D"),
        };
        active.BackgroundColor = Color.FromArgb(color);
        active.TextColor = Colors.White;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetActive(List<Button> group, Button active)
    {
        foreach (var b in group)
        {
            b.BackgroundColor = Color.FromArgb("#E8EAF6");
            b.TextColor = Color.FromArgb("#1A237E");
        }
        active.BackgroundColor = Color.FromArgb("#1A237E");
        active.TextColor = Colors.White;
    }

    // ── Volume ────────────────────────────────────────────────────────────────

    private void OnVolumeChanged(object sender, ValueChangedEventArgs e)
        => LblVolume.Text = $"{(int)e.NewValue}%";

    // ── Speech Rate ───────────────────────────────────────────────────────────

    private void OnSlowClicked(object s, EventArgs e) => SetSpeechRate(0.6f, "🐢", BtnSlow);
    private void OnNormalClicked(object s, EventArgs e) => SetSpeechRate(1.0f, "🚶", BtnNormal);
    private void OnFastClicked(object s, EventArgs e) => SetSpeechRate(1.4f, "🚴", BtnFast);
    private void OnVeryFastClicked(object s, EventArgs e) => SetSpeechRate(1.8f, "🚀", BtnVeryFast);

    private void SetSpeechRate(float rate, string emoji, Button active)
    {
        SpeechRate = rate;
        LblSpeechRate.Text = $"{emoji} {active.Text.Split(' ').LastOrDefault()}";
        SetActive(_rateBtns, active);
    }

    // ── Repeat ────────────────────────────────────────────────────────────────

    private void OnRepeat1Clicked(object s, EventArgs e) => SetRepeat(1, BtnR1);
    private void OnRepeat2Clicked(object s, EventArgs e) => SetRepeat(2, BtnR2);
    private void OnRepeat3Clicked(object s, EventArgs e) => SetRepeat(3, BtnR3);
    private void OnRepeat4Clicked(object s, EventArgs e) => SetRepeat(4, BtnR4);
    private void OnRepeat5Clicked(object s, EventArgs e) => SetRepeat(5, BtnR5);

    private void SetRepeat(int val, Button active)
    {
        RepeatCount = val;
        LblRepeat.Text = FormatRepeat(val);
        SetActive(_repeatBtns, active);
    }

    // ── Cooldown (thêm 3s) ────────────────────────────────────────────────────

    private void OnCooldown3Clicked(object s, EventArgs e) => SetCooldown(3, BtnC3);
    private void OnCooldown10Clicked(object s, EventArgs e) => SetCooldown(10, BtnC10);
    private void OnCooldown30Clicked(object s, EventArgs e) => SetCooldown(30, BtnC30);
    private void OnCooldown60Clicked(object s, EventArgs e) => SetCooldown(60, BtnC60);
    private void OnCooldown120Clicked(object s, EventArgs e) => SetCooldown(120, BtnC120);

    private void SetCooldown(int val, Button active)
    {
        CooldownSeconds = val;
        LblCooldown.Text = $"{val}s";
        SetActive(_cooldownBtns, active);
    }

    // ── Radius ────────────────────────────────────────────────────────────────

    private void OnRadiusChanged(object sender, ValueChangedEventArgs e)
    {
        GlobalRadius = e.NewValue;
        LblRadius.Text = $"{(int)e.NewValue}m";
    }

    private void OnRadius10Clicked(object s, EventArgs e) => SetRadius(10, BtnRad10);
    private void OnRadius30Clicked(object s, EventArgs e) => SetRadius(30, BtnRad30);
    private void OnRadius50Clicked(object s, EventArgs e) => SetRadius(50, BtnRad50);

    private void SetRadius(double val, Button active)
    {
        RadiusSlider.Value = val;
        GlobalRadius = val;
        LblRadius.Text = $"{val}m";
        SetActive(_radiusBtns, active);
    }

    // ── TTS Test ──────────────────────────────────────────────────────────────

    private async void OnTestTtsClicked(object sender, EventArgs e)
        => await _tts.SpeakAsync(
            "Xin chào! Chào mừng đến Phố Ẩm Thực Vĩnh Khánh!",
            "Hello! Welcome to Vinh Khanh Food Street!",
            "欢迎来到永庆美食街！",
            "ヴィンカインフードストリートへようこそ！",
            "빈칸 음식 거리에 오신 것을 환영합니다!");

    // ── Language Buttons ──────────────────────────────────────────────────────

    private void OnViClicked(object sender, EventArgs e) => SwitchLanguage(AppLocale.Vietnamese);
    private void OnEnClicked(object sender, EventArgs e) => SwitchLanguage(AppLocale.English);
    private void OnZhClicked(object sender, EventArgs e) => SwitchLanguage(AppLocale.Chinese);
    private void OnJaClicked(object sender, EventArgs e) => SwitchLanguage(AppLocale.Japanese);
    private void OnKoClicked(object sender, EventArgs e) => SwitchLanguage(AppLocale.Korean);

    private void SwitchLanguage(AppLocale locale)
    {
        _loc.SetLocale(locale);   // LanguageChanged event tự kích → RefreshLocalizedText + HighlightCurrentLanguage
    }
}
