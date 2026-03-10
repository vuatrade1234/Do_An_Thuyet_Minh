using Google.Cloud.Firestore;
using VinhKhanhTour.Shared.Models;
namespace VinhKhanhTour.Api.Services;

public class FirestoreService
{
    private readonly FirestoreDb _db;

    // ⚠️ Thay "vinhkhanhtour-xxxxx" bằng Project ID của bạn
    const string PROJECT_ID = "vinhkhanhtour-c8e3f";
    public FirestoreService()
        => _db = FirestoreDb.Create(PROJECT_ID);

    // ── POI ──────────────────────────────────────────
    public async Task<List<PoiModel>> GetAllPoisAsync()
    {
        var snap = await _db.Collection("pois").GetSnapshotAsync();
        return snap.Documents
            .Select(d => d.ConvertTo<PoiModel>())
            .ToList();
    }

    public async Task<string> SavePoiAsync(PoiModel poi)
    {
        poi.UpdatedAt = DateTime.UtcNow;
        if (string.IsNullOrEmpty(poi.Id))
        {
            poi.Id = Guid.NewGuid().ToString("N")[..8];
            poi.CreatedAt = DateTime.UtcNow;
        }
        await _db.Collection("pois").Document(poi.Id).SetAsync(poi);
        return poi.Id;
    }

    public async Task DeletePoiAsync(string id)
        => await _db.Collection("pois").Document(id).DeleteAsync();

    // ── Analytics ─────────────────────────────────────
    public async Task LogEventAsync(AnalyticsEvent ev)
    {
        ev.Lat = Math.Round(ev.Lat, 3);
        ev.Lng = Math.Round(ev.Lng, 3);
        await _db.Collection("analytics").AddAsync(ev);
    }

    // ── App History ───────────────────────────────────
    public async Task<List<AppHistory>> GetHistoryAsync(int limit = 100)
    {
        var snap = await _db.Collection("history")
            .OrderByDescending("Timestamp")
            .Limit(limit)
            .GetSnapshotAsync();
        return snap.Documents
            .Select(d => d.ConvertTo<AppHistory>())
            .ToList();
    }

    public async Task LogHistoryAsync(AppHistory history)
    {
        history.Id = Guid.NewGuid().ToString("N")[..8];
        await _db.Collection("history").Document(history.Id).SetAsync(history);
    }

    // ── Tour ──────────────────────────────────────────
    public async Task<List<TourModel>> GetAllToursAsync()
    {
        var snap = await _db.Collection("tours").GetSnapshotAsync();
        return snap.Documents
            .Select(d => d.ConvertTo<TourModel>())
            .ToList();
    }

    public async Task<string> SaveTourAsync(TourModel tour)
    {
        if (string.IsNullOrEmpty(tour.Id))
        {
            tour.Id = Guid.NewGuid().ToString("N")[..8];
            tour.CreatedAt = DateTime.UtcNow;
        }
        await _db.Collection("tours").Document(tour.Id).SetAsync(tour);
        return tour.Id;
    }

    public async Task DeleteTourAsync(string id)
        => await _db.Collection("tours").Document(id).DeleteAsync();
}