using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhTour.Data;
using VinhKhanhTour.Models;
using VinhKhanhTour.Services;
using VinhKhanhTour.Views;

namespace VinhKhanhTour.ViewModels;

public partial class MapViewModel : ObservableObject
{
    private readonly GpsTrackingService _gps;
    private readonly GeofenceService _geofence;
    private readonly AudioQueueService _audioQueue;
    private readonly TtsService _tts;
    private readonly object _lock = new object();

    [ObservableProperty] private GpsLocation? currentLocation;
    [ObservableProperty] private List<PoiModel> allPoi = PoiData.GetAllPoi();
    [ObservableProperty] private PoiModel? nearestPoi;
    [ObservableProperty] private PoiModel? selectedPoi;
    [ObservableProperty] private string statusText = "Nhấn Bắt đầu để khởi động";
    [ObservableProperty] private bool isTracking;
    [ObservableProperty] private string selectedLanguage = "🇻🇳 VI";
    [ObservableProperty] private string startStopText = "▶ Bắt đầu";
    [ObservableProperty] private string mapHtml = string.Empty;
    [ObservableProperty] private string mapUpdateJs = string.Empty;
    [ObservableProperty] private string poiIndexText = "-- / --";
    [ObservableProperty] private string selectedPoiEmoji = "📡";
    [ObservableProperty] private int selectedPoiIndex = 0;
    [ObservableProperty] private bool hasPoiInRadius = false;

    public bool IsMapInitialized { get; set; } = false;

    private double _virtualLat = PoiData.CENTER_LAT;
    private double _virtualLng = PoiData.CENTER_LNG;
    private bool _usingVirtual = false;
    private bool _showingRoute = false;
    private PoiModel? _routeTarget = null;

    public MapViewModel()
    {
        try
        {
            _tts = TtsService.Instance;
            _audioQueue = new AudioQueueService(_tts);
            _gps = new GpsTrackingService();
            _geofence = new GeofenceService();

            _gps.LocationUpdated += OnLocationUpdated;
            _gps.ErrorOccurred += OnGpsError;
            _geofence.PoiTriggered += OnPoiTriggered;

            _geofence.StopSpeaking += () =>
            {
                _audioQueue.StopImmediate();
                MainThread.BeginInvokeOnMainThread(() =>
                    StatusText = "⭕ Ra khỏi vùng thuyết minh");
            };

            SelectedPoi = AllPoi.FirstOrDefault();
            SelectedPoiIndex = 0;
            PoiIndexText = $"1 / {AllPoi.Count}";
            SelectedPoiEmoji = "🍜";

            GenerateInitialMapHtml();
        }
        catch (Exception ex)
        {
            StatusText = $"⚠️ Lỗi: {ex.Message}";
        }
    }

    // ── Commands ──────────────────────────────────────────────────────

    [RelayCommand]
    public async Task StartTourAsync()
    {
        StatusText = "🔄 Đang kết nối GPS...";
        StartStopText = "⏹ Dừng";
        await _gps.StartTracking();
        IsTracking = true;
        StatusText = "🟢 Đang theo dõi vị trí...";
    }

    [RelayCommand]
    public void StopTour()
    {
        _gps.StopTracking();
        _audioQueue.ClearQueue();
        IsTracking = false;
        StartStopText = "▶ Bắt đầu";
        StatusText = "⏹ Đã dừng";
    }

    [RelayCommand]
    public void ToggleLanguage()
    {
        if (_tts.CurrentLanguage == AppLanguage.Vietnamese)
        {
            _tts.CurrentLanguage = AppLanguage.English;
            _audioQueue.SetLanguage(AppLanguage.English);
            SelectedLanguage = "🇬🇧 EN";
        }
        else
        {
            _tts.CurrentLanguage = AppLanguage.Vietnamese;
            _audioQueue.SetLanguage(AppLanguage.Vietnamese);
            SelectedLanguage = "🇻🇳 VI";
        }
    }

    // ── POI Navigation ────────────────────────────────────────────────

