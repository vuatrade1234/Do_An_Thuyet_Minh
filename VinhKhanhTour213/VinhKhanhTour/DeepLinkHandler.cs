namespace VinhKhanhTour;

/// <summary>
/// Xử lý deep link vinhkhanh://play/{tourId} và vinhkhanh://navigate/{tourId}
/// Gọi từ Android MainActivity và iOS AppDelegate.
/// </summary>
public static class DeepLinkHandler
{
    // URL API backend — đổi nếu deploy lên server khác
    private const string API_BASE = "https://vinhkhanh-api.onrender.com";

    public static void Handle(string? rawUri)
    {
        if (string.IsNullOrWhiteSpace(rawUri)) return;

        Uri uri;
        try { uri = new Uri(rawUri); }
        catch { return; }

        if (!uri.Scheme.Equals("vinhkhanh", StringComparison.OrdinalIgnoreCase))
            return;

        var action = uri.Host.ToLowerInvariant();   // "play" hoặc "navigate"
        var tourId = uri.AbsolutePath.Trim('/');    // "{tourId}"

        if (string.IsNullOrEmpty(tourId)) return;

        System.Diagnostics.Debug.WriteLine($"[DeepLink] Received: {rawUri}");

        // Tăng QrScans counter (fire-and-forget, không block UI)
        _ = IncrementScanAsync(tourId);

        // Chạy trên UI thread
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var route = action switch
                {
                    "play" => $"//MapPage?tourId={Uri.EscapeDataString(tourId)}&mode=play",
                    "navigate" => $"//MapPage?tourId={Uri.EscapeDataString(tourId)}&mode=navigate",
                    _ => $"//MapPage?tourId={Uri.EscapeDataString(tourId)}&mode=play"
                };

                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Navigation error: {ex.Message}");
            }
        });
    }

    // Gọi API tăng QrScans +1 mỗi khi quét QR
    private static async Task IncrementScanAsync(string tourId)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            await http.PostAsync($"{API_BASE}/api/tours/{tourId}/scan", null);
            System.Diagnostics.Debug.WriteLine($"[DeepLink] QrScans incremented for {tourId}");
        }
        catch (Exception ex)
        {
            // Không ảnh hưởng UX nếu API lỗi
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Increment failed: {ex.Message}");
        }
    }
}