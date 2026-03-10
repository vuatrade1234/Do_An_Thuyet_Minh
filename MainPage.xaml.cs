using SQLite;
using VinhKhanhTour.Models;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using System.Threading;

namespace VinhKhanhTour;

public partial class MainPage : ContentPage
{
    private SQLiteAsyncConnection? _db;
    private List<POI> _danhSachPOI = new List<POI>();
    private POI? _currentClosestPOI;
    private CancellationTokenSource? _cts;

    // Tọa độ mô phỏng xuất phát (Phố Vĩnh Khánh)
    private double userLat = 10.7613;
    private double userLng = 106.7047;
    private double step = 0.00015; // Mỗi lần nhấn nút di chuyển khoảng 15m

    public MainPage()
    {
        InitializeComponent();
        _ = InitApp();
    }

    private async Task InitApp()
    {
        // Đổi tên file DB sang V28 để cập nhật đủ 10 quán mới với HinhAnh từ file máy
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "VinhKhanh_V28.db3");
        _db = new SQLiteAsyncConnection(dbPath);
        await _db.CreateTableAsync<POI>();

        if (await _db.Table<POI>().CountAsync() == 0)
        {
            var data = new List<POI> {
                new POI { Id="1", TenQuan="Ốc Oanh", Latitude=10.7602, Longitude=106.7042, BanKinh=30, Menu="Ốc hương rang muối, sò lông nướng.", ThuyetMinhVi="Chào mừng bạn đến với Ốc Oanh.", HinhAnh="oc_oanh.jpg" },
                new POI { Id="2", TenQuan="Vịt Phát Thành", Latitude=10.7615, Longitude=106.7051, BanKinh=30, Menu="Vịt quay Bắc Kinh, xá xíu.", ThuyetMinhVi="Bạn đang ở Vịt Phát Thành.", HinhAnh="vit_phat_thanh.jpg" },
                new POI { Id="3", TenQuan="Sushi Ko", Latitude=10.7608, Longitude=106.7045, BanKinh=30, Menu="Sashimi cá hồi, cơm cuộn.", ThuyetMinhVi="Sushi Ko mang đến hương vị Nhật Bản.", HinhAnh="sushi_ko.jpg" },
                new POI { Id="4", TenQuan="Ốc Đào", Latitude=10.7621, Longitude=106.7058, BanKinh=30, Menu="Ốc tỏi nướng, nghêu hấp sả.", ThuyetMinhVi="Bạn đã đến khu vực Ốc Đào.", HinhAnh="oc_dao.jpg" },
                new POI { Id="5", TenQuan="Lẩu Bò Khu Nhà Cháy", Latitude=10.7595, Longitude=106.7038, BanKinh=30, Menu="Lẩu bò thập cẩm, vú nướng.", ThuyetMinhVi="Thưởng thức lẩu bò tại Khu Nhà Cháy.", HinhAnh="lau_bo.jpg" },
                new POI { Id="6", TenQuan="Phá Lấu Cô Thảo", Latitude=10.7628, Longitude=106.7062, BanKinh=30, Menu="Phá lấu bò, bánh mì.", ThuyetMinhVi="Phá lấu Cô Thảo nổi tiếng Quận 4.", HinhAnh="pha_lau.jpg" },
                new POI { Id="7", TenQuan="Bún Cá Châu Đốc", Latitude=10.7635, Longitude=106.7068, BanKinh=30, Menu="Bún cá miền Tây.", ThuyetMinhVi="Hương vị bún cá Châu Đốc đặc trưng.", HinhAnh="bun_ca.jpg" },
                new POI { Id="8", TenQuan="Chè Hà Trâm", Latitude=10.7612, Longitude=106.7049, BanKinh=30, Menu="Chè thái, khúc bạch.", ThuyetMinhVi="Giải nhiệt tại tiệm chè Hà Trâm.", HinhAnh="che_ha_tram.jpg" },
                new POI { Id="9", TenQuan="Mì Gia Tân Tòng Lợi", Latitude=10.7618, Longitude=106.7055, BanKinh=30, Menu="Mì vịt tiềm, sủi cảo.", ThuyetMinhVi="Mì gia truyền thống Tân Tòng Lợi.", HinhAnh="mi_vit_tiem.jpg" },
                new POI { Id="10", TenQuan="Bánh Tráng Chú Viên", Latitude=10.7625, Longitude=106.7060, BanKinh=30, Menu="Bánh tráng trộn thập cẩm.", ThuyetMinhVi="Thưởng thức bánh tráng trộn chú Viên.", HinhAnh="banh_trang.jpg" }
            };
            await _db.InsertAllAsync(data);
        }
        _danhSachPOI = await _db.Table<POI>().ToListAsync();
        mapRadar.Source = new HtmlWebViewSource { Html = GetMapHtml() };
    }

    // Xử lý Audio hàng chờ
    private async Task SmartSpeak(string text)
    {
        if (_cts != null) { _cts.Cancel(); _cts.Dispose(); }
        _cts = new CancellationTokenSource();
        try
        {
            // FIX .NET 10: Truyền null cho options, dùng token quản lý hàng chờ
            await TextToSpeech.Default.SpeakAsync(text, options: null, cancelToken: _cts.Token);
        }
        catch (OperationCanceledException) { }
    }

    // Điều hướng nhân vật
    private void MoveUp(object sender, EventArgs e) { userLat += step; UpdateLocation(); }
    private void MoveDown(object sender, EventArgs e) { userLat -= step; UpdateLocation(); }
    private void MoveLeft(object sender, EventArgs e) { userLng -= step; UpdateLocation(); }
    private void MoveRight(object sender, EventArgs e) { userLng += step; UpdateLocation(); }

    private async void UpdateLocation()
    {
        POI? closest = null;
        double minDistance = double.MaxValue;

        foreach (var poi in _danhSachPOI)
        {
            double dist = Location.CalculateDistance(userLat, userLng, poi.Latitude, poi.Longitude, DistanceUnits.Kilometers) * 1000;

            if (dist < minDistance) { minDistance = dist; closest = poi; }

            // Chỉ thuyết minh khi vào bán kính và đảm bảo đúng quán đó
            if (dist <= poi.BanKinh && !poi.DaPhat)
            {
                _ = SmartSpeak(poi.ThuyetMinhVi);
                poi.DaPhat = true; // Chống phát lặp
            }
            else if (dist > poi.BanKinh + 10) { poi.DaPhat = false; }
        }

        _currentClosestPOI = closest;
        lblTenQuan.Text = closest?.TenQuan ?? "Đang di chuyển...";
        lblDistance.Text = $"Cách quán gần nhất: {Math.Round(minDistance)}m";

        // Hiển thị ảnh góc trái bản đồ nếu khoảng cách < 100m
        if (closest != null && minDistance < 100)
        {
            imgPopup.Source = closest.HinhAnh;
            imgPopupBorder.IsVisible = true;
        }
        else
        {
            imgPopupBorder.IsVisible = false;
        }

        // Highlight quán gần nhất trên bản đồ JS
        string jsCall = (closest != null && minDistance < 100)
            ? $"updateUser({userLat}, {userLng}, {closest.Latitude}, {closest.Longitude});"
            : $"updateUser({userLat}, {userLng}, null, null);";

        await mapRadar.EvaluateJavaScriptAsync(jsCall);
    }

    private string GetMapHtml()
    {
        string mks = "";
        foreach (var p in _danhSachPOI)
            mks += $"L.marker([{p.Latitude}, {p.Longitude}]).addTo(map).bindPopup('{p.TenQuan}');";

        return $@"<html><head>
                <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
                <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
                </head><body style='margin:0'><div id='map' style='height:100vh'></div>
                <script>
                    var map = L.map('map').setView([10.7613, 106.7047], 17);
                    L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png').addTo(map);
                    {mks}
                    var userMarker = L.circleMarker([10.7613, 106.7047], {{color:'blue', radius:8, fillOpacity:1}}).addTo(map);
                    var highlightCircle = L.circle([0,0], {{radius: 40, color: 'red', weight: 2, fillOpacity: 0.3}}).addTo(map);

                    function updateUser(lat, lng, pLat, pLng) {{
                        userMarker.setLatLng([lat, lng]);
                        map.panTo([lat, lng]);
                        if(pLat && pLng) {{
                            highlightCircle.setLatLng([pLat, pLng]);
                            highlightCircle.setStyle({{opacity: 1, fillOpacity: 0.3}});
                        }} else {{
                            highlightCircle.setStyle({{opacity: 0, fillOpacity: 0}});
                        }}
                    }}
                </script></body></html>";
    }

    private async void OnShowDetailsTapped(object sender, EventArgs e)
    {
        if (_cts != null) _cts.Cancel(); // Dừng nói khi chuyển trang
        if (_currentClosestPOI != null)
            await Navigation.PushAsync(new DetailsPage(_currentClosestPOI));
    }
}