    public void SelectNextPoi()
    {
        if (AllPoi.Count == 0) return;
        SelectedPoiIndex = (SelectedPoiIndex + 1) % AllPoi.Count;
        SelectedPoi = AllPoi[SelectedPoiIndex];
        _showingRoute = false;
        UpdatePoiIndex();
        InjectMarkersUpdate();
    }

    public void SelectPrevPoi()
    {
        if (AllPoi.Count == 0) return;
        SelectedPoiIndex = (SelectedPoiIndex - 1 + AllPoi.Count) % AllPoi.Count;
        SelectedPoi = AllPoi[SelectedPoiIndex];
        _showingRoute = false;
        UpdatePoiIndex();
        InjectMarkersUpdate();
    }

    public void FlyToCurrentLocation()
    {
        _showingRoute = false;
        _usingVirtual = false;
        var lat = (CurrentLocation?.Latitude ?? PoiData.CENTER_LAT)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lng = (CurrentLocation?.Longitude ?? PoiData.CENTER_LNG)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        MapUpdateJs = $"map.flyTo([{lat},{lng}],18);";
    }

    public void ShowRouteToPoi(PoiModel poi)
    {
        _showingRoute = true;
        _routeTarget = poi;
        StatusText = $"🧭 Đang dẫn đến {poi.Name}...";
        InjectRouteJs();
    }

    // ── D-pad ─────────────────────────────────────────────────────────

    public void MoveVirtualLocation(double dLat, double dLng)
    {
        _usingVirtual = true;
        _virtualLat += dLat;
        _virtualLng += dLng;

        var loc = new GpsLocation
        {
            Latitude = _virtualLat,
            Longitude = _virtualLng,
            Accuracy = 5
        };

        ProcessLocation(loc);

        var uLat = _virtualLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var uLng = _virtualLng.ToString(System.Globalization.CultureInfo.InvariantCulture);

        MapUpdateJs = $"updateUserMarker({uLat},{uLng},true);map.panTo([{uLat},{uLng}]);";
        StatusText = $"🕹 {_virtualLat:F5}, {_virtualLng:F5}";
    }

    public void ResetVirtualLocation()
    {
        _usingVirtual = false;
        _virtualLat = CurrentLocation?.Latitude ?? PoiData.CENTER_LAT;
        _virtualLng = CurrentLocation?.Longitude ?? PoiData.CENTER_LNG;
        StatusText = "📍 Reset về GPS thật";
        InjectMarkersUpdate();
    }

    private void UpdatePoiIndex()
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

    // ── JS Injection ──────────────────────────────────────────────────

    public void InjectMarkersUpdate()
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
            var rad = radius.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var isSelected = SelectedPoi?.Id == p.Id;
            var isNearest = NearestPoi?.Id == p.Id;
            var inRadius = p.DistanceFromUser.HasValue && p.DistanceFromUser <= radius;

            var color = isSelected ? "#FF5722"
                       : isNearest ? "#FF9800"
                       : inRadius ? "#4CAF50"
                       : "#2196F3";
            var size = isSelected ? 44 : isNearest ? 38 : 32;
            var anchor = size / 2;
            var fs = isSelected ? 22 : isNearest ? 18 : 15;
            var em = p.Category == "food" ? "🍜"
                       : p.Category == "drink" ? "🥤" : "🏛";
            var dist = p.DistanceFromUser.HasValue
                       ? $"{p.DistanceFromUser:F0}m" : "?";
            var border = isSelected ? "4px solid #BF360C"
                       : isNearest ? "3px solid #E65100"
                       : "3px solid white";

