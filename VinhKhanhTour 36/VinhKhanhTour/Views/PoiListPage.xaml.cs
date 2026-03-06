using VinhKhanhTour.Models;
using VinhKhanhTour.Services;
using VinhKhanhTour.Data;
using VinhKhanhTour.ViewModels;

namespace VinhKhanhTour.Views;

public partial class PoiListPage : ContentPage
{
    private List<PoiModel> _allPoi;
    private MapViewModel _mapVm;

    public PoiListPage()
    {
        InitializeComponent();

        _allPoi = PoiData.GetAllPoi();
        PoiList.ItemsSource = _allPoi;

        // Dùng chung MapViewModel (singleton đơn giản)
        _mapVm = new MapViewModel();
    }

    // ── Tìm kiếm ─────────────────────────────────────────────────────

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = e.NewTextValue?.Trim().ToLower() ?? "";

        if (string.IsNullOrEmpty(keyword))
        {
            SuggestionPanel.IsVisible = false;
            ClearBtn.IsVisible = false;
            PoiList.ItemsSource = _allPoi;
            return;
        }

        ClearBtn.IsVisible = true;

        // Lọc gợi ý
        var suggestions = _allPoi
            .Where(p => p.Name.ToLower().Contains(keyword)
                     || p.Description.ToLower().Contains(keyword)
                     || p.Category.ToLower().Contains(keyword))
            .ToList();

        SuggestionList.ItemsSource = suggestions;
        SuggestionPanel.IsVisible = suggestions.Any();

        // Lọc danh sách chính
        PoiList.ItemsSource = suggestions;
    }

    private void OnClearSearch(object sender, EventArgs e)
    {
        SearchEntry.Text = "";
        SuggestionPanel.IsVisible = false;
        ClearBtn.IsVisible = false;
        PoiList.ItemsSource = _allPoi;
    }

    // Khi nhấn gợi ý
    private async void OnSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not PoiModel poi) return;
        SuggestionPanel.IsVisible = false;
        SearchEntry.Text = poi.Name;
        await NavigateToPoi(poi);
    }

    // ── Chỉ đường trong app ───────────────────────────────────────────

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        if (btn.CommandParameter is not PoiModel poi) return;
        await NavigateToPoi(poi);
    }

    private async void OnPoiTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not PoiModel poi) return;

        var action = await DisplayActionSheet(
            $"📍 {poi.Name}",
            "Hủy", null,
            "🧭 Chỉ đường trong app",
            "📋 Xem chi tiết"
        );

        if (action == "🧭 Chỉ đường trong app")
            await NavigateToPoi(poi);
        else if (action == "📋 Xem chi tiết")
            await Navigation.PushAsync(new PoiDetailPage(poi));
    }

    private async Task NavigateToPoi(PoiModel poi)
    {
        // Chuyển sang tab Bản đồ và hiện route
        _mapVm.ShowRouteToPoi(poi);

        // Switch sang tab Map
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//MapPage");
    }
}