using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views;

public partial class PoiDetailPage : ContentPage
{
    private readonly PoiModel _poi;
    private readonly TtsService _tts = TtsService.Instance;

    public PoiDetailPage(PoiModel poi)
    {
        InitializeComponent();
        _poi = poi;
        LoadData();
    }

    private void LoadData()
    {
        PoiImage.Source = _poi.Category switch
        {
            "food" => "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=800",
            "drink" => "https://images.unsplash.com/photo-1544145945-f90425340c7e?w=800",
            "landmark" => "https://images.unsplash.com/photo-1528360983277-13d401cdc186?w=800",
            _ => "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=800"
        };
        LblName.Text = _poi.Name;
        LblCategory.Text = _poi.Category == "food" ? "🍜 Ẩm thực"
                            : _poi.Category == "drink" ? "🥤 Đồ uống"
                            : "🏛 Điểm tham quan";
        LblDistance.Text = _poi.DistanceFromUser.HasValue
                            ? $"{_poi.DistanceFromUser:F0}m" : "--m";
        LblRadius.Text = $"{_poi.RadiusMeters:F0}m";
        LblDescription.Text = _poi.Description;
        LblScript.Text = _tts.CurrentLanguage == AppLanguage.Vietnamese
                            ? _poi.TtsScript : _poi.TtsScriptEn;
    }

    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.Text = "⏳ Đang phát...";
        btn.IsEnabled = false;
        await _tts.SpeakAsync(_poi.TtsScript, _poi.TtsScriptEn);
        btn.Text = "🔊 Nghe thuyết minh";
        btn.IsEnabled = true;
    }

    private async void OnMapClicked(object sender, EventArgs e)
        => await Navigation.PopAsync();
}