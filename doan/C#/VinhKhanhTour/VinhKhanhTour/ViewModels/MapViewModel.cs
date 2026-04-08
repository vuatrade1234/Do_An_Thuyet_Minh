using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhTour.Data;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;
using VinhKhanhTour.Views;

namespace VinhKhanhTour.ViewModels;

public partial class MapViewModel : ObservableObject
{
    private readonly GpsTrackingService _gps = null!;
    private readonly ApiSyncService _apiSync = null!;
    private readonly AudioQueueService _audioQueue = null!;
    private readonly TtsService _tts = null!;
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly object _lock = new();

    [ObservableProperty] private GpsLocation? currentLocation;
    [ObservableProperty] private List<PoiModel> allPoi = new();
    [ObservableProperty] private PoiModel? nearestPoi;
    [ObservableProperty] private PoiModel? selectedPoi;
    [ObservableProperty] private string statusText = "Nhấn Bắt đầu để khởi động";
    [ObservableProperty] private bool isTracking;
    [ObservableProperty] private string selectedLanguage = "🇻🇳 Tiếng Việt";
    [ObservableProperty] private string startStopText = "▶ Bắt đầu";
    [ObservableProperty] private string mapHtml = string.Empty;
    [ObservableProperty] private string mapUpdateJs = string.Empty;

    // Event để inject JS trực tiếp, bypass MVVM property-equality check
    public event Action<string>? JsInjectionRequested;
    public void InjectJs(string js) => JsInjectionRequested?.Invoke(js);
    [ObservableProperty] private string poiIndexText = "-- / --";
    [ObservableProperty] private string selectedPoiEmoji = "📡";
    [ObservableProperty] private int selectedPoiIndex = 0;
    [ObservableProperty] private bool hasPoiInRadius = false;
    [ObservableProperty] private bool showPoiPanel = false;
    [ObservableProperty] private bool showCancelRoute = false;

    public bool IsMapInitialized { get; set; } = false;

    private double _virtualLat = PoiData.CENTER_LAT;
    private double _virtualLng = PoiData.CENTER_LNG;
    private bool _usingVirtual = false;
    private bool _showingRoute = false;
    private bool _userManualSelected = false;
    private PoiModel? _routeTarget = null;
    private bool _routeInjected = false; // true sau khi đã fetch OSRM xong 1 lần

    // POI IDs đang thực sự trong bán kính (để detect exit)
    private readonly HashSet<string> _poiCurrentlyInRadius = new();
    // POI IDs + thời điểm đọc gần nhất (cooldown chống spam)
    private readonly Dictionary<string, DateTime> _poiAnnouncedThisVisit = new();
    // POI gần nhất ở lần check trước
    private string? _lastNearestPoiId = null;

    // ── Constructor ───────────────────────────────────────────────────────────

    public MapViewModel()
    {
        try
        {
            _tts = TtsService.Instance;
            _audioQueue = new AudioQueueService(_tts);
            _gps = new GpsTrackingService();
            _apiSync = new ApiSyncService();  // ← thêm dòng này

            // Callback ghi analytics với duration thật từ AudioQueueService
            _audioQueue.OnPoiSpoken = (poiId, poiName, lang, duration) =>
            {
                var lat = _usingVirtual ? _virtualLat : CurrentLocation?.Latitude ?? PoiData.CENTER_LAT;
                var lng = _usingVirtual ? _virtualLng : CurrentLocation?.Longitude ?? PoiData.CENTER_LNG;
                _ = _apiSync.LogPoiPlayAsync(poiId, poiName, lang, duration, lat, lng);
            };

            _gps.LocationUpdated += OnLocationUpdated;
            _gps.ErrorOccurred += OnGpsError;

            _loc.LanguageChanged += OnLanguageChanged;
            UpdateLanguageDisplay();

            GenerateInitialMapHtml();

            // Load POI từ API (async, không block UI)
            _ = LoadPoisAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"⚠️ Lỗi khởi tạo: {ex.Message}";
        }
    }

    /// <summary>
    /// Cho phép bên ngoài (DeepLink, QR) yêu cầu phát thuyết minh POI
    /// </summary>
    public void EnqueuePoiForSpeak(Models.PoiModel poi)
    {
        _audioQueue.Enqueue(poi);
    }

