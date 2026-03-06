using VinhKhanhTour.Models;

namespace VinhKhanhTour.Data;

/// <summary>
/// Dữ liệu POI cho Phố Ẩm Thực Vĩnh Khánh — Quận 4, TP.HCM
/// Đường Vĩnh Khánh nổi tiếng với ẩm thực đường phố về đêm
/// </summary>
public static class PoiData
{
    // Tọa độ trung tâm Phố Vĩnh Khánh (Quận 4, TP.HCM)
    public const double CENTER_LAT = 10.7556;
    public const double CENTER_LNG = 106.7031;

    public static List<PoiModel> GetAllPoi() => new()
    {
        new PoiModel
        {
            Id = "vk_entrance",
            Name = "Cổng Vào Phố Vĩnh Khánh",
            NameEn = "Vinh Khanh Street Entrance",
            Latitude = 10.7565,
            Longitude = 106.7025,
            RadiusMeters = 25,
            Priority = 1,
            Category = "landmark",
            Description = "Phố Vĩnh Khánh - Thiên đường ẩm thực Quận 4. Con đường dài hơn 500m với hàng trăm quán ăn đường phố nổi tiếng nhất Sài Gòn.",
            DescriptionEn = "Vinh Khanh Street - Food paradise in District 4. A 500m stretch with hundreds of famous street food stalls in Saigon.",
            TtsScript = "Chào mừng bạn đến với Phố Ẩm Thực Vĩnh Khánh! Con phố nổi tiếng này thuộc Quận 4, Thành phố Hồ Chí Minh, được mệnh danh là thiên đường ẩm thực đêm của Sài Gòn. Hơn 500 mét đường với hàng trăm quán ăn, từ hải sản tươi sống đến các món ăn vặt truyền thống. Hãy thư giãn và thưởng thức!",
            TtsScriptEn = "Welcome to Vinh Khanh Food Street! This famous street in District 4, Ho Chi Minh City, is known as Saigon's night food paradise. Over 500 meters of food stalls featuring fresh seafood and traditional Vietnamese street food. Relax and enjoy!",
            ImageUrl = "https://example.com/vinhkhanh_entrance.jpg",
            IsActive = true
        },

        new PoiModel
        {
            Id = "vk_banh_trang_tron",
            Name = "Khu Bánh Tráng Trộn & Ăn Vặt",
            NameEn = "Mixed Rice Paper & Snacks Area",
            Latitude = 10.7560,
            Longitude = 106.7028,
            RadiusMeters = 20,
            Priority = 2,
            Category = "food",
            Description = "Khu vực tập trung các xe bánh tráng trộn, bánh tráng nướng, bắp xào, hột vịt lộn — những món ăn vặt kinh điển của Sài Gòn.",
            DescriptionEn = "Area featuring classic Saigon snacks: mixed rice paper, grilled rice paper, stir-fried corn, and balut eggs.",
            TtsScript = "Bạn đang đứng trước khu ăn vặt nổi tiếng nhất Vĩnh Khánh! Tại đây bạn sẽ tìm thấy bánh tráng trộn thập cẩm giòn rụm, bánh tráng nướng thơm lừng, bắp xào bơ béo ngậy, và đặc biệt là hột vịt lộn — món ăn đặc sản của người Sài Gòn. Giá từ 10 đến 30 nghìn đồng một phần.",
            TtsScriptEn = "You are at the most famous snack area of Vinh Khanh! Here you'll find crispy mixed rice paper, fragrant grilled rice paper, buttery stir-fried corn, and the famous balut eggs — a Saigon specialty. Prices range from 10,000 to 30,000 VND per serving.",
            ImageUrl = "https://example.com/banh_trang.jpg",
            IsActive = true
        },

        new PoiModel
        {
            Id = "vk_hai_san",
            Name = "Khu Hải Sản Tươi Sống",
            NameEn = "Fresh Seafood Zone",
            Latitude = 10.7553,
            Longitude = 106.7033,
            RadiusMeters = 30,
            Priority = 1,
            Category = "food",
            Description = "Dãy hàng hải sản tươi sống: ốc, mực, tôm, cua — nổi tiếng nhất khu vực Quận 4.",
            DescriptionEn = "Row of fresh seafood stalls: snails, squid, shrimp, crab — the most famous in District 4.",
            TtsScript = "Đây là khu hải sản tươi sống nổi tiếng nhất của Phố Vĩnh Khánh! Bạn sẽ thấy các loại ốc bươu, ốc mỡ, mực nướng sa tế, tôm hùm, và cua biển được nuôi sống và chế biến ngay tại chỗ. Món ốc len xào dừa và ốc hương hấp gừng là đặc sản không thể bỏ qua. Nhớ mặc cả giá nhé!",
            TtsScriptEn = "This is the most famous fresh seafood area of Vinh Khanh Street! You'll see various types of snails, grilled squid with chili sauce, lobster, and sea crab — all kept alive and cooked on-site. The coconut-stir-fried snails and ginger-steamed snails are must-try specialties. Don't forget to bargain!",
            ImageUrl = "https://example.com/hai_san.jpg",
            IsActive = true
        },

        new PoiModel
        {
            Id = "vk_bun_mam",
            Name = "Hàng Bún Mắm Nổi Tiếng",
            NameEn = "Famous Fermented Fish Noodle Soup",
            Latitude = 10.7548,
            Longitude = 106.7037,
            RadiusMeters = 15,
            Priority = 2,
            Category = "food",
            Description = "Quán bún mắm gia truyền hơn 30 năm — đậm đà hương vị miền Tây Nam Bộ.",
            DescriptionEn = "Family-recipe fermented fish noodle soup restaurant with over 30 years of history — rich Southern Vietnamese flavors.",
            TtsScript = "Trước mặt bạn là quán bún mắm gia truyền đã có hơn 30 năm lịch sử! Bún mắm là đặc sản miền Tây, được nấu từ mắm cá sặc và cá linh, kết hợp với hải sản tươi, rau muống, và bắp chuối bào. Tô bún đậm đà, béo ngậy này là niềm tự hào của người dân Vĩnh Khánh. Giá khoảng 40 đến 60 nghìn đồng một tô.",
            TtsScriptEn = "In front of you is a family-recipe fermented fish noodle soup restaurant with over 30 years of history! Bun mam is a Southern Vietnamese specialty made from fermented fish broth combined with fresh seafood, water spinach, and banana blossom. This rich and savory bowl is the pride of Vinh Khanh. Price: 40,000 to 60,000 VND per bowl.",
            ImageUrl = "https://example.com/bun_mam.jpg",
            IsActive = true
        },

        new PoiModel
        {
            Id = "vk_nuoc_mia",
            Name = "Xe Nước Mía Nguyên Chất",
            NameEn = "Fresh Sugarcane Juice Stand",
            Latitude = 10.7557,
            Longitude = 106.7030,
            RadiusMeters = 15,
            Priority = 3,
            Category = "drink",
            Description = "Xe nước mía tươi ép ngay tại chỗ, thêm chút tắc (quất) — giải khát hoàn hảo khi dạo phố.",
            DescriptionEn = "Fresh sugarcane juice squeezed on the spot, with a hint of calamansi — the perfect refreshment while strolling.",
            TtsScript = "Ghé lại xe nước mía này để giải nhiệt nhé! Nước mía tươi ép ngay tại chỗ, vị ngọt thanh tự nhiên, thêm vài giọt tắc tạo nên hương vị thơm mát đặc trưng. Chỉ 10 nghìn đồng một ly lớn — một trong những thức uống kinh tế nhất Sài Gòn!",
            TtsScriptEn = "Stop by this sugarcane juice stand to cool down! Freshly squeezed on the spot with a naturally sweet taste, plus a few drops of calamansi for a refreshing aroma. Only 10,000 VND for a large glass — one of the most affordable drinks in Saigon!",
            ImageUrl = "https://example.com/nuoc_mia.jpg",
            IsActive = true
        },

        new PoiModel
        {
            Id = "vk_cuoi_pho",
            Name = "Điểm Cuối Phố — Check-in",
            NameEn = "End of Street — Check-in Spot",
            Latitude = 10.7540,
            Longitude = 106.7042,
            RadiusMeters = 25,
            Priority = 2,
            Category = "landmark",
            Description = "Điểm cuối của phố ẩm thực Vĩnh Khánh — nơi lý tưởng để check-in và nhìn lại cả con phố.",
            DescriptionEn = "The end of Vinh Khanh Food Street — perfect spot for photos and a view of the whole street.",
            TtsScript = "Bạn đã đi hết Phố Vĩnh Khánh rồi! Từ đây nhìn lại, bạn sẽ thấy toàn bộ con phố lung linh ánh đèn với hàng trăm quán ăn sầm uất. Đây là địa điểm check-in lý tưởng để lưu lại kỷ niệm. Hy vọng bạn đã có một trải nghiệm ẩm thực tuyệt vời tại Vĩnh Khánh. Hẹn gặp lại!",
            TtsScriptEn = "You've reached the end of Vinh Khanh Street! Looking back, you'll see the entire street glowing with lights from hundreds of busy food stalls. This is the perfect check-in spot to capture memories. We hope you've had a wonderful culinary experience at Vinh Khanh. See you next time!",
            ImageUrl = "https://example.com/cuoi_pho.jpg",
            IsActive = true
        },
    };
}