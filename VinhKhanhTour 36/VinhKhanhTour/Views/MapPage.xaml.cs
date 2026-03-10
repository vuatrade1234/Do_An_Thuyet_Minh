using Microsoft.Maui.Platform;
using VinhKhanhTour.Data;
using VinhKhanhTour.ViewModels;

namespace VinhKhanhTour.Views;

public partial class MapPage : ContentPage
{
    private MapViewModel _vm;
    private bool _mapReady = false;

    public MapPage()
    {
        InitializeComponent();
        _vm = new MapViewModel();
        BindingContext = _vm;

        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("EnableJS", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.Settings.JavaScriptEnabled = true;
            handler.PlatformView.Settings.DomStorageEnabled = true;
            handler.PlatformView.Settings.MixedContentMode =
                Android.Webkit.MixedContentHandling.AlwaysAllow;
            Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
        });

        _vm.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(_vm.MapUpdateJs)
                && _mapReady
                && !string.IsNullOrEmpty(_vm.MapUpdateJs))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await MapWebView.EvaluateJavaScriptAsync(_vm.MapUpdateJs));
            }
        };

        MapWebView.Navigated += (s, e) =>
        {
            _mapReady = true;
            _vm.IsMapInitialized = true;
            MainThread.BeginInvokeOnMainThread(() => _vm.InjectMarkersUpdate());
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _mapReady = false;
        _vm.IsMapInitialized = false;
        MapWebView.Source = new HtmlWebViewSource { Html = _vm.MapHtml };
        if (!_vm.IsTracking)
            await _vm.StartTourCommand.ExecuteAsync(null);
    }

    // ── Map Controls ──────────────────────────────────────────────────

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

    // ── D-pad ─────────────────────────────────────────────────────────

    private void OnMoveUp(object sender, EventArgs e)
        => _vm.MoveVirtualLocation(0.00005, 0);
    private void OnMoveDown(object sender, EventArgs e)
        => _vm.MoveVirtualLocation(-0.00005, 0);
    private void OnMoveLeft(object sender, EventArgs e)
        => _vm.MoveVirtualLocation(0, -0.00007);
    private void OnMoveRight(object sender, EventArgs e)
        => _vm.MoveVirtualLocation(0, 0.00007);
    private void OnMoveReset(object sender, EventArgs e)
        => _vm.ResetVirtualLocation();

    // ── Tour Controls ─────────────────────────────────────────────────

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if (_vm.IsTracking) _vm.StopTourCommand.Execute(null);
        else await _vm.StartTourCommand.ExecuteAsync(null);
    }

    private void OnToggleLanguageClicked(object sender, EventArgs e)
        => _vm.ToggleLanguageCommand.Execute(null);

    private void OnPrevPoiClicked(object sender, EventArgs e)
        => _vm.SelectPrevPoi();

    private void OnNextPoiClicked(object sender, EventArgs e)
        => _vm.SelectNextPoi();

    private void OnDirectionClicked(object sender, EventArgs e)
    {
        if (_vm.SelectedPoi == null) return;
        _vm.ShowRouteToPoi(_vm.SelectedPoi);
    }

    private async void OnPoiDetailClicked(object sender, EventArgs e)
    {
        if (_vm.SelectedPoi == null) return;
        await Navigation.PushAsync(new PoiDetailPage(_vm.SelectedPoi));
    }
}