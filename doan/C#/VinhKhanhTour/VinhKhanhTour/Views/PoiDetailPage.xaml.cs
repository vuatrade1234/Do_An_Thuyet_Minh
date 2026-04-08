using CommunityToolkit.Mvvm.Messaging;
using Plugin.Maui.Audio;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views;

public partial class PoiDetailPage : ContentPage
{
    private readonly PoiModel _poi;
    private readonly TtsService _tts = TtsService.Instance;
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly ApiSyncService _apiSync = new();
    private bool _isPlaying = false;
    private IAudioPlayer? _detailPlayer;
    private static readonly HttpClient _httpClient = new();

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
        StopAudioAndTts();
    }

    private void StopAudioAndTts()
    {
        _ = _tts.StopAsync();
        if (_detailPlayer != null)
        {
            _detailPlayer.Stop();
            _detailPlayer.Dispose();
            _detailPlayer = null;
        }
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
            StopAudioAndTts();
            _isPlaying = false;
            BtnListen.Text = _loc.Get("detail_listen");
            BtnListen.BackgroundColor = Color.FromArgb("#E65100");
            return;
        }

        var audioUrl = _poi.LocalizedAudioUrl;
        var script = _poi.LocalizedTtsScript;
        
        if (string.IsNullOrEmpty(audioUrl) && string.IsNullOrEmpty(script))
        {
            await DisplayAlertAsync("⚠️", "Chưa có nội dung thuyết minh / Audio", "OK");
            return;
        }

        _isPlaying = true;
        BtnListen.Text = "⏹ Dừng";
        BtnListen.BackgroundColor = Color.FromArgb("#B71C1C");

        var startTime = DateTime.Now;

        try
        {
            if (!string.IsNullOrEmpty(audioUrl))
            {
                // Ưu tiên Audio File
                var bytes = await _httpClient.GetByteArrayAsync(audioUrl);
                if (bytes != null && bytes.Length > 0 && _isPlaying)
                {
                    var stream = new MemoryStream(bytes);
                    _detailPlayer = AudioManager.Current.CreatePlayer(stream);
                    _detailPlayer.Play();

                    // Chờ đến khi hết
                    while (_detailPlayer != null && _detailPlayer.IsPlaying)
                    {
                        await Task.Delay(200);
                        if (!_isPlaying) break; // Bị bấm dừng
                    }
                }
                else if (_isPlaying && !string.IsNullOrEmpty(script)) // Rỗng -> Fallback
                {
                    await _tts.SpeakAsync(script);
                }
            }
            else if (!string.IsNullOrEmpty(script)) // Chỉ có TTS
            {
                await _tts.SpeakAsync(script);
            }
        }
        catch (Exception)
        {
            // Lỗi mạng hoặc lỗi player -> fallback TTS
            if (_isPlaying && !string.IsNullOrEmpty(script))
                await _tts.SpeakAsync(script);
        }

        var duration = (int)(DateTime.Now - startTime).TotalSeconds;

        _isPlaying = false;
        BtnListen.Text = _loc.Get("detail_listen");
        BtnListen.BackgroundColor = Color.FromArgb("#E65100");

        // Ghi Analytics sự kiện nghe thủ công
        var lang = _loc.CurrentLocale switch
        {
            AppLocale.English => "en",
            AppLocale.Chinese => "zh",
            AppLocale.Japanese => "ja",
            AppLocale.Korean => "ko",
            _ => "vi"
        };
        _ = _apiSync.LogPoiPlayAsync(_poi.Id, _poi.Name, lang, duration);
    }

    // ── Quay lại bản đồ ───────────────────────────────────────────────

    private async void OnBackMapClicked(object sender, EventArgs e)
    {
        StopAudioAndTts();
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//MapPage");
        else
            await Navigation.PopAsync();
    }

    // ── Chỉ đường ─────────────────────────────────────────────────────

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        StopAudioAndTts();
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//MapPage");
        await Task.Delay(300);
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default
            .Send(new ShowRouteMessage { Poi = _poi });
    }

    // ── Back ──────────────────────────────────────────────────────────

    private async void OnBackClicked(object sender, EventArgs e)
    {
        StopAudioAndTts();
        await Navigation.PopAsync();
    }
}