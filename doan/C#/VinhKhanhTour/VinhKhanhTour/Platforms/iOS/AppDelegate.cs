using Foundation;
using UIKit;

namespace VinhKhanhTour
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        // ── Deep Link: vinhkhanh://play/{tourId}  &  vinhkhanh://navigate/{tourId} ──
        public override bool OpenUrl(UIApplication application, NSUrl url,
            NSDictionary options)
        {
            DeepLinkHandler.Handle(url.AbsoluteString);
            return true;
        }
    }
}