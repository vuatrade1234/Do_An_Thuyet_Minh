using VinhKhanhTour.Models;
using Microsoft.Maui.Media;
using System.Threading;

namespace VinhKhanhTour;

public partial class DetailsPage : ContentPage
{
    private POI _p;
    private CancellationTokenSource? _dtsCts;

    public DetailsPage(POI p)
    {
        InitializeComponent();
        _p = p;
        lblTitle.Text = p.TenQuan;
        lblMenu.Text = p.Menu;
        imgPOI.Source = p.HinhAnh;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await SpeakMenu();
    }

    private async Task SpeakMenu()
    {
        if (_dtsCts != null) { _dtsCts.Cancel(); _dtsCts.Dispose(); }
        _dtsCts = new CancellationTokenSource();
        try
        {
            // FIX CHO .NET 10: Dùng đúng biến _dtsCts và truyền tham số trực tiếp
            await TextToSpeech.Default.SpeakAsync($"Menu của {_p.TenQuan} có: {_p.Menu}", options: null, cancelToken: _dtsCts.Token);
        }
        catch (OperationCanceledException) { }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_dtsCts != null) _dtsCts.Cancel();
    }

    private async void OnReplayMenu(object sender, EventArgs e) => await SpeakMenu();

    private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}