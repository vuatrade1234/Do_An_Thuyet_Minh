namespace VinhKhanhTour.Models;

public class PoiModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameZh { get; set; } = string.Empty;
    public string NameJa { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusMeters { get; set; } = 30;
    public int Priority { get; set; } = 1;
    public string Description { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionZh { get; set; } = string.Empty;
    public string DescriptionJa { get; set; } = string.Empty;
    public string DescriptionKo { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string AudioFile { get; set; } = string.Empty;
    public string TtsScript { get; set; } = string.Empty;
    public string TtsScriptEn { get; set; } = string.Empty;
    public string TtsScriptZh { get; set; } = string.Empty;
    public string TtsScriptJa { get; set; } = string.Empty;
    public string TtsScriptKo { get; set; } = string.Empty;
    public string Category { get; set; } = "food";
    public bool IsActive { get; set; } = true;

    public string CoverImage => Category switch
    {
        "food" => "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=600&q=80",
        "drink" => "https://images.unsplash.com/photo-1544145945-f90425340c7e?w=600&q=80",
        "landmark" => "https://images.unsplash.com/photo-1528360983277-13d401cdc186?w=600&q=80",
        _ => "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=600&q=80"
    };

    public string CategoryEmoji => Category switch
    {
        "food" => "🍜",
        "drink" => "🥤",
        "landmark" => "🏛",
        _ => "📍"
    };

    public string CategoryLabel
    {
        get
        {
            var loc = Services.LocalizationService.Instance;
            return Category switch
            {
                "food" => $"🍜 {loc.Get("cat_food")}",
                "drink" => $"🥤 {loc.Get("cat_drink")}",
                "landmark" => $"🏛 {loc.Get("cat_landmark")}",
                _ => $"📍 {loc.Get("cat_other")}"
            };
        }
    }

    public string CategoryColor => Category switch
    {
        "food" => "#E65100",
        "drink" => "#1565C0",
        "landmark" => "#4A148C",
        _ => "#1A237E"
    };

    public string LocalizedName
    {
        get
        {
            var loc = Services.LocalizationService.Instance;
            return loc.CurrentLocale switch
            {
                Services.AppLocale.English => string.IsNullOrEmpty(NameEn) ? Name : NameEn,
                Services.AppLocale.Chinese => string.IsNullOrEmpty(NameZh) ? Name : NameZh,
                Services.AppLocale.Japanese => string.IsNullOrEmpty(NameJa) ? Name : NameJa,
                Services.AppLocale.Korean => string.IsNullOrEmpty(NameKo) ? Name : NameKo,
                _ => Name
            };
        }
    }

    public string LocalizedDescription
    {
        get
        {
            var loc = Services.LocalizationService.Instance;
            return loc.CurrentLocale switch
            {
                Services.AppLocale.English => string.IsNullOrEmpty(DescriptionEn) ? Description : DescriptionEn,
                Services.AppLocale.Chinese => string.IsNullOrEmpty(DescriptionZh) ? Description : DescriptionZh,
                Services.AppLocale.Japanese => string.IsNullOrEmpty(DescriptionJa) ? Description : DescriptionJa,
                Services.AppLocale.Korean => string.IsNullOrEmpty(DescriptionKo) ? Description : DescriptionKo,
                _ => Description
            };
        }
    }

    public string LocalizedTtsScript
    {
        get
        {
            var loc = Services.LocalizationService.Instance;
            return loc.CurrentLocale switch
            {
                Services.AppLocale.English => string.IsNullOrEmpty(TtsScriptEn) ? TtsScript : TtsScriptEn,
                Services.AppLocale.Chinese => string.IsNullOrEmpty(TtsScriptZh) ? TtsScript : TtsScriptZh,
                Services.AppLocale.Japanese => string.IsNullOrEmpty(TtsScriptJa) ? TtsScript : TtsScriptJa,
                Services.AppLocale.Korean => string.IsNullOrEmpty(TtsScriptKo) ? TtsScript : TtsScriptKo,
                _ => TtsScript
            };
        }
    }

    public string DistanceText
    {
        get
        {
            if (!DistanceFromUser.HasValue) return "?";
            return DistanceFromUser < 1000
                ? $"📏 {DistanceFromUser:F0}m"
                : $"📏 {DistanceFromUser / 1000:F1}km";
        }
    }

    // Runtime state
    public double? DistanceFromUser { get; set; }
    public bool IsNearby { get; set; }
    public bool IsHighlighted { get; set; }
}