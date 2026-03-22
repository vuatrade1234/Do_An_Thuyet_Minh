using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services;

public class ApiSyncService
{
    private readonly HttpClient _http;
    const string API_BASE = "https://vinhkhanh-api.onrender.com/";
    const string CACHE_FILE = "pois_cache.json";

    public ApiSyncService()
        => _http = new HttpClient { BaseAddress = new Uri(API_BASE) };

    public async Task<List<PoiModel>> GetPoisAsync()
    {
        try
        {
            var json = await _http.GetStringAsync("api/pois");
            var nodes = JsonNode.Parse(json)?.AsArray();
            if (nodes != null && nodes.Count > 0)
            {
                var pois = ConvertFromApi(nodes);
                await SaveCacheAsync(pois);
                return pois;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiSync] Lỗi: {ex.Message}");
        }
        return await LoadCacheAsync();
    }

    private List<PoiModel> ConvertFromApi(JsonArray nodes)
    {
        var result = new List<PoiModel>();
        foreach (var node in nodes)
        {
            if (node == null) continue;
            var content = node["content"];
            var poi = new PoiModel
            {
                Id = node["id"]?.ToString() ?? "",
                Name = node["name"]?.ToString() ?? "",
                Category = node["category"]?.ToString() ?? "food",
                Latitude = node["latitude"]?.GetValue<double>() ?? 0,
                Longitude = node["longitude"]?.GetValue<double>() ?? 0,
                RadiusMeters = node["radius"]?.GetValue<double>() ?? 30,
                Priority = node["priority"]?.GetValue<int>() ?? 1,
                IsActive = node["isActive"]?.GetValue<bool>() ?? true,
                TtsScript = content?["vi"]?.ToString() ?? "",
                TtsScriptEn = content?["en"]?.ToString() ?? "",
                TtsScriptZh = content?["zh"]?.ToString() ?? "",
                TtsScriptJa = content?["ja"]?.ToString() ?? "",
                TtsScriptKo = content?["ko"]?.ToString() ?? "",
                Description = content?["vi"]?.ToString() ?? "",
                DescriptionEn = content?["en"]?.ToString() ?? "",
                DescriptionZh = content?["zh"]?.ToString() ?? "",
                DescriptionJa = content?["ja"]?.ToString() ?? "",
                DescriptionKo = content?["ko"]?.ToString() ?? "",
            };
            result.Add(poi);
        }
        return result;
    }

    private async Task SaveCacheAsync(List<PoiModel> pois)
    {
        try
        {
            var json = JsonSerializer.Serialize(pois);
            var path = Path.Combine(FileSystem.CacheDirectory, CACHE_FILE);
            await File.WriteAllTextAsync(path, json);
        }
        catch { }
    }

    private async Task<List<PoiModel>> LoadCacheAsync()
    {
        try
        {
            var path = Path.Combine(FileSystem.CacheDirectory, CACHE_FILE);
            if (!File.Exists(path)) return Data.PoiData.GetAllPoi();
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<PoiModel>>(json)
                   ?? Data.PoiData.GetAllPoi();
        }
        catch { return Data.PoiData.GetAllPoi(); }
    }

    // ── Ghi lịch sử nghe POI ─────────────────────────────────────────
    public async Task LogPoiPlayAsync(string poiId, string poiName, string lang, int duration = 0)
    {
        await LogHistoryAsync("play_poi", poiId, poiName, lang, duration);
    }

    // ── Ghi lịch sử quét QR ──────────────────────────────────────────
    public async Task LogQrScanAsync(string poiId, string poiName, string lang)
    {
        await LogHistoryAsync("scan_qr", poiId, poiName, lang);
    }

    // ── Ghi lịch sử chỉ đường ────────────────────────────────────────
    public async Task LogRouteStartAsync(string poiId, string poiName, string lang)
    {
        await LogHistoryAsync("route_start", poiId, poiName, lang);
    }

    // ── Core ghi history → api/history ───────────────────────────────
    private async Task LogHistoryAsync(string action, string poiId, string poiName, string lang, int duration = 0)
    {
        try
        {
            var body = new
            {
                action = action,
                poiId = poiId,
                poiName = poiName,
                language = lang,
                device = GetDeviceName(),
                duration = duration,
                timestamp = DateTime.UtcNow
            };

            var res = await _http.PostAsJsonAsync("api/history", body);
            System.Diagnostics.Debug.WriteLine(res.IsSuccessStatusCode
                ? $"[History] OK → {action} {poiName}"
                : $"[History] Fail: {res.StatusCode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[History] Error: {ex.Message}");
        }
    }

    private static string GetDeviceName()
    {
        try { return $"{DeviceInfo.Current.Model} ({DeviceInfo.Current.Platform})"; }
        catch { return "Unknown"; }
    }
}