namespace VinhKhanhTour.Models;

public class GpsLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }   // meters
    public DateTime Timestamp { get; set; }
    public double Speed { get; set; }      // m/s
}