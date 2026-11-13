using System.Globalization;
using Electronics.CsvImport.Models;
using Electronics.CsvImport.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton(new CsvFilePathResolver(builder.Environment.ContentRootPath));
builder.Services.AddSingleton<IProductImportService, ProductImportService>();
builder.Services.AddSingleton(builder.Configuration.GetSection("CsvImport").Get<CsvImportOptions>() ?? new());
builder.Services.AddSingleton<StartupImportService>();

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");

var app = builder.Build();
var startup = app.Services.GetRequiredService<StartupImportService>().Run();
app.Logger.LogInformation("Импорт при запуске: строк {Rows}, добавлено {Imported}, сообщений {Errors}",
    startup.Result.TotalRows, startup.Result.ImportedCount, startup.Result.ErrorCount);

app.UseExceptionHandler("/Products/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Products}/{action=Index}/{id?}");
app.Run();
