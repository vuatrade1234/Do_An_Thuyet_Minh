# <p align="center">🎙️ AI Text-to-Speech Research Project 🎙️</p>

<p align="center">
  <img src="https://capsule-render.vercel.app/render?type=rect&color=00b4d8&height=200&section=header&text=TEXT%20TO%20SPEECH&fontSize=70&subText=Multi-language%20Synthesis&subFontSize=25&animation=twinkling" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/AI-Research-brightgreen?style=for-the-badge&logo=openai" />
  <img src="https://img.shields.io/badge/Language-Bilingual-blue?style=for-the-badge" />
</p>

---

## 🔬 Nhóm Nghiên Cứu | Research Team

<div align="center">

| Vai trò | Thành viên | MSSV |
| :--- | :--- | :---: |
| 🛡️ **Nghiên cứu viên** | **Nguyễn Trung Nghĩa** | `3123411197` |
| 🛡️ **Nghiên cứu viên** | **Lê Hoàng Giang** | `3123411077` |

</div>

---

## 🚀 Tổng Quan Dự Án | Project Overview

Dự án tập trung nghiên cứu và triển khai hệ thống **Text-to-Speech (TTS)**, cho phép chuyển đổi văn bản thô thành giọng nói tự nhiên với độ trễ thấp.

### 🇻🇳 Tiếng Việt
- **Xử lý ngôn ngữ:** Phân tích ngữ pháp và thanh điệu tiếng Việt.
- **Tính năng:** Hỗ trợ đọc văn bản từ file hoặc nhập liệu trực tiếp.

### 🇺🇸 English
- **Natural Synthesis:** High-quality voice output with proper intonation.
- **Multi-Voice:** Supports various accents and speech rates.

---

## 🛠 Kiến Trúc Hệ Thống | Tech Architecture

```mermaid
graph LR
    A[Văn bản/Text] --> B{NLP Engine}
    B --> C[Phân tích âm tiết]
    C --> D[Bộ tổng hợp/Synthesizer]
    D --> E((Giọng nói/Audio))
    style B fill:#f9f,stroke:#333,stroke-width:2px
    style D fill:#bbf,stroke:#333,stroke-width:2px