            js.Append($"addPoiMarker({lat},{lng},{rad},'{color}',{size},{anchor},{fs},'{em}','{p.Name}','{dist}','{border}',{(isSelected ? "true" : "false")});");
        }

        // Cập nhật user marker
        var uLat = (_usingVirtual ? _virtualLat : CurrentLocation?.Latitude ?? PoiData.CENTER_LAT)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var uLng = (_usingVirtual ? _virtualLng : CurrentLocation?.Longitude ?? PoiData.CENTER_LNG)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        js.Append($"updateUserMarker({uLat},{uLng},{(_usingVirtual ? "true" : "false")});");

        MapUpdateJs = js.ToString();
    }

    private void InjectRouteJs()
    {
        if (_routeTarget == null) return;

        var uLat = (_usingVirtual ? _virtualLat : CurrentLocation?.Latitude ?? PoiData.CENTER_LAT)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var uLng = (_usingVirtual ? _virtualLng : CurrentLocation?.Longitude ?? PoiData.CENTER_LNG)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var rLat = _routeTarget.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var rLng = _routeTarget.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

        MapUpdateJs =
            "clearRoute();" +
            $"fetch('https://router.project-osrm.org/route/v1/foot/{uLng},{uLat};{rLng},{rLat}?overview=full&geometries=geojson')" +
            ".then(function(r){return r.json()})" +
            ".then(function(data){" +
            "if(data.routes&&data.routes.length>0){" +
            "var coords=data.routes[0].geometry.coordinates.map(function(c){return[c[1],c[0]];});" +
            "var dist=Math.round(data.routes[0].distance);" +
            "var dur=Math.round(data.routes[0].duration/60);" +
            "routeLine=L.polyline(coords,{color:'#1A237E',weight:5,opacity:0.85,dashArray:'10,5'}).addTo(map);" +
            $"L.popup().setLatLng([{rLat},{rLng}])" +
            ".setContent('<b>🧭 Đang dẫn đường</b><br>📏 '+dist+'m | ⏱ ~'+dur+' phút')" +
            ".openOn(map);" +
            "map.fitBounds(L.latLngBounds(coords),{padding:[40,40]});" +
            "}}).catch(function(e){console.log(e);});";
    }

    // ── Location Processing ───────────────────────────────────────────

    private void ProcessLocation(GpsLocation loc)
    {
        List<PoiModel> snapshot;
        lock (_lock) { snapshot = AllPoi.ToList(); }

        var radius = SettingsPage.GlobalRadius;

        foreach (var poi in snapshot)
        {
            poi.DistanceFromUser = GpsTrackingService.CalculateDistance(
                loc.Latitude, loc.Longitude,
                poi.Latitude, poi.Longitude);
        }

        // Chỉ POI TRONG bán kính, gần nhất ưu tiên
        var inRadius = snapshot
            .Where(p => p.IsActive && p.DistanceFromUser <= radius)
            .OrderBy(p => p.DistanceFromUser)
            .ToList();

        HasPoiInRadius = inRadius.Any();
        NearestPoi = inRadius.FirstOrDefault();

        if (!_showingRoute)
        {
            if (NearestPoi != null && SelectedPoi?.Id != NearestPoi.Id)
            {
                SelectedPoi = NearestPoi;
                SelectedPoiIndex = snapshot.IndexOf(NearestPoi);
                MainThread.BeginInvokeOnMainThread(UpdatePoiIndex);
            }
            else if (NearestPoi == null)
            {
                SelectedPoi = null;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PoiIndexText = "-- / --";
                    SelectedPoiEmoji = "📡";
                    HasPoiInRadius = false;
                });
            }
        }

        _geofence.CheckGeofencesWithRadius(loc, snapshot, radius);
        MainThread.BeginInvokeOnMainThread(InjectMarkersUpdate);
    }

    private void OnLocationUpdated(GpsLocation loc)
    {
        if (_usingVirtual) return;
        CurrentLocation = loc;
        ProcessLocation(loc);
        if (!_showingRoute)
            StatusText = $"🟢 ±{loc.Accuracy:F0}m";
    }

    private void OnGpsError(string error)
        => MainThread.BeginInvokeOnMainThread(() => StatusText = $"⚠️ {error}");

    private void OnPoiTriggered(PoiModel poi, GeofenceService.TriggerType type)
    {
        var prefix = type == GeofenceService.TriggerType.Enter ? "📍" : "🔔";
        MainThread.BeginInvokeOnMainThread(() => StatusText = $"{prefix} {poi.Name}");
        _audioQueue.Enqueue(poi);
    }

    // ── Initial Map HTML ──────────────────────────────────────────────

    public void GenerateInitialMapHtml()
    {
        var focusLat = (SelectedPoi?.Latitude ?? PoiData.CENTER_LAT)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var focusLng = (SelectedPoi?.Longitude ?? PoiData.CENTER_LNG)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);

        var initMarkers = new System.Text.StringBuilder();
        foreach (var p in AllPoi)
        {
            var lat = p.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lng = p.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var rad = SettingsPage.GlobalRadius
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            var em = p.Category == "food" ? "🍜"
                    : p.Category == "drink" ? "🥤" : "🏛";
            initMarkers.Append(
                $"addPoiMarker({lat},{lng},{rad},'#2196F3',32,16,15,'{em}','{p.Name}','?','3px solid white',false);");
        }

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
            ".leaflet-popup-content{font-family:sans-serif;font-size:12px}" +
            "</style>" +
            "</head><body><div id='map'></div><script>" +

            // Khởi tạo map
            $"var map=L.map('map',{{zoomControl:false}}).setView([{focusLat},{focusLng}],17);" +
            "L.control.zoom({position:'topright'}).addTo(map);" +
            "L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png'," +
            "{maxZoom:19,attribution:''}).addTo(map);" +

            // Biến toàn cục
            "var poiMarkers=[];var poiCircles=[];var userMarker=null;var routeLine=null;" +

            // addPoiMarker
            "function addPoiMarker(lat,lng,rad,color,size,anchor,fontSize,em,name,dist,border,isSelected){" +
            "var c=L.circle([lat,lng],{radius:rad,color:color," +
            "fillOpacity:isSelected?0.15:0.08,weight:isSelected?2.5:1.5}).addTo(map);" +
            "poiCircles.push(c);" +
            "var d=document.createElement('div');" +
            "d.style.cssText='width:'+size+'px;height:'+size+'px;border-radius:50%;background:'+color+" +
            "';border:'+border+';box-shadow:0 2px 8px rgba(0,0,0,0.3);" +
            "display:flex;align-items:center;justify-content:center;font-size:'+fontSize+'px;';" +
            "d.textContent=em;" +
            "var m=L.marker([lat,lng],{icon:L.divIcon({" +
            "html:d.outerHTML,iconSize:[size,size],iconAnchor:[anchor,anchor]})}).addTo(map);" +
            "m.bindPopup('<b>'+name+'</b><br>📏 '+dist);" +
            "poiMarkers.push(m);}" +

            // clearMarkers
            "function clearMarkers(){" +
            "poiMarkers.forEach(function(m){map.removeLayer(m);});" +
            "poiCircles.forEach(function(c){map.removeLayer(c);});" +
            "poiMarkers=[];poiCircles=[];}" +

            // updateUserMarker
            "function updateUserMarker(lat,lng,isVirtual){" +
            "if(userMarker)map.removeLayer(userMarker);" +
            "var d=document.createElement('div');" +
            "d.style.cssText='width:22px;height:22px;border-radius:50%;background:'+" +
            "(isVirtual?'#FF9800':'#4CAF50')+';border:3px solid white;" +
            "box-shadow:0 0 0 6px '+(isVirtual?'rgba(255,152,0,0.3)':'rgba(76,175,80,0.25)')+';';" +
            "userMarker=L.marker([lat,lng],{icon:L.divIcon({" +
            "html:d.outerHTML,iconSize:[22,22],iconAnchor:[11,11]})}).addTo(map);" +
            "userMarker.bindPopup(isVirtual?'🕹 Vị trí test':'📍 Vị trí của bạn');}" +

            // clearRoute
            "function clearRoute(){if(routeLine){map.removeLayer(routeLine);routeLine=null;}}" +

            // Vẽ markers ban đầu
            initMarkers.ToString() +
            "</script></body></html>";
    }

    public void GenerateMapHtml() => GenerateInitialMapHtml();
}