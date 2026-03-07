using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services;

public class GpsTrackingService
{
    private CancellationTokenSource? _cts;
    private GpsLocation? _lastLocation;
    private bool _isTracking;

    // Events
    public event Action<GpsLocation>? LocationUpdated;
    public event Action<string>? ErrorOccurred;

    public bool IsTracking => _isTracking;
    public GpsLocation? LastLocation => _lastLocation;

    public async Task StartTracking()
    {
        if (_isTracking) return;

        // Kiểm tra permission
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            ErrorOccurred?.Invoke("Cần cấp quyền vị trí để sử dụng app.");
            return;
        }

        _isTracking = true;
        _cts = new CancellationTokenSource();

        // Bắt đầu vòng lặp GPS
        _ = Task.Run(() => TrackingLoop(_cts.Token));
    }

    public void StopTracking()
    {
        _isTracking = false;
        _cts?.Cancel();
    }

    private async Task TrackingLoop(CancellationToken ct)
    {
        var request = new GeolocationRequest(
            GeolocationAccuracy.Best,        // độ chính xác cao nhất
            TimeSpan.FromSeconds(3)          // timeout mỗi lần lấy vị trí
        );

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(request, ct);

                if (location != null)
                {
                    var gpsLoc = new GpsLocation
                    {
                        Latitude  = location.Latitude,
                        Longitude = location.Longitude,
                        Accuracy  = location.Accuracy ?? 0,
                        Timestamp = location.Timestamp.UtcDateTime,
                        Speed     = location.Speed ?? 0
                    };

                    _lastLocation = gpsLoc;
                    LocationUpdated?.Invoke(gpsLoc);
                }
            }
            catch (FeatureNotSupportedException)
            {
                ErrorOccurred?.Invoke("Thiết bị không hỗ trợ GPS.");
                break;
            }
            catch (PermissionException)
            {
                ErrorOccurred?.Invoke("Quyền GPS bị từ chối.");
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ErrorOccurred?.Invoke($"Lỗi GPS: {ex.Message}");
            }

            // Interval cập nhật: 5 giây (cân bằng pin & độ chính xác)
            try { await Task.Delay(5000, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>Tính khoảng cách (meters) giữa 2 tọa độ — Haversine Formula</summary>
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // bán kính Trái Đất (m)
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
