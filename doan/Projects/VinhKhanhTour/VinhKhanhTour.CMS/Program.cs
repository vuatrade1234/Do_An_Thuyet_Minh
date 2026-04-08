using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using VinhKhanhTour.CMS;
using VinhKhanhTour.CMS.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL API backend đang chạy local
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5206/")
});

builder.Services.AddMudServices();
builder.Services.AddScoped<CmsApiService>();
await builder.Build().RunAsync();