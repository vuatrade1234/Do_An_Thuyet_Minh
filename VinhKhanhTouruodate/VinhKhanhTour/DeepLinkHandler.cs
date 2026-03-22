using VinhKhanhTour.Services;

namespace VinhKhanhTour;

/// <summary>
/// Xử lý deep link:
///   vinhkhanh://poi/{poiId}          → đọc thuyết minh POI ngay, không GPS
///   vinhkhanh://play/{tourId}        → mở tour, phát audio theo GPS
///   vinhkhanh://navigate/{tourId}    → mở tour, bật chỉ đường
/// </summary>
public static class DeepLinkHandler
{
    private const string API_BASE = "https://vinhkhanh-api.onrender.com";
    private static readonly ApiSyncService _apiSync = new();

    public static void Handle(string? rawUri)
    {
        if (string.IsNullOrWhiteSpace(rawUri)) return;

        Uri uri;
        try { uri = new Uri(rawUri); }
        catch { return; }

        if (!uri.Scheme.Equals("vinhkhanh", StringComparison.OrdinalIgnoreCase))
            return;

        var action = uri.Host.ToLowerInvariant();   // "poi" | "play" | "navigate"
        var id = uri.AbsolutePath.Trim('/');    // poiId hoặc tourId

        if (string.IsNullOrEmpty(id)) return;

        System.Diagnostics.Debug.WriteLine($"[DeepLink] action={action} id={id}");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var route = action switch
                {
                    "poi" => $"//MapPage?poiId={Uri.EscapeDataString(id)}&mode=speak",
                    "play" => $"//MapPage?tourId={Uri.EscapeDataString(id)}&mode=play",
                    "navigate" => $"//MapPage?tourId={Uri.EscapeDataString(id)}&mode=navigate",
                    _ => $"//MapPage?poiId={Uri.EscapeDataString(id)}&mode=speak"
                };

                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Nav error: {ex.Message}");
            }
        });

        // Ghi lịch sử + tăng counter (fire-and-forget)
        _ = LogAndIncrementAsync(action, id);
    }

    private static async Task LogAndIncrementAsync(string action, string id)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();

            if (action == "poi")
            {
                // Lấy tên POI từ AllPoi đang có trong MapViewModel
                var poiName = TryGetPoiName(id);

                // Ghi lịch sử quét QR vào api/history
                await _apiSync.LogQrScanAsync(id, poiName, GetCurrentLang());

                // Tăng counter scan cho POI
                await http.PostAsync($"{API_BASE}/api/pois/{id}/scan", null);

                System.Diagnostics.Debug.WriteLine($"[DeepLink] QR scan logged: {poiName}");
            }
            else
            {
                // Tour QR — tăng counter tour
                await http.PostAsync($"{API_BASE}/api/tours/{id}/scan", null);
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Tour scan incremented: {id}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Log failed: {ex.Message}");
        }
    }

    // Lấy tên POI từ cache (nếu có)
    private static string TryGetPoiName(string poiId)
    {
        try
        {
            // Tìm trong AllPoi của MapViewModel qua Shell
            var mapVm = (Shell.Current?.CurrentPage?.BindingContext
                        as VinhKhanhTour.ViewModels.MapViewModel);
            var poi = mapVm?.AllPoi?.FirstOrDefault(p => p.Id == poiId);
            return poi?.Name ?? poiId;
        }
        catch { return poiId; }
    }

    // Lấy ngôn ngữ hiện tại
    private static string GetCurrentLang()
    {
        try
        {
            return LocalizationService.Instance.CurrentLocale
                .ToString().ToLower() switch
            {
                "vietnamese" => "vi",
                "english" => "en",
                "chinese" => "zh",
                "japanese" => "ja",
                "korean" => "ko",
                _ => "vi"
            };
        }
        catch { return "vi"; }
    }
}