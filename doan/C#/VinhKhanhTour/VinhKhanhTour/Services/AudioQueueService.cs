using Plugin.Maui.Audio;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Services;

public class AudioQueueService
{
    private readonly TtsService _tts;
    private readonly Queue<PoiModel> _queue = new();
    private readonly object _lock = new();
    private bool _isPlaying;
    private bool _stopRequested; // Cờ theo dõi khi muốn dừng khẩn cấp
    private IAudioPlayer? _currentPlayer;
    private AppLanguage _language = AppLanguage.Vietnamese;
    private int _repeatCount => Views.SettingsPage.RepeatCount;
    private int _cooldownSecs => Views.SettingsPage.CooldownSeconds;

    // Callback ghi analytics với duration thật
    public Action<string, string, string, int>? OnPoiSpoken { get; set; }

    // HttpClient để stream audio từ GCS
    private static readonly HttpClient _httpClient = new();

    public AudioQueueService(TtsService tts)
    {
        _tts = tts;
    }

    public void SetLanguage(AppLanguage lang) => _language = lang;

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
        _stopRequested = true;
        
        // Dừng TTS
        _tts.StopAsync();

        // Dừng và giải phóng file âm thanh đang chạy
        if (_currentPlayer != null)
        {
            _currentPlayer.Stop();
            _currentPlayer.Dispose();
            _currentPlayer = null;
        }

        _isPlaying = false;
    }

    public void ClearQueue()
    {
        lock (_lock) { _queue.Clear(); }
        _stopRequested = true;

        if (_currentPlayer != null)
        {
            _currentPlayer.Stop();
            _currentPlayer.Dispose();
            _currentPlayer = null;
        }
        
        _isPlaying = false;
    }

    private async Task PlayNextAsync()
    {
        _isPlaying = true;
        _stopRequested = false;

        while (true)
        {
            if (_stopRequested) break;
            
            PoiModel? poi;
            lock (_lock)
            {
                if (_queue.Count == 0) break;
                poi = _queue.Dequeue();
            }
            if (poi == null) break;

            var startTime = DateTime.Now;

            // ✅ Ưu tiên 1: phát file mp3 từ GCS nếu có
            var audioUrl = poi.LocalizedAudioUrl;
            if (!string.IsNullOrEmpty(audioUrl))
            {
                System.Diagnostics.Debug.WriteLine($"[Audio] 🎵 Phát file: {audioUrl}");
                await PlayAudioUrlAsync(audioUrl, poi);
            }
            else
            {
                // ✅ Ưu tiên 2: fallback về TTS nếu không có file
                var script = poi.LocalizedTtsScript;
                if (string.IsNullOrEmpty(script))
                {
                    System.Diagnostics.Debug.WriteLine($"[Audio] ⚠️ Không có audio & TTS cho {poi.Name}");
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"[Audio] 🗣️ TTS fallback: {poi.Name}");
                for (int i = 0; i < _repeatCount; i++)
                {
                    if (_stopRequested) break;
                    await _tts.SpeakAsync(script);
                    if (i < _repeatCount - 1 && !_stopRequested)
                        await Task.Delay(1000);
                }
            }

            if (_stopRequested) break;

            var duration = (int)(DateTime.Now - startTime).TotalSeconds;
            System.Diagnostics.Debug.WriteLine($"[Duration] {poi.Name} = {duration}s");

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

    /// <summary>
    /// Download và phát audio file từ URL (GCS) dùng Plugin.Maui.Audio
    /// Repeat theo RepeatCount, giống TTS
    /// </summary>
    private async Task PlayAudioUrlAsync(string url, PoiModel poi)
    {
        try
        {
            // Download file về memory
            var bytes = await _httpClient.GetByteArrayAsync(url);
            if (bytes == null || bytes.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Audio] Download rỗng, fallback TTS");
                await FallbackTtsAsync(poi);
                return;
            }

            for (int i = 0; i < _repeatCount; i++)
            {
                if (_stopRequested) break;

                using var stream = new MemoryStream(bytes);
                _currentPlayer = AudioManager.Current.CreatePlayer(stream);
                _currentPlayer.Play();

                // Chờ phát xong (polling mỗi 200ms)
                while (_currentPlayer != null && _currentPlayer.IsPlaying && !_stopRequested)
                    await Task.Delay(200);

                if (_currentPlayer != null)
                {
                    _currentPlayer.Dispose();
                    _currentPlayer = null;
                }

                if (i < _repeatCount - 1 && !_stopRequested)
                    await Task.Delay(800);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio] Lỗi phát file: {ex.Message}, fallback TTS");
            await FallbackTtsAsync(poi);
        }
    }

    /// <summary>Fallback TTS khi phát file lỗi</summary>
    private async Task FallbackTtsAsync(PoiModel poi)
    {
        var script = poi.LocalizedTtsScript;
        if (string.IsNullOrEmpty(script)) return;

        for (int i = 0; i < _repeatCount; i++)
        {
            if (_stopRequested) break;
            
            await _tts.SpeakAsync(script);
            
            if (i < _repeatCount - 1 && !_stopRequested)
                await Task.Delay(1000);
        }
    }
}