    public void LoadTourById(string tourId)
    {
        if (string.IsNullOrWhiteSpace(tourId)) return;

        // Dùng AllPoi đã load từ API (không dùng PoiData.GetAllPoi() nữa)
        var source = AllPoi.Count > 0 ? AllPoi : new();

        var filtered = source
            .Where(p => p.TourIds != null && p.TourIds.Contains(tourId))
            .ToList();

        // Nếu không có POI nào khớp → dùng toàn bộ
        var pois = filtered.Count > 0 ? filtered : source;

        AllPoi = pois;
        SelectedPoiIndex = 0;
        SelectedPoi = AllPoi.FirstOrDefault();
        UpdatePoiIndexPublic();
        GenerateInitialMapHtml();

        StatusText = filtered.Count > 0
            ? $"🗺️ Tour loaded: {filtered.Count} điểm"
            : "🗺️ Hiển thị toàn bộ điểm tham quan";
    }

    // Wrapper public cho UpdatePoiIndex (vốn là private)
    public void UpdatePoiIndexPublic()
    {
        if (SelectedPoi == null)
        {
            PoiIndexText = "-- / --";
            SelectedPoiEmoji = "📡";
            return;
        }
        PoiIndexText = $"{SelectedPoiIndex + 1} / {AllPoi.Count}";
        SelectedPoiEmoji = SelectedPoi.Category switch
        {
            "food" => "🍜",
            "drink" => "🥤",
            "landmark" => "🏛",
            _ => "📍"
        };
        foreach (var p in AllPoi)
            p.IsHighlighted = p.Id == SelectedPoi.Id;
    }

    private async Task LoadPoisAsync()
    {
        try
        {
            var pois = await _apiSync.GetPoisAsync();
            if (pois.Count > 0)
            {
                AllPoi = pois;

                // Set lại virtual location về cổng vào
                var entrance = AllPoi.FirstOrDefault(p => p.Id == "poi_01");
                if (entrance != null)
                {
                    _virtualLat = entrance.Latitude;
                    _virtualLng = entrance.Longitude;
                    _usingVirtual = true;
                }

                // Reload map với POI mới
                GenerateInitialMapHtml();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapVM] LoadPois lỗi: {ex.Message}");
        }
    }

    // ── Destructor ────────────────────────────────────────────────────────────

    ~MapViewModel()
    {
        _loc.LanguageChanged -= OnLanguageChanged;
        _gps.LocationUpdated -= OnLocationUpdated;
        _gps.ErrorOccurred -= OnGpsError;
    }

