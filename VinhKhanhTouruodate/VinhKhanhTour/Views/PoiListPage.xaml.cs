using CommunityToolkit.Mvvm.Messaging;
using VinhKhanhTour.Data;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;

namespace VinhKhanhTour.Views;

public class ShowRouteMessage
{
    public PoiModel Poi { get; set; } = null!;
}

public partial class PoiListPage : ContentPage
{
    private List<PoiModel> _allPoi = new();
    private LocalizationService _loc = LocalizationService.Instance;
    private readonly ApiSyncService _apiSync = new();

    public PoiListPage()
    {
        InitializeComponent();
        _allPoi = PoiData.GetAllPoi();
        PoiList.ItemsSource = _allPoi;
        RefreshLocalizedText();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshLocalizedText();
        _loc.LanguageChanged += OnLanguageChanged;
        _ = LoadPoisFromApiAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loc.LanguageChanged -= OnLanguageChanged;
    }

    private async Task LoadPoisFromApiAsync()
    {
        var pois = await _apiSync.GetPoisAsync();
        if (pois.Count > 0)
        {
            _allPoi = pois;
            MainThread.BeginInvokeOnMainThread(RefreshList);
        }
    }

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshLocalizedText();
            RefreshList();
        });
    }

    private void RefreshLocalizedText()
    {
        LblListTitle.Text = _loc.Get("poi_list_title");
        SearchEntry.Placeholder = _loc.Get("search_placeholder");
    }

    private void RefreshList()
    {
        PoiList.ItemsSource = null;
        PoiList.ItemsSource = _allPoi;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = e.NewTextValue?.Trim().ToLower() ?? "";

        if (string.IsNullOrEmpty(kw))
        {
            SuggestionPanel.IsVisible = false;
            ClearBtn.IsVisible = false;
            PoiList.ItemsSource = _allPoi;
            return;
        }

        ClearBtn.IsVisible = true;

        var filtered = _allPoi.Where(p =>
            p.Name.ToLower().Contains(kw) ||
            p.LocalizedName.ToLower().Contains(kw) ||
            p.Description.ToLower().Contains(kw) ||
            p.LocalizedDescription.ToLower().Contains(kw) ||
            p.Category.ToLower().Contains(kw)).ToList();

        SuggestionList.ItemsSource = filtered;
        SuggestionPanel.IsVisible = filtered.Any();
        PoiList.ItemsSource = filtered;
    }

    private void OnClearSearch(object sender, EventArgs e)
    {
        SearchEntry.Text = "";
        SuggestionPanel.IsVisible = false;
        ClearBtn.IsVisible = false;
        PoiList.ItemsSource = _allPoi;
    }

    private async void OnSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not PoiModel poi) return;
        SuggestionPanel.IsVisible = false;
        SearchEntry.Text = poi.LocalizedName;
        await NavigateToPoi(poi);
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PoiModel poi)
            await NavigateToPoi(poi);
    }

    private async void OnPoiTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not PoiModel poi) return;

        var action = await DisplayActionSheetAsync(
            $"📍 {poi.LocalizedName}",
            _loc.Get("cancel"), null,
            _loc.Get("navigate_in_app"),
            _loc.Get("view_detail")
        );

        if (action == _loc.Get("navigate_in_app"))
            await NavigateToPoi(poi);
        else if (action == _loc.Get("view_detail"))
            await Navigation.PushAsync(new PoiDetailPage(poi));
    }

    private async Task NavigateToPoi(PoiModel poi)
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("//MapPage");
        await Task.Delay(300);
        WeakReferenceMessenger.Default.Send(new ShowRouteMessage { Poi = poi });
    }
}