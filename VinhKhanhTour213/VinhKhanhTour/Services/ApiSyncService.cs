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

    // Convert từ API format (content dictionary) sang MAUI PoiModel
    private List<PoiModel> ConvertFromApi(JsonArray nodes)
    {
        var result = new List<PoiModel>();
        foreach (var node in nodes)
        {
            if (node == null) continue;
            var content = node["content"];
            var audio = node["audioUrls"];

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

                // Lấy nội dung từ content dictionary
                TtsScript = content?["vi"]?.ToString() ?? "",
                TtsScriptEn = content?["en"]?.ToString() ?? "",
                TtsScriptZh = content?["zh"]?.ToString() ?? "",
                TtsScriptJa = content?["ja"]?.ToString() ?? "",
                TtsScriptKo = content?["ko"]?.ToString() ?? "",

                // Description dùng chung content (vì API không có field riêng)
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
        catch
        {
            return Data.PoiData.GetAllPoi();
        }
    }
    public async Task LogPoiPlayAsync(string poiId, string lang, double lat, double lng)
    {
        try
        {
            var ev = new
            {
                eventType = "poi_play",
                poiId = poiId,
                language = lang,
                lat = lat,
                lng = lng,
                timestamp = DateTime.UtcNow
            };

            var res = await _http.PostAsJsonAsync("api/analytics", ev);

            if (res.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[Analytics] OK → {poiId}");
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[Analytics] Fail: {res.StatusCode} - {err}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Analytics] Error: {ex.Message}");
        }
    }
}