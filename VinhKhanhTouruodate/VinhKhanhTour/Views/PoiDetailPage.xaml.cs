using CommunityToolkit.Mvvm.Messaging;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views;

public partial class PoiDetailPage : ContentPage
{
    private readonly PoiModel _poi;
    private readonly TtsService _tts = TtsService.Instance;
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private bool _isPlaying = false;

    public PoiDetailPage(PoiModel poi)
    {
        InitializeComponent();
        _poi = poi;
        BindData();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _loc.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loc.LanguageChanged -= OnLanguageChanged;
        _ = _tts.StopAsync();
    }

    // ── Bind dữ liệu POI ──────────────────────────────────────────────

    private void BindData()
    {
        CoverImg.Source = _poi.CoverImage;
        LblRadius.Text = $"{_poi.RadiusMeters:F0}m";
        RefreshLocalizedText();
    }

    // ── Refresh text theo ngôn ngữ ────────────────────────────────────

    private void OnLanguageChanged()
        => MainThread.BeginInvokeOnMainThread(RefreshLocalizedText);

    private void RefreshLocalizedText()
    {
        LblTitle.Text = _loc.Get("detail_title");
        LblPoiName.Text = _poi.LocalizedName;
        LblCategoryEmoji.Text = _poi.CategoryEmoji;
        LblCategoryLabel.Text = _poi.CategoryLabel;
        LblDistanceTitle.Text = _loc.Get("detail_distance");
        LblRadiusTitle.Text = _loc.Get("detail_radius");
        LblIntroTitle.Text = _loc.Get("detail_intro");
        LblTtsTitle.Text = _loc.Get("detail_tts");
        LblDescription.Text = _poi.LocalizedDescription;
        LblTtsScript.Text = _poi.LocalizedTtsScript;
        LblDistance.Text = _poi.DistanceText;
        BtnListen.Text = _loc.Get("detail_listen");
        BtnBackMap.Text = _loc.Get("detail_back_map");
        BtnNavigateHeader.Text = _loc.Get("detail_navigate");
    }

    // ── Nghe thuyết minh ──────────────────────────────────────────────

    private async void OnListenClicked(object sender, EventArgs e)
    {
        if (_isPlaying)
        {
            await _tts.StopAsync();
            _isPlaying = false;
            BtnListen.Text = _loc.Get("detail_listen");
            BtnListen.BackgroundColor = Color.FromArgb("#E65100");
            return;
        }

        var script = _poi.LocalizedTtsScript;
        if (string.IsNullOrEmpty(script))
        {
            await DisplayAlertAsync("⚠️", "Chưa có nội dung thuyết minh", "OK");
            return;
        }

        _isPlaying = true;
        BtnListen.Text = "⏹ Dừng";
        BtnListen.BackgroundColor = Color.FromArgb("#B71C1C");

        await _tts.SpeakAsync(script);

        _isPlaying = false;
        BtnListen.Text = _loc.Get("detail_listen");
        BtnListen.BackgroundColor = Color.FromArgb("#E65100");
    }

    // ── Quay lại bản đồ ───────────────────────────────────────────────

    private async void OnBackMapClicked(object sender, EventArgs e)
    {
        await _tts.StopAsync();
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//MapPage");
        else
            await Navigation.PopAsync();
    }

    // ── Chỉ đường ─────────────────────────────────────────────────────

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        await _tts.StopAsync();
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//MapPage");
        await Task.Delay(300);
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default
            .Send(new ShowRouteMessage { Poi = _poi });
    }

    // ── Back ──────────────────────────────────────────────────────────

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await _tts.StopAsync();
        await Navigation.PopAsync();
    }
}