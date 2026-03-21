using Microsoft.Extensions.Logging;
using VinhKhanhTour.Services;
using VinhKhanhTour.Views;
using VinhKhanhTour.ViewModels;

namespace VinhKhanhTour;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services (Singleton = dùng chung toàn app)
        builder.Services.AddSingleton<TtsService>();
        builder.Services.AddSingleton<GpsTrackingService>();
        builder.Services.AddSingleton<GeofenceService>();
        builder.Services.AddSingleton<AudioQueueService>();

        // Register ViewModels & Pages
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<MapPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}