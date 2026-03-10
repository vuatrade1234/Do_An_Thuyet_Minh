namespace VinhKhanhTour.Shared.Models;

public class TourModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Desc { get; set; } = "";
    public List<string> PoiIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int QrScans { get; set; } = 0;
    public string QrType { get; set; } = "play";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}