using VinhKhanhTour.Models;
using VinhKhanhTour.Views;

namespace VinhKhanhTour.Services;

public class AudioQueueService
{
    private readonly TtsService _tts;
    private readonly Queue<PoiModel> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private AppLanguage _language = AppLanguage.Vietnamese;
    private bool _cancelled = false;

    public AudioQueueService(TtsService tts)
    {
        _tts = tts;
    }

    public void SetLanguage(AppLanguage lang) => _language = lang;

    public void Enqueue(PoiModel poi)
    {
        _cancelled = false;

        // Ưu tiên Priority 1 — xóa queue hiện tại nếu POI quan trọng hơn
        if (poi.Priority == 1 && _queue.Any())
        {
            _queue.Clear();
            _tts.StopAsync();
        }

        _queue.Enqueue(poi);
        _ = ProcessQueueAsync();
    }

    // Dừng ngay lập tức khi ra khỏi bán kính
    public void StopImmediate()
    {
        _cancelled = true;
        _queue.Clear();
        _ = _tts.StopAsync();
    }

    public void ClearQueue()
    {
        _queue.Clear();
        _ = _tts.StopAsync();
    }

    private async Task ProcessQueueAsync()
    {
        if (!await _semaphore.WaitAsync(0)) return;
        try
        {
            while (_queue.Count > 0 && !_cancelled)
            {
                var poi = _queue.Dequeue();
                if (poi == null) continue;

                // Đọc đủ số lần theo setting
                int repeatCount = SettingsPage.RepeatCount;
                for (int i = 0; i < repeatCount; i++)
                {
                    if (_cancelled) break;

                    var textVi = poi.TtsScript;
                    var textEn = poi.TtsScriptEn;

                    await _tts.SpeakAsync(textVi, textEn);

                    // Chờ 1 giây giữa các lần đọc lại
                    if (i < repeatCount - 1 && !_cancelled)
                        await Task.Delay(1000);
                }

                // Chờ thêm giữa các POI khác nhau
                if (_queue.Count > 0 && !_cancelled)
                    await Task.Delay(500);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}