    // ── Language ──────────────────────────────────────────────────────────────

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateLanguageDisplay();
            GenerateInitialMapHtml();
        });
    }

    private void UpdateLanguageDisplay()
    {
        SelectedLanguage = _loc.FlagAndName;
        StartStopText = IsTracking ? _loc.Get("stop") : _loc.Get("start");
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task StartTourAsync()
    {
        // Reset trạng thái POI — bắt đầu tour mới từ đầu
        _poiCurrentlyInRadius.Clear();
        _poiAnnouncedThisVisit.Clear();
        _lastNearestPoiId = null;

        // Về lại cổng
        var entrance = AllPoi.FirstOrDefault(p => p.Id == "poi_01");
        if (entrance != null)
        {
            _virtualLat = entrance.Latitude;
            _virtualLng = entrance.Longitude;
            _usingVirtual = true;
        }

        // Start background GPS service
#if ANDROID
        var startIntent = new Android.Content.Intent(
            Android.App.Application.Context,
            typeof(VinhKhanhTour.Platforms.Android.GpsBackgroundService));
        Android.App.Application.Context.StartForegroundService(startIntent);
#endif

        StatusText = _loc.Get("gps_connecting");
        StartStopText = _loc.Get("stop");
        await _gps.StartTracking();
        IsTracking = true;
        StatusText = _loc.Get("gps_tracking");
    }

    [RelayCommand]
    public void StopTour()
    {
        _gps.StopTracking();
        _audioQueue.ClearQueue();
        IsTracking = false;
        StartStopText = _loc.Get("start");
        StatusText = _loc.Get("gps_stopped");

        // Stop background GPS service
#if ANDROID
        var stopIntent = new Android.Content.Intent(
            Android.App.Application.Context,
            typeof(VinhKhanhTour.Platforms.Android.GpsBackgroundService));
        Android.App.Application.Context.StopService(stopIntent);
#endif
    }

    [RelayCommand]
    public void ToggleLanguage() { /* dùng ActionSheet trong MapPage */ }

    // ── Hủy đường ─────────────────────────────────────────────────────────────

    public void CancelRoute()
    {
        _showingRoute = false;
        _userManualSelected = false;
        _routeTarget = null;
        _routeInjected = false;
        ShowCancelRoute = false;
        MapUpdateJs = "clearRoute();" + BuildMarkersJs();
        StatusText = _loc.Get("gps_tracking");
    }

    // ── POI Navigation ────────────────────────────────────────────────────────

    public void SelectNextPoi()
    {
        if (AllPoi.Count == 0) return;
        _userManualSelected = true;
        _showingRoute = false;
        SelectedPoiIndex = (SelectedPoiIndex + 1) % AllPoi.Count;
        SetDisplayPoi(AllPoi[SelectedPoiIndex]);
        ShowRouteToPoi(AllPoi[SelectedPoiIndex]);
    }

    public void SelectPrevPoi()
    {
        if (AllPoi.Count == 0) return;
        _userManualSelected = true;
        _showingRoute = false;
        SelectedPoiIndex = (SelectedPoiIndex - 1 + AllPoi.Count) % AllPoi.Count;
        SetDisplayPoi(AllPoi[SelectedPoiIndex]);
        ShowRouteToPoi(AllPoi[SelectedPoiIndex]);
    }

    private void SetDisplayPoi(PoiModel poi)
    {
        SelectedPoi = poi;
        SelectedPoiIndex = AllPoi.IndexOf(poi);
        ShowPoiPanel = true;
        PoiIndexText = $"{SelectedPoiIndex + 1} / {AllPoi.Count}";
        SelectedPoiEmoji = poi.Category switch
        {
            "food" => "🍜",
            "drink" => "🥤",
            "landmark" => "🏛",
            _ => "📍"
        };
    }

    // ── Route ─────────────────────────────────────────────────────────────────

    public void ShowRouteToPoi(PoiModel poi)
    {
        var realPoi = AllPoi.FirstOrDefault(p => p.Id == poi.Id) ?? poi;
        _showingRoute = true;
        _userManualSelected = true;
        _routeTarget = realPoi;
        _routeInjected = false; // reset để fetch lại cho POI mới
        ShowCancelRoute = true;

        SetDisplayPoi(realPoi);
        StatusText = $"{_loc.Get("navigating_to")} {realPoi.Name}...";
        InjectRouteWithMarkers();
    }

    public void FlyToCurrentLocation()
    {
        _showingRoute = false;
        _userManualSelected = false;
        _usingVirtual = false;
        var lat = (CurrentLocation?.Latitude ?? PoiData.CENTER_LAT)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lng = (CurrentLocation?.Longitude ?? PoiData.CENTER_LNG)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        MapUpdateJs = $"map.flyTo([{lat},{lng}],18);";
    }

    // ── D-pad ─────────────────────────────────────────────────────────────────

    public void MoveVirtualLocation(double dLat, double dLng)
    {
        _usingVirtual = true;
        _virtualLat += dLat;
        _virtualLng += dLng;

        ProcessLocation(new GpsLocation
        {
            Latitude = _virtualLat,
            Longitude = _virtualLng,
            Accuracy = 5
        });

        var uLat = _virtualLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var uLng = _virtualLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
        MapUpdateJs = $"updateUserMarker({uLat},{uLng},true);map.panTo([{uLat},{uLng}]);";
        StatusText = $"🕹 {_virtualLat:F5}, {_virtualLng:F5}";
    }

    public void ResetVirtualLocation()
    {
        _usingVirtual = false;
        _userManualSelected = false;
        _virtualLat = CurrentLocation?.Latitude ?? PoiData.CENTER_LAT;
        _virtualLng = CurrentLocation?.Longitude ?? PoiData.CENTER_LNG;
        StatusText = _loc.Get("reset_gps");
        MapUpdateJs = "clearRoute();" + BuildMarkersJs();
    }

    // ── FIX CORE: CheckPoiProximity ───────────────────────────────────────────
    // - Cooldown riêng từng POI → POI gần nhau không block nhau
    // - Chỉ trigger khi BƯỚC VÀO radius (không trigger khi đứng trong mãi)
    // - Ra khỏi radius → reset → lần vào tiếp theo trigger lại bình thường

    private void CheckPoiProximity(double lat, double lng)
    {
        var radius = SettingsPage.GlobalRadius;

        // ── 1. Tính khoảng cách tất cả POI ────────────────────────────────
        var distMap = new Dictionary<string, double>();
        var insideNow = new HashSet<string>();

        foreach (var poi in AllPoi.Where(p => p.IsActive))
        {
            var rad = poi.RadiusMeters > 0 ? poi.RadiusMeters : radius;
            var dist = GpsTrackingService.CalculateDistance(lat, lng, poi.Latitude, poi.Longitude);
            poi.DistanceFromUser = dist;
            if (dist <= rad) { insideNow.Add(poi.Id); distMap[poi.Id] = dist; }
        }

        // ── 2. Ra khỏi tất cả bán kính → dừng đọc ngay ───────────────────
        bool wasInside = _poiCurrentlyInRadius.Count > 0;
        _poiCurrentlyInRadius.IntersectWith(insideNow); // giữ lại POI vẫn còn trong radius

        if (wasInside && _poiCurrentlyInRadius.Count == 0)
        {
            _lastNearestPoiId = null;
            _audioQueue.StopImmediate();
            MainThread.BeginInvokeOnMainThread(() =>
                StatusText = _loc.Get("outside_zone"));
        }

        if (insideNow.Count == 0) return;

        // Cập nhật set
        foreach (var id in insideNow) _poiCurrentlyInRadius.Add(id);

        // ── 3. Tìm POI gần nhất trong bán kính ────────────────────────────
        var nearest = AllPoi
            .Where(p => insideNow.Contains(p.Id))
            .OrderBy(p => distMap[p.Id])
            .FirstOrDefault();

        if (nearest == null) return;

        // ── 4. Nếu nearest không đổi → không làm gì ───────────────────────
        if (nearest.Id == _lastNearestPoiId) return;
        _lastNearestPoiId = nearest.Id;

        // ── 5. Cooldown: nếu đã đọc POI này rồi, kiểm tra đã hết cooldown chưa
        //    Mới vào lần đầu (_poiAnnouncedThisVisit không có) → đọc NGAY không cần chờ
        if (_poiAnnouncedThisVisit.TryGetValue(nearest.Id, out var lastRead))
        {
            var elapsed = (DateTime.Now - lastRead).TotalSeconds;
            if (elapsed < SettingsPage.CooldownSeconds) return; // chưa hết cooldown
        }

        // ── 6. Announce ────────────────────────────────────────────────────
        _poiAnnouncedThisVisit[nearest.Id] = DateTime.Now;

        if (!_userManualSelected)
        {
            var cap = nearest;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusText = $"📍 {cap.Name}";
                SetDisplayPoi(cap);
                ShowPoiPanel = true;
            });
        }

        // Ghi analytics với duration thật
        var capturedPoi = nearest;
        var capturedLang = _loc.CurrentLocale.ToString().ToLower();
        var startTime = DateTime.Now;

        void OnSpeechDone()
        {
            _tts.SpeechCompleted -= OnSpeechDone;
            var duration = (int)(DateTime.Now - startTime).TotalSeconds;
            _ = _apiSync.LogPoiPlayAsync(capturedPoi.Id, capturedPoi.Name, capturedLang, duration);
        }
        _tts.SpeechCompleted += OnSpeechDone;

        _audioQueue.Enqueue(nearest);
    }

    // ── Location Processing ───────────────────────────────────────────────────

    private void ProcessLocation(GpsLocation loc)
    {
        List<PoiModel> snapshot;
        lock (_lock) { snapshot = AllPoi.ToList(); }

        var radius = SettingsPage.GlobalRadius;

        // Tính khoảng cách
        foreach (var poi in snapshot)
        {
            poi.DistanceFromUser = GpsTrackingService.CalculateDistance(
                loc.Latitude, loc.Longitude,
                poi.Latitude, poi.Longitude);
        }

        // Đồng bộ vào AllPoi
        foreach (var sp in snapshot)
        {
            var o = AllPoi.FirstOrDefault(p => p.Id == sp.Id);
            if (o != null) o.DistanceFromUser = sp.DistanceFromUser;
        }

        var effectiveRadius = (PoiModel p) => p.RadiusMeters > 0 ? p.RadiusMeters : radius;
        var inRadius = snapshot
            .Where(p => p.IsActive && p.DistanceFromUser <= effectiveRadius(p))
            .OrderBy(p => p.DistanceFromUser)
            .ToList();

        var nearest = inRadius.FirstOrDefault();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            NearestPoi = nearest != null ? AllPoi.FirstOrDefault(p => p.Id == nearest.Id) : null;
            HasPoiInRadius = inRadius.Any();

            if (SelectedPoi != null)
            {
                var r = AllPoi.FirstOrDefault(p => p.Id == SelectedPoi.Id);
                if (r != null) SelectedPoi = r;
            }

            if (!_userManualSelected && !_showingRoute)
            {
                if (NearestPoi != null)
                {
                    if (SelectedPoi?.Id != NearestPoi.Id) SetDisplayPoi(NearestPoi);
                    ShowPoiPanel = true;
                }
                else
                {
                    SelectedPoi = null;
                    ShowPoiPanel = false;
                    PoiIndexText = "-- / --";
                    SelectedPoiEmoji = "📡";
                }
            }

            if (_userManualSelected) ShowPoiPanel = true;

            InjectMarkersUpdate();
        });

        // Kiểm tra trigger TTS — chạy trên thread hiện tại (không block UI)
        CheckPoiProximity(loc.Latitude, loc.Longitude);
    }

    private void OnLocationUpdated(GpsLocation loc)
    {
        if (_usingVirtual) return;
        CurrentLocation = loc;
        ProcessLocation(loc);
        if (!_showingRoute && !_userManualSelected)
            StatusText = $"🟢 ±{loc.Accuracy:F0}m";
    }

    private void OnGpsError(string error)
        => MainThread.BeginInvokeOnMainThread(() => StatusText = $"⚠️ {error}");

    // ── Build Markers JS ──────────────────────────────────────────────────────

    private string BuildMarkersJs()
    {
        List<PoiModel> snapshot;
        lock (_lock) { snapshot = AllPoi.ToList(); }

        var radius = SettingsPage.GlobalRadius;
        var js = new System.Text.StringBuilder();
        js.Append("clearMarkers();");

        foreach (var p in snapshot)
        {
            var lat = p.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lng = p.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var effectR = p.RadiusMeters > 0 ? p.RadiusMeters : radius;
            var rad = effectR.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var isSel = SelectedPoi?.Id == p.Id;
            var isNear = NearestPoi?.Id == p.Id;
            var inRad = p.DistanceFromUser.HasValue && p.DistanceFromUser <= effectR;

            // Updated modern colors
            var color = isSel ? "#E65100" : isNear ? "#FF9800" : inRad ? "#4CAF50" : "#1976D2";
            var size = isSel ? 48 : isNear ? 42 : 36;
            var anchor = size / 2;
            var fs = isSel ? 20 : isNear ? 18 : 15;
            var em = p.Category == "food" ? "🍜" : p.Category == "drink" ? "🥤" : "🏛";
            var dist = p.DistanceFromUser.HasValue
                ? (p.DistanceFromUser < 1000 ? $"{p.DistanceFromUser:F0}m" : $"{p.DistanceFromUser / 1000:F1}km")
                : "?";
            var border = isSel ? "box-shadow: 0 0 10px rgba(230,81,0,0.8)" : "box-shadow: 2px 2px 6px rgba(0,0,0,0.4)";
            
            var safeName = p.LocalizedName.Replace("'", "\\'");
            var safeId = p.Id.Replace("'", "\\'");
            var navText = _loc.Get("navigate_in_app").Replace("'", "\\'");

            js.Append($"addPoiMarker({lat},{lng},{rad},'{color}',{size},{anchor},{fs}," +
                      $"'{em}','{safeName}','{dist}','{border}'," +
                      $"{(isSel ? "true" : "false")},'{safeId}','{navText}');");
        }

        var uLat = (_usingVirtual ? _virtualLat : CurrentLocation?.Latitude ?? PoiData.CENTER_LAT)
                            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var uLng = (_usingVirtual ? _virtualLng : CurrentLocation?.Longitude ?? PoiData.CENTER_LNG)
                            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var testLocText = _loc.Get("test_location").Replace("'", "\\'");
        var myLocText = _loc.Get("my_location").Replace("'", "\\'");
        js.Append($"updateUserMarker({uLat},{uLng},{(_usingVirtual ? "true" : "false")},'{testLocText}','{myLocText}');");

        return js.ToString();
    }

    // ── Inject ────────────────────────────────────────────────────────────────

    public void InjectMarkersUpdate()
    {
        if (_showingRoute)
        {
            if (!_routeInjected)
            {
                // Lần đầu: fetch OSRM + vẽ full
                InjectRouteWithMarkers();
            }
            else
            {
                // Các lần sau (GPS update): chỉ update vị trí user, KHÔNG xóa route
                var uLat2 = (_usingVirtual ? _virtualLat : CurrentLocation?.Latitude ?? PoiData.CENTER_LAT)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
                var uLng2 = (_usingVirtual ? _virtualLng : CurrentLocation?.Longitude ?? PoiData.CENTER_LNG)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
                var testLocText2 = _loc.Get("test_location").Replace("'", "\'");
                var myLocText2 = _loc.Get("my_location").Replace("'", "\'");
                MapUpdateJs = $"updateUserMarker({uLat2},{uLng2},{(_usingVirtual ? "true" : "false")},'{testLocText2}','{myLocText2}');";
            }
            return;
        }
        MapUpdateJs = "clearRoute();" + BuildMarkersJs();
    }

    private void InjectRouteWithMarkers()
    {
        if (_routeTarget == null) return;

        var uLat = (_usingVirtual ? _virtualLat : CurrentLocation?.Latitude ?? PoiData.CENTER_LAT)
                     .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var uLng = (_usingVirtual ? _virtualLng : CurrentLocation?.Longitude ?? PoiData.CENTER_LNG)
                     .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var rLat = _routeTarget.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var rLng = _routeTarget.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var safeName = _routeTarget.LocalizedName.Replace("'", "\\\'");
        var minText = _loc.Get("route_minutes");

        _routeInjected = true;

        // Bước 1a: Xóa route cũ + update markers (chuỗi riêng)
        MapUpdateJs = "clearRoute();" + BuildMarkersJs();

        // Bước 1b: Vẽ đường thẳng ngay — chuỗi ngắn gọn, luôn hoạt động
        var jsLine =
            $"routeLine=L.polyline([[{uLat},{uLng}],[{rLat},{rLng}]]," +
            "{color:'#1A237E',weight:5,opacity:0.8,dashArray:'10,6'}).addTo(map);" +
            $"map.fitBounds([[{uLat},{uLng}],[{rLat},{rLng}]],{{padding:[60,60]}});" +
            $"L.popup().setLatLng([{rLat},{rLng}])" +
            $".setContent('<b>🧭 {safeName}</b>').openOn(map);";

        // Inject line sau 300ms để markers kịp render trước
        _ = Task.Delay(300).ContinueWith(_ =>
            MainThread.BeginInvokeOnMainThread(() => InjectJs(jsLine)));

        // Bước 2: Thử fetch OSRM để vẽ đường thực tế (chạy riêng, không block)
        _ = TryFetchOsrmRouteAsync(uLat, uLng, rLat, rLng, safeName, minText);
    }

    private async Task TryFetchOsrmRouteAsync(
        string uLat, string uLng, string rLat, string rLng,
        string safeName, string minText)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            // Thêm User-Agent để API định tuyến OSRM không từ chối kết nối (Nguyên nhân chính gây lỗi vẽ đường thẳng)
            http.DefaultRequestHeaders.Add("User-Agent", "VinhKhanhTourApp/1.0"); 
            
            var url = $"https://router.project-osrm.org/route/v1/foot/" +
                      $"{uLng},{uLat};{rLng},{rLat}?overview=full&geometries=geojson";
            
            var resp = await http.GetStringAsync(url);

            // Sử dụng JsonDocument chuẩn hóa để không bị lỗi khi cấu trúc JSON thay đổi (Thuật toán Shortest Path của OSRM)
            using var doc = System.Text.Json.JsonDocument.Parse(resp);
            var routes = doc.RootElement.GetProperty("routes");
            if (routes.GetArrayLength() == 0) return;

            var coordsArray = routes[0].GetProperty("geometry").GetProperty("coordinates");
            
            // Đổi từ [lng, lat] (Quy chuẩn OSRM) sang [lat, lng] (Quy chuẩn Leaflet Map)
            var sb = new System.Text.StringBuilder("[");
            foreach (var coord in coordsArray.EnumerateArray())
            {
                var currentLng = coord[0].GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
                var currentLat = coord[1].GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
                sb.Append($"[{currentLat},{currentLng}],");
            }
            if (sb.Length > 1) sb.Length--; // Bỏ dấu phẩy cuối
            sb.Append("]");
            
            var coordsJs = sb.ToString();

            if (!_showingRoute || _routeTarget == null) return;

            var jsUpdate =
                "clearRoute();" +
                $"routeLine=L.polyline({coordsJs}," +
                "{color:'#1A237E',weight:5,opacity:0.85,dashArray:'10,5'}).addTo(map);" +
                $"L.popup().setLatLng([{rLat},{rLng}])" +
                $".setContent('<b>🧭 {safeName}</b>').openOn(map);" +
                $"map.fitBounds(routeLine.getBounds(),{{padding:[40,40]}});";

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_showingRoute)
                    MapUpdateJs = jsUpdate;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OSRM routing error: {ex.Message}");
            // Bị lỗi kết nối, Map sẽ tự động dùng đường chim bay (Fallback)
        }
    }

    // ── Map HTML ──────────────────────────────────────────────────────────────

    public void GenerateInitialMapHtml()
    {
        // Khi map được tạo lại (đổi ngôn ngữ), route cần vẽ lại
        _routeInjected = false;

        // Mở map tập trung vào cổng vào
        var entrance = AllPoi.FirstOrDefault(p => p.Id == "poi_01");
        var focusLat = (entrance?.Latitude ?? PoiData.CENTER_LAT)
                         .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var focusLng = (entrance?.Longitude ?? PoiData.CENTER_LNG)
                         .ToString(System.Globalization.CultureInfo.InvariantCulture);

        var initMarkers = new System.Text.StringBuilder();
        foreach (var p in AllPoi)
        {
            var lat = p.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lng = p.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var effectR = p.RadiusMeters > 0 ? p.RadiusMeters : SettingsPage.GlobalRadius;
            var rad = effectR.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var em = p.Category == "food" ? "🍜" : p.Category == "drink" ? "🥤" : "🏛";
            var safeName = p.LocalizedName.Replace("'", "\\'");
            var safeId = p.Id.Replace("'", "\\'");
            var navText = _loc.Get("navigate_in_app").Replace("'", "\\'");

            initMarkers.Append(
                $"addPoiMarker({lat},{lng},{rad},'#1976D2',36,18,15," +
                $"'{em}','{safeName}','?','box-shadow: 2px 2px 6px rgba(0,0,0,0.4)',false,'{safeId}','{navText}');");
        }

        var testLocText = _loc.Get("test_location").Replace("'", "\\'");
        var myLocText = _loc.Get("my_location").Replace("'", "\\'");

        MapHtml =
            "<html><head>" +
            "<meta charset='utf-8'/>" +
            "<meta name='viewport' content='width=device-width,initial-scale=1'/>" +
            "<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>" +
            "<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>" +
            "<style>" +
            "*{margin:0;padding:0}" +
            "body,#map{width:100vw;height:100vh}" +
            ".leaflet-popup-content b{color:#1A237E}" +
            ".leaflet-popup-content{font-family:sans-serif;font-size:13px;line-height:1.8}" +
            ".btn-route{margin-top:6px;padding:6px 14px;background:#1A237E;color:white;" +
            "border:none;border-radius:8px;font-size:13px;cursor:pointer;width:100%}" +
            ".custom-pin{position:relative;z-index:999;}" +
            ".pin-body{position:absolute;top:0;left:0;width:100%;height:100%;" +
            "border-radius:50% 50% 50% 0;transform:rotate(-45deg);border: 2px solid white;}" +
            ".pin-inner{position:absolute;top:10%;left:10%;width:80%;height:80%;" +
            "border-radius:50%;background:white;transform:rotate(45deg);" +
            "display:flex;align-items:center;justify-content:center;box-shadow:inset 0 1px 3px rgba(0,0,0,0.3);}" +
            "@keyframes pulse-ring { 0%{transform:scale(0.8);box-shadow:0 0 0 0 rgba(230,81,0,0.8);} " +
            "70%{transform:scale(1.1);box-shadow:0 0 0 15px rgba(230,81,0,0);} " +
            "100%{transform:scale(0.8);box-shadow:0 0 0 0 rgba(230,81,0,0);} }" +
            ".pin-pulse .pin-body { animation: pulse-ring 2s infinite; border: 3px solid white; z-index:1000;}" +
            "</style>" +
            "</head><body><div id='map'></div><script>" +

            $"var map=L.map('map',{{zoomControl:false}}).setView([{focusLat},{focusLng}],18);" +
            "L.control.zoom({position:'topright'}).addTo(map);" +
            "L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png'," +
            "{maxZoom:19,attribution:''}).addTo(map);" +
            "var poiMarkers=[];var poiCircles=[];var userMarker=null;var routeLine=null;" +

            "function addPoiMarker(lat,lng,rad,color,size,anchor,fontSize,em,name,dist,border,isSelected,poiId,navText){" +
            "  var c=L.circle([lat,lng],{radius:rad,color:color," +
            "    fillOpacity:isSelected?0.2:0.08,weight:isSelected?2.5:1.5," +
            "    dashArray:isSelected?'6,6':''}).addTo(map);" +
            "  poiCircles.push(c);" +
            "  var htmlStr = '<div class=\"custom-pin '+(isSelected?'pin-pulse':'')+'\" style=\"width:'+size+'px;height:'+size+'px;\">'+" +
            "    '<div class=\"pin-body\" style=\"background:'+color+';'+border+'\"></div>'+" +
            "    '<div class=\"pin-inner\" style=\"font-size:'+fontSize+'px;\">'+em+'</div>'+" +
            "  '</div>';" +
            "  var m=L.marker([lat,lng],{icon:L.divIcon({" +
            "    className:''," +
            "    html:htmlStr," +
            "    iconSize:[size,size]," +
            "    iconAnchor:[size/2,size]})}).addTo(map);" +
            "  var popHtml='<b>'+name+'</b><br>📏 '+dist+" +
            "    '<br><button class=\"btn-route\" " +
            "    onclick=\"window.location=\\'mauibridge://showroute?id='+encodeURIComponent(poiId)+'\\'\">'+" +
            "    navText+'</button>';" +
            "  m.bindPopup(popHtml,{minWidth:180});" +
            "  m.on('click',function(){m.openPopup();});" +
            "  poiMarkers.push(m);" +
            "}" +

            "function clearMarkers(){" +
            "  poiMarkers.forEach(function(m){map.removeLayer(m);});" +
            "  poiCircles.forEach(function(c){map.removeLayer(c);});" +
            "  poiMarkers=[];poiCircles=[];" +
            "}" +

            "function updateUserMarker(lat,lng,isVirtual,testText,myText){" +
            "  if(userMarker)map.removeLayer(userMarker);" +
            "  var d=document.createElement('div');" +
            "  d.style.cssText='width:22px;height:22px;border-radius:50%;background:'+" +
            "    (isVirtual?'#FF9800':'#4CAF50')+';border:3px solid white;" +
            "    box-shadow:0 0 0 6px '+(isVirtual?'rgba(255,152,0,0.3)':'rgba(76,175,80,0.25)')+';';" +
            "  userMarker=L.marker([lat,lng],{icon:L.divIcon({" +
            "    html:d.outerHTML,iconSize:[22,22],iconAnchor:[11,11]})}).addTo(map);" +
            "  userMarker.bindPopup(isVirtual?testText:myText);" +
            "}" +

            "function clearRoute(){" +
            "  if(routeLine){map.removeLayer(routeLine);routeLine=null;}" +
            "  map.eachLayer(function(l){" +
            "    if(l instanceof L.Polyline)map.removeLayer(l);" +
            "  });" +
            "  map.closePopup();" +
            "}" +

            initMarkers.ToString() +
            $"updateUserMarker({focusLat},{focusLng},true,'{testLocText}','{myLocText}');" +
            "</script></body></html>";
    }

    public void GenerateMapHtml() => GenerateInitialMapHtml();
}