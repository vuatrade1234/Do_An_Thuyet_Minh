# <p align="center">🎧 Vĩnh Khánh Tour Audio Guide 🎧</p>

<p align="center">
  <img src="https://capsule-render.vercel.app/render?type=soft&color=auto&height=200&section=header&text=SMART%20FOOD%20TOUR&fontSize=60&subText=Automated%20Menu%20Narrator%20via%20QR%20&%20Geofencing&subFontSize=25&animation=fadeIn" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Project-Research_&_Development-eb3b5a?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Tech-Text_To_Speech-2d98da?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Feature-QR_Scanner_%26_GPS-20bf6b?style=for-the-badge" />
</p>

---

## 👥 Nhóm Nghiên Cứu | Research Team

<div align="center">

| Vai trò | Thành viên | MSSV |
| :--- | :--- | :---: |
| 🔍 **Nghiên cứu viên** | **Nguyễn Trung Nghĩa** | `3123411197` |
| 🔍 **Nghiên cứu viên** | **Lê Hoàng Giang** | `3123411077` |

</div>

---

## 🌟 Ý Tưởng Đột Phá (Core Concept)
Ứng dụng cung cấp trải nghiệm du lịch thông minh tại phố ẩm thực Vĩnh Khánh bằng cách tự động hóa việc thuyết minh:

1.  **📲 Quét mã QR:** Khách hàng quét mã tại điểm chờ để tải nhanh bộ dữ liệu âm thanh (Audio Data).
2.  **🛰️ Định vị Bán kính:** Khi khách di chuyển vào phạm vi của một gian hàng, ứng dụng tự động nhận diện vị trí.
3.  **🔊 Text-to-Speech:* Hệ thống tự động đọc thực đơn (Menu) và giới thiệu món ăn dựa trên dữ liệu đã tải.

---

## 🛠 Quy Trình Hoạt Động (Workflow)

```mermaid
graph TD
    A[Khách hàng tới Vĩnh Khánh] --> B(Quét mã QR để tải Audio)
    B --> C[Di chuyển trong phố ẩm thực]
    C --> D{Trong bán kính cửa hàng?}
    D -- Có --> E[AI TTS: Tự động đọc Menu]
    D -- Không --> C
    E --> F[Khách lựa chọn món ăn]
