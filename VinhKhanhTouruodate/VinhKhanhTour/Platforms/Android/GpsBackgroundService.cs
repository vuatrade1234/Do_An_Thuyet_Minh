using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace VinhKhanhTour.Platforms.Android;

[Service(ForegroundServiceType = ForegroundService.TypeLocation)]
public class GpsBackgroundService : Service
{
    const int NOTIFICATION_ID = 1001;
    const string CHANNEL_ID = "gps_channel";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(
        Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        var notification = BuildNotification();
        StartForeground(NOTIFICATION_ID, notification,
            ForegroundService.TypeLocation);
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        StopForeground(StopForegroundFlags.Remove);
        base.OnDestroy();
    }

    // ── Notification Channel ──────────────────────────────────────
    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var channel = new NotificationChannel(
            CHANNEL_ID,
            "GPS Tour",
            NotificationImportance.Low)
        {
            Description = "Đang theo dõi vị trí tour"
        };

        var manager = (NotificationManager?)
            GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        var intent = new Intent(this, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(
            this, 0, intent,
            PendingIntentFlags.UpdateCurrent |
            PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, CHANNEL_ID)
            .SetContentTitle("🗺️ Phố Ẩm Thực Vĩnh Khánh")
            .SetContentText("Đang dẫn đường tour...")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .Build()!;
    }
}