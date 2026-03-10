namespace VinhKhanhTour.Models;

public class PoiModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusMeters { get; set; } = 30;   // bán kính kích hoạt
    public int Priority { get; set; } = 1;            // 1=cao, 2=tb, 3=thấp
    public string Description { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string AudioFile { get; set; } = string.Empty; // file .mp3 local
    public string TtsScript { get; set; } = string.Empty;  // text cho TTS
    public string TtsScriptEn { get; set; } = string.Empty;
    public string Category { get; set; } = "food";         // food, landmark, etc.
    public bool IsActive { get; set; } = true;

    // Runtime state (không lưu)
    public double? DistanceFromUser { get; set; }
    public bool IsNearby { get; set; }
    public bool IsHighlighted { get; set; }
}