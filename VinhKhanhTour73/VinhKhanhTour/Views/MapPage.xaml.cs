using CommunityToolkit.Mvvm.Messaging;
using VinhKhanhTour.Data;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;
using VinhKhanhTour.ViewModels;

namespace VinhKhanhTour.Views;

public partial class MapPage : ContentPage
{
    private MapViewModel _vm = null!;
    private LocalizationService _loc = LocalizationService.Instance;
    private bool _mapReady = false;

    public MapPage()
    {
        InitializeComponent();
        _vm = new MapViewModel();
        BindingContext = _vm;

        // Bật JS cho Android WebView
        Microsoft.Maui.Handlers.WebViewHandler.Mapper
            .AppendToMapping("EnableJS", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.Settings.JavaScriptEnabled = true;
                handler.PlatformView.Settings.DomStorageEnabled = true;
                handler.PlatformView.Settings.MixedContentMode =
                    Android.Webkit.MixedContentHandling.AlwaysAllow;
                Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
            });

        // ── Lắng nghe thay đổi từ ViewModel ──────────────────────────────────
        _vm.JsInjectionRequested += async (js) =>
        {
            if (_mapReady) await EvalJs(js);
        };

        _vm.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(_vm.MapUpdateJs)
                && _mapReady
                && !string.IsNullOrEmpty(_vm.MapUpdateJs))
            {
                await EvalJs(_vm.MapUpdateJs);
            }

            if (e.PropertyName == nameof(_vm.ShowPoiPanel)
             || e.PropertyName == nameof(_vm.HasPoiInRadius))
                MainThread.BeginInvokeOnMainThread(UpdatePoiPanel);

            if (e.PropertyName == nameof(_vm.ShowCancelRoute))
                MainThread.BeginInvokeOnMainThread(() =>
                    CancelRouteBtn.IsVisible = _vm.ShowCancelRoute);
        };
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _mapReady = false;
        _vm.IsMapInitialized = false;

        UpdatePoiPanel();
        UpdateLangButtonColor();
        RefreshLocalizedText();

        // Load map HTML
        MapWebView.Source = new HtmlWebViewSource { Html = _vm.MapHtml };

        // Tự động bắt đầu tour
        if (!_vm.IsTracking)
            await _vm.StartTourCommand.ExecuteAsync(null);

        // Nhận message ShowRoute từ PoiListPage / PoiDetailPage
        WeakReferenceMessenger.Default
            .Register<ShowRouteMessage>(this, (r, msg) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _vm.ShowRouteToPoi(msg.Poi);
                    UpdatePoiPanel();
                    CancelRouteBtn.IsVisible = true;
                });
            });

        _loc.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        WeakReferenceMessenger.Default.Unregister<ShowRouteMessage>(this);
        _loc.LanguageChanged -= OnLanguageChanged;
    }

    // ── JS Helper ─────────────────────────────────────────────────────────────

    private async Task EvalJs(string js)
    {
        if (string.IsNullOrEmpty(js)) return;
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await MapWebView.EvaluateJavaScriptAsync(js));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EvalJs error: {ex.Message}");
        }
    }

    // ── WebView Events ────────────────────────────────────────────────────────

    private void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
    {
        _mapReady = true;
        _vm.IsMapInitialized = true;
        MainThread.BeginInvokeOnMainThread(() => _vm.InjectMarkersUpdate());
    }

    // Nhận link "mauibridge://showroute?id=..." từ button trong popup marker
    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("mauibridge://")) return;
        e.Cancel = true;

        try
        {
            var uri = new Uri(e.Url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var poiId = Uri.UnescapeDataString(query["id"] ?? "");
            if (string.IsNullOrEmpty(poiId)) return;

            var poi = _vm.AllPoi.FirstOrDefault(p => p.Id == poiId);
            if (poi == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _vm.ShowRouteToPoi(poi);
                UpdatePoiPanel();
                CancelRouteBtn.IsVisible = true;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bridge error: {ex.Message}");
        }
    }

    // ── Language ──────────────────────────────────────────────────────────────

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateLangButtonColor();
            RefreshLocalizedText();
            // Reload map để popup đổi ngôn ngữ
            _mapReady = false;
            _vm.GenerateMapHtml();
            MapWebView.Source = new HtmlWebViewSource { Html = _vm.MapHtml };
        });
    }

    private void UpdateLangButtonColor()
    {
        var color = LocalizationService.LocaleColor[_loc.CurrentLocale];
        BtnLangHeader.BackgroundColor = Color.FromArgb(color);
    }

    private void RefreshLocalizedText()
    {
        LblNoPoi.Text = _loc.Get("no_poi_in_radius");
        CancelRouteBtn.Text = _loc.Get("cancel_route");
    }

    // Mở ActionSheet chọn ngôn ngữ
    private async void OnToggleLanguageClicked(object sender, EventArgs e)
    {
        var options = LocalizationService.LocaleInfo
            .Select(kv => $"{kv.Value.Flag}  {kv.Value.NativeName}")
            .ToArray();

        var chosen = await DisplayActionSheet(
            "🌐 Language / Ngôn ngữ",
            _loc.Get("cancel"), null,
            options);

        if (string.IsNullOrEmpty(chosen) || chosen == _loc.Get("cancel")) return;

        var selected = LocalizationService.LocaleInfo
            .FirstOrDefault(kv => chosen.Contains(kv.Value.NativeName));

        if (selected.Key != default)
            _loc.SetLocale(selected.Key);
    }

    // ── Bottom Panel ──────────────────────────────────────────────────────────

    private void UpdatePoiPanel()
    {
        var show = _vm.ShowPoiPanel;
        NoPOIBanner.IsVisible = !show;
        PoiInfoGrid.IsVisible = show;
        PoiIndexLabel.IsVisible = show;
    }

    // ── Map Controls ──────────────────────────────────────────────────────────

    private void OnZoomInClicked(object sender, EventArgs e)
        => _ = MapWebView.EvaluateJavaScriptAsync("map.zoomIn();");

    private void OnZoomOutClicked(object sender, EventArgs e)
        => _ = MapWebView.EvaluateJavaScriptAsync("map.zoomOut();");

    private void OnMyLocationClicked(object sender, EventArgs e)
    {
        var lat = (_vm.CurrentLocation?.Latitude ?? PoiData.CENTER_LAT)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lng = (_vm.CurrentLocation?.Longitude ?? PoiData.CENTER_LNG)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        _ = MapWebView.EvaluateJavaScriptAsync($"map.flyTo([{lat},{lng}],18);");
    }

    // ── D-pad ─────────────────────────────────────────────────────────────────

    private void OnMoveUp(object sender, EventArgs e) => _vm.MoveVirtualLocation(0.00005, 0);
    private void OnMoveDown(object sender, EventArgs e) => _vm.MoveVirtualLocation(-0.00005, 0);
    private void OnMoveLeft(object sender, EventArgs e) => _vm.MoveVirtualLocation(0, -0.00007);
    private void OnMoveRight(object sender, EventArgs e) => _vm.MoveVirtualLocation(0, 0.00007);
    private void OnMoveReset(object sender, EventArgs e) => _vm.ResetVirtualLocation();

    // ── Tour ──────────────────────────────────────────────────────────────────

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if (_vm.IsTracking) _vm.StopTourCommand.Execute(null);
        else await _vm.StartTourCommand.ExecuteAsync(null);
    }

    // ── POI Controls ─────────────────────────────────────────────────────────

    private void OnDirectionClicked(object sender, EventArgs e)
    {
        if (_vm.SelectedPoi == null) return;
        _vm.ShowRouteToPoi(_vm.SelectedPoi);
        CancelRouteBtn.IsVisible = true;
    }

    private async void OnPoiDetailClicked(object sender, EventArgs e)
    {
        if (_vm.SelectedPoi == null) return;
        await Navigation.PushAsync(new PoiDetailPage(_vm.SelectedPoi));
    }

    private void OnCancelRouteClicked(object sender, EventArgs e)
    {
        _vm.CancelRoute();
        CancelRouteBtn.IsVisible = false;
    }
}
