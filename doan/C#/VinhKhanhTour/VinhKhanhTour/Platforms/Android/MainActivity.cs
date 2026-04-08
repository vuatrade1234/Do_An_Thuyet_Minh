using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace VinhKhanhTour
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges =
            ConfigChanges.ScreenSize | ConfigChanges.Orientation |
            ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

    // ── Deep Link: vinhkhanh://play/{tourId}  &  vinhkhanh://navigate/{tourId} ──
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "vinhkhanh")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleDeepLink(intent);
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleDeepLink(Intent);
        }

        private static void HandleDeepLink(Intent? intent)
        {
            if (intent?.Action != Intent.ActionView) return;
            var uri = intent.Data?.ToString();
            if (string.IsNullOrEmpty(uri)) return;

            // Gửi sang DeepLinkHandler để xử lý điều hướng
            DeepLinkHandler.Handle(uri);
        }
    }
}