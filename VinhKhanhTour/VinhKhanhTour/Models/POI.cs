using SQLite;

namespace VinhKhanhTour.Models
{
    public class POI
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;
        public string TenQuan { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public string Menu { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = "dotnet_bot.png";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double BanKinh { get; set; } = 30;
        public string ThuyetMinhVi { get; set; } = string.Empty;

        [Ignore]
        public bool DaPhat { get; set; } = false;
    }
}