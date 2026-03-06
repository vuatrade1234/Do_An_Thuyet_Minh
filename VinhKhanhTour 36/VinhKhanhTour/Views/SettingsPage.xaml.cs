using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views;

public partial class SettingsPage : ContentPage
{
    private readonly TtsService _tts = TtsService.Instance;

    public static double GlobalRadius { get; private set; } = 30;
    public static int RepeatCount { get; private set; } = 1;
    public static int CooldownSeconds { get; private set; } = 60;

    // Groups để đổi màu active
    private List<Button> _repeatBtns = new();
    private List<Button> _cooldownBtns = new();
    private List<Button> _radiusBtns = new();

    public SettingsPage()
    {
        InitializeComponent();

        // Khởi tạo groups
        _repeatBtns = new() { BtnR1, BtnR2, BtnR3, BtnR4, BtnR5 };
        _cooldownBtns = new() { BtnC10, BtnC30, BtnC60, BtnC120 };
        _radiusBtns = new() { BtnRad10, BtnRad30, BtnRad50 };
    }

    // ── Helper: set active button ─────────────────────────────────────
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

    // ── Volume ────────────────────────────────────────────────────────
    private void OnVolumeChanged(object sender, ValueChangedEventArgs e)
        => LblVolume.Text = $"{(int)e.NewValue}%";

    // ── Repeat ────────────────────────────────────────────────────────
    private void OnRepeat1Clicked(object s, EventArgs e) => SetRepeat(1, BtnR1);
    private void OnRepeat2Clicked(object s, EventArgs e) => SetRepeat(2, BtnR2);
    private void OnRepeat3Clicked(object s, EventArgs e) => SetRepeat(3, BtnR3);
    private void OnRepeat4Clicked(object s, EventArgs e) => SetRepeat(4, BtnR4);
    private void OnRepeat5Clicked(object s, EventArgs e) => SetRepeat(5, BtnR5);

    private void SetRepeat(int val, Button active)
    {
        RepeatCount = val;
        LblRepeat.Text = $"{val} lần";
        SetActive(_repeatBtns, active);
    }

    // ── Cooldown ──────────────────────────────────────────────────────
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

    // ── Radius ────────────────────────────────────────────────────────
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

    // ── TTS ───────────────────────────────────────────────────────────
    private async void OnTestTtsClicked(object sender, EventArgs e)
        => await _tts.SpeakAsync(
            "Xin chào! Chào mừng đến Phố Ẩm Thực Vĩnh Khánh!",
            "Hello! Welcome to Vinh Khanh Food Street!");

    private void OnViClicked(object sender, EventArgs e)
    {
        _tts.CurrentLanguage = AppLanguage.Vietnamese;
        SetActive(new List<Button> { BtnVi, BtnEn }, BtnVi);
        DisplayAlert("✅", "Đã chuyển sang Tiếng Việt", "OK");
    }

    private void OnEnClicked(object sender, EventArgs e)
    {
        _tts.CurrentLanguage = AppLanguage.English;
        SetActive(new List<Button> { BtnVi, BtnEn }, BtnEn);
        DisplayAlert("✅", "Switched to English", "OK");
    }
}