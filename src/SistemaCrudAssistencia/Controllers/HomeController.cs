using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Models.ViewModels;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;

    public HomeController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var resumo = await _dashboardService.ObterAsync(cancellationToken);
        var viewModel = new DashboardViewModel(resumo);
        return View(viewModel);
    }
}
