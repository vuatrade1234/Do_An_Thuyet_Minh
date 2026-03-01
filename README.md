# <p align="center">🎧 Vĩnh Khánh Smart Audio Guide 🎧</p>

<p align="center">
  <img src="https://capsule-render.vercel.app/render?type=rect&color=f4a261&height=220&section=header&text=SMART%20FOOD%20TOUR&fontSize=65&subText=Automated%20Menu%20Narrator%20via%20QR%20&%20GPS&subFontSize=25&animation=fadeIn" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Project-Research_&_Development-eb3b5a?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Tech-Text_To_Speech-2d98da?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Feature-QR_Scanner_%26_Geofencing-20bf6b?style=for-the-badge" />
</p>

---

## 🔬 Đội Ngũ Nghiên Cứu | Research Team

<div align="center">

| Vai trò | Thành viên | MSSV |
| :--- | :--- | :---: |
| 🔍 **Nghiên cứu viên** | **Nguyễn Trung Nghĩa** | `3123411197` |
| 🔍 **Nghiên cứu viên** | **Lê Hoàng Giang** | `3123411077` |

</div>

---

## 🌟 Ý Tưởng Đột Phá | Core Concept
Hệ thống giải quyết vấn đề rào cản ngôn ngữ và sự tiện lợi khi trải nghiệm ẩm thực tại phố Vĩnh Khánh thông qua 2 bước chạm:

1.  **📲 Quét QR:** Khách hàng quét mã tại cổng hoặc điểm chờ để tải bộ dữ liệu âm thanh.
2.  **🔊 Tự động thuyết minh:** Khi di chuyển vào bán kính cho phép của một gian hàng, ứng dụng tự động kích hoạt **Text-to-Speech** để đọc thực đơn và giới thiệu món ăn.

---

## 🛠 Quy Trình Hoạt Động | Workflow

```mermaid
graph TD
    A[Khách hàng tới Vĩnh Khánh] --> B(Quét mã QR tại điểm đón)
    B --> C{Tải Audio Data}
    C --> D[Di chuyển bằng GPS/Geofencing]
    D --> E{Trong bán kính cửa hàng?}
    E -- Có --> F[AI TTS: Tự động đọc Menu]
    E -- Không --> D
    F --> G[Trải nghiệm thực tế]
