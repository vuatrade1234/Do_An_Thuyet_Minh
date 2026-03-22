using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services;

public class AudioQueueService
{
    private readonly TtsService _tts;
    private readonly Queue<PoiModel> _queue = new();
    private readonly object _lock = new();
    private bool _isPlaying;
    private AppLanguage _language = AppLanguage.Vietnamese;
    private int _repeatCount => Views.SettingsPage.RepeatCount;
    private int _cooldownSecs => Views.SettingsPage.CooldownSeconds;

    // Callback ghi analytics với duration thật
    // MapViewModel set: _audioQueue.OnPoiSpoken = (id, name, lang, sec) => ...
    public Action<string, string, string, int>? OnPoiSpoken { get; set; }

    public AudioQueueService(TtsService tts)
    {
        _tts = tts;
    }

    public void SetLanguage(AppLanguage lang)
        => _language = lang;

    public void Enqueue(PoiModel poi)
    {
        lock (_lock)
        {
            if (_queue.Any(p => p.Id == poi.Id)) return;
            _queue.Enqueue(poi);
        }

        if (!_isPlaying)
            _ = PlayNextAsync();
    }

    public void StopImmediate()
    {
        lock (_lock) { _queue.Clear(); }
        _tts.StopAsync();
        _isPlaying = false;
    }

    public void ClearQueue()
    {
        lock (_lock) { _queue.Clear(); }
        _isPlaying = false;
    }

    private async Task PlayNextAsync()
    {
        _isPlaying = true;

        while (true)
        {
            PoiModel? poi;
            lock (_lock)
            {
                if (_queue.Count == 0) break;
                poi = _queue.Dequeue();
            }

            if (poi == null) break;

            var script = poi.LocalizedTtsScript;
            if (string.IsNullOrEmpty(script)) continue;

            // Đo thời gian đọc thật
            var startTime = DateTime.Now;

            for (int i = 0; i < _repeatCount; i++)
            {
                await _tts.SpeakAsync(script);
                if (i < _repeatCount - 1)
                    await Task.Delay(1000);
            }

            var duration = (int)(DateTime.Now - startTime).TotalSeconds;

            System.Diagnostics.Debug.WriteLine(
                $"[Duration] {poi.Name} = {duration}s");

            // Ghi analytics
            var lang = _language switch
            {
                AppLanguage.Vietnamese => "vi",
                AppLanguage.English => "en",
                AppLanguage.Chinese => "zh",
                AppLanguage.Japanese => "ja",
                AppLanguage.Korean => "ko",
                _ => "vi"
            };
            OnPoiSpoken?.Invoke(poi.Id, poi.Name, lang, duration);

            await Task.Delay(_cooldownSecs * 1000);
        }

        _isPlaying = false;
    }
}