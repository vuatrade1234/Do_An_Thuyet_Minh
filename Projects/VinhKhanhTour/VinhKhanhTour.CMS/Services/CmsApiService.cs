using System.Net.Http.Json;
using VinhKhanhTour.Shared.Models;

namespace VinhKhanhTour.CMS.Services;

public class CmsApiService
{
    private readonly HttpClient _http;

    public CmsApiService(HttpClient http) => _http = http;

    public Task<List<PoiModel>?> GetPoisAsync()
        => _http.GetFromJsonAsync<List<PoiModel>>("api/pois");

    public Task<HttpResponseMessage> SavePoiAsync(PoiModel poi)
        => _http.PostAsJsonAsync("api/pois", poi);

    public Task<HttpResponseMessage> DeletePoiAsync(string id)
        => _http.DeleteAsync($"api/pois/{id}");
}