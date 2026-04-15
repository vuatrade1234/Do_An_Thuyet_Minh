# VinhKhanhTour - Đối chiếu Sơ đồ Sequence và Mã nguồn (C#)

Tài liệu này giúp bạn tìm nhanh các đoạn code thực thi logic đã được mô tả trong các sơ đồ Sequence của đồ án.

---

### UC1: Xem Bản Đồ (Map Interaction)
*   **Hành động:** Khởi tạo bản đồ Leaflet trong WebView.
    *   **File:** `VinhKhanhTour/ViewModels/MapViewModel.cs`
    *   **Phương thức:** `GenerateInitialMapHtml()` (Dòng 717) - Tạo chuỗi HTML chứa script khởi tạo bản đồ.
*   **Hành động:** Inject Market POI lên bản đồ.
    *   **File:** `VinhKhanhTour/ViewModels/MapViewModel.cs`
    *   **Phương thức:** `BuildMarkersJs()` (Dòng 546) - Chuyển danh sách POI thành các lệnh JavaScript `addPoiMarker`.
*   **Hành động:** Bay tới vị trí GPS hiện tại.
    *   **File:** `VinhKhanhTour/Views/MapPage.xaml.cs`
    *   **Phương thức:** `OnMyLocationClicked()` (Dòng 250) - Thực thi lệnh `map.flyTo` qua WebView.

---

### UC2: Nhận Diện Tự Động (Geofencing)
*   **Hành động:** Vòng lặp lấy GPS mỗi 5 giây.
    *   **File:** `VinhKhanhTour/Services/GpsTrackingService.cs`
    *   **Phương thức:** `TrackingLoop()` (Dòng 43) - Gọi `Geolocation.GetLocationAsync`.
*   **Hành động:** Tính khoảng cách Haversine.
    *   **File:** `VinhKhanhTour/Services/GpsTrackingService.cs`
    *   **Phương thức:** `CalculateDistance()` (Dòng 93).
*   **Hành động:** Logic kiểm tra vùng và kích hoạt thuyết minh.
    *   **File:** `VinhKhanhTour/ViewModels/MapViewModel.cs`
    *   **Phương thức:** `CheckPoiProximity()` (Dòng 377) - Kiểm tra bán kính và xử lý Cooldown.

---

### UC3: Tìm Kiếm & Gợi Ý (Search)
*   **Hành động:** Lọc danh sách khi người dùng gõ phím.
    *   **File:** `VinhKhanhTour/Views/PoiListPage.xaml.cs`
    *   **Phương thức:** `OnSearchTextChanged()` (Dòng 74).
*   **Hành động:** Chuẩn hóa tiếng Việt (bỏ dấu) để tìm nhanh.
    *   **File:** `VinhKhanhTour/Views/PoiListPage.xaml.cs`
    *   **Phương thức:** `NormalizeString()` (Dòng 102).

---

### UC6: Chỉ Đường (Routing)
*   **Hành động:** Gửi yêu cầu chỉ đường từ List sang Map.
    *   **File:** `VinhKhanhTour/Views/PoiListPage.xaml.cs`
    *   **Phương thức:** `NavigateToPoi()` (Dòng 160) - Gửi `ShowRouteMessage`.
*   **Hành động:** Vẽ đường thẳng (Fallback).
    *   **File:** `VinhKhanhTour/ViewModels/MapViewModel.cs`
    *   **Phương thức:** `InjectRouteWithMarkers()` (Dòng 623).
*   **Hành động:** Gọi API thực tế và vẽ đường uốn lượn.
    *   **File:** `VinhKhanhTour/ViewModels/MapViewModel.cs`
    *   **Phương thức:** `TryFetchOsrmRouteAsync()` (Dòng 657).

---

### UC7: Quét Mã QR (DeepLink)
*   **Hành động:** Tiếp nhận URI từ hệ điều hành.
    *   **File:** `VinhKhanhTour/DeepLinkHandler.cs`
    *   **Phương thức:** `Handle()` (Dòng 16).
*   **Hành động:** Logic xử lý QR cho POI (Hỏi người dùng, phát audio).
    *   **File:** `VinhKhanhTour/DeepLinkHandler.cs`
    *   **Phương thức:** `HandlePoiQrAsync()` (Dòng 67).
*   **Hành động:** Ghi log lượt quét lên server.
    *   **File:** `VinhKhanhTour/Services/ApiSyncService.cs`
    *   **Phương thức:** `LogQrScanAsync()` (Dòng 119).

---

### UC8: Cài Đặt (Settings)
*   **Hành động:** Thay đổi ngôn ngữ toàn hệ thống.
    *   **File:** `VinhKhanhTour/Views/SettingsPage.xaml.cs`
    *   **Phương thức:** `SwitchLanguage()` (Dòng 246) - Gọi `LocalizationService.SetLocale`.
*   **Hành động:** Cập nhật bán kính nhận diện.
    *   **File:** `VinhKhanhTour/Views/SettingsPage.xaml.cs`
    *   **Phương thức:** `OnRadiusChanged()` (Dòng 210) - Gán giá trị vào biến `static GlobalRadius`.

---

### UC5: Nghe Thuyết Minh (Audio Queue)
*   **Hành động:** Xếp hàng phát âm thanh.
    *   **File:** `VinhKhanhTour/Services/AudioQueueService.cs`
    *   **Phương thức:** `Enqueue()` (Dòng 63 trong file Services/AudioQueueService.cs).
*   **Hành động:** Ưu tiên MP3 hoặc chuyển sang TTS.
    *   **File:** `VinhKhanhTour/Services/AudioQueueService.cs`
    *   **Phương thức:** `PlayNextAsync()` xử lý logic kiểm tra `audioUrl`.

---
**Ghi chú:** Các số dòng có thể thay đổi nhẹ tùy vào phiên bản code, nhưng tên phương thức và file là cố định theo kiến trúc MVVM của dự án.
