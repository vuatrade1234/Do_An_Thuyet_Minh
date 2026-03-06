using VinhKhanhTour.Models;
using VinhKhanhTour.Views;

namespace VinhKhanhTour.Services;

public class GeofenceService
{
    private readonly Dictionary<string, DateTime> _lastTriggered = new();
    private readonly Dictionary<string, Timer> _debounceTimers = new();
    private readonly HashSet<string> _currentlyInside = new();

    // Dùng settings động
    private TimeSpan Cooldown => TimeSpan.FromSeconds(SettingsPage.CooldownSeconds);
    private TimeSpan Debounce => TimeSpan.FromSeconds(3);

    public event Action<PoiModel, TriggerType>? PoiTriggered;
    public event Action? StopSpeaking; // khi ra khỏi bán kính
    public enum TriggerType { Enter, Nearby }

    public void CheckGeofences(GpsLocation userLocation, IEnumerable<PoiModel> allPoi)
        => CheckGeofencesWithRadius(userLocation, allPoi, SettingsPage.GlobalRadius);

    public void CheckGeofencesWithRadius(GpsLocation userLocation,
        IEnumerable<PoiModel> allPoi, double radiusMeters)
    {
        var sorted = allPoi.Where(p => p.IsActive).ToList();

        // Tính khoảng cách tất cả POI
        foreach (var poi in sorted)
        {
            poi.DistanceFromUser = GpsTrackingService.CalculateDistance(
                userLocation.Latitude, userLocation.Longitude,
                poi.Latitude, poi.Longitude);
        }

        // POI đang trong bán kính — sắp xếp theo khoảng cách (gần nhất ưu tiên)
        var insidePois = sorted
            .Where(p => p.DistanceFromUser <= radiusMeters)
            .OrderBy(p => p.DistanceFromUser)
            .ToList();

        // POI vừa vào bán kính
        foreach (var poi in insidePois)
        {
            bool wasInside = _currentlyInside.Contains(poi.Id);
            if (!wasInside)
            {
                _currentlyInside.Add(poi.Id);
                // Chỉ trigger POI gần nhất nếu nhiều POI overlap
                var nearest = insidePois.First();
                if (poi.Id == nearest.Id)
                    TriggerWithDebounce(poi, TriggerType.Enter);
            }
        }

        // POI đã ra khỏi bán kính
        var insideIds = insidePois.Select(p => p.Id).ToHashSet();
        var justExited = _currentlyInside.Where(id => !insideIds.Contains(id)).ToList();

        foreach (var id in justExited)
        {
            _currentlyInside.Remove(id);
            _lastTriggered.Remove(id);
        }

        // Nếu vừa ra khỏi TẤT CẢ bán kính → stop TTS
        if (justExited.Any() && _currentlyInside.Count == 0)
            StopSpeaking?.Invoke();

        // Cập nhật IsNearby
        foreach (var poi in sorted)
        {
            poi.IsNearby = poi.DistanceFromUser <= radiusMeters * 1.5
                        && poi.DistanceFromUser > radiusMeters;
        }
    }

    private void TriggerWithDebounce(PoiModel poi, TriggerType type, string? key = null)
    {
        key ??= poi.Id;

        if (_lastTriggered.TryGetValue(key, out var lastTime))
            if (DateTime.UtcNow - lastTime < Cooldown) return;

        if (_debounceTimers.TryGetValue(key, out var existing))
            existing.Dispose();

        _debounceTimers[key] = new Timer(_ =>
        {
            _lastTriggered[key] = DateTime.UtcNow;
            _debounceTimers.Remove(key);
            PoiTriggered?.Invoke(poi, type);
        }, null, Debounce, Timeout.InfiniteTimeSpan);
    }

    public void ResetCooldowns() => _lastTriggered.Clear();
}