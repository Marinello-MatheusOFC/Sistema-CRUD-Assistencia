using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Controllers;

/// <summary>
/// Busca rápida unificada (OS, clientes e aparelhos) em uma única tela.
/// A consulta é POST: CPF, telefone e IMEI nunca aparecem na URL nem em logs.
/// Toda a pesquisa é feita pelo BuscaService — nada de consultas no controller.
/// </summary>
public class BuscaController(IBuscaService buscaService) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new ResultadoBusca());

    [HttpPost]
    public async Task<IActionResult> Pesquisar(string? termo, CancellationToken cancellationToken)
    {
        var resultado = await buscaService.BuscarAsync(termo, cancellationToken);
        return View(nameof(Index), resultado);
    }
}
