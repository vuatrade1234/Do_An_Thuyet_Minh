using VinhKhanhTour.Services;

namespace VinhKhanhTour;

public partial class AppShell : Shell
{
    private LocalizationService _loc = LocalizationService.Instance;

    public AppShell()
    {
        InitializeComponent();
        RefreshTabTitles();
        _loc.LanguageChanged += () =>
            MainThread.BeginInvokeOnMainThread(RefreshTabTitles);
        Routing.RegisterRoute("MapPage", typeof(Views.MapPage));
    }

    private void RefreshTabTitles()
    {
        TabMap.Title = _loc.Get("tab_map");
        TabList.Title = _loc.Get("tab_list");
        TabSettings.Title = _loc.Get("tab_settings");
    }
}