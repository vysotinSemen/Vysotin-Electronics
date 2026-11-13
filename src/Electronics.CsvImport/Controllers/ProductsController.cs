using Electronics.CsvImport.Models;
using Electronics.CsvImport.Services;
using Microsoft.AspNetCore.Mvc;

namespace Electronics.CsvImport.Controllers;

public sealed class ProductsController(
    IProductRepository repository,
    IProductImportService importService,
    StartupImportService startup) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new CatalogViewModel(repository.GetAll(), startup.Report));

    [HttpGet]
    public IActionResult Import() => View(new ImportViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Import(string? filePath) => View(new ImportViewModel
    {
        FilePath = filePath ?? string.Empty,
        Result = importService.ImportFromCsv(filePath)
    });

    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View();
    }
}
