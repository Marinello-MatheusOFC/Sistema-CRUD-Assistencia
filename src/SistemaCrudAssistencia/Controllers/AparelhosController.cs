using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Models.ViewModels;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Controllers;

public class AparelhosController(IAparelhoService aparelhoService, IClienteService clienteService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(int clienteId, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.BuscarPorIdAsync(clienteId, cancellationToken);

        if (cliente is null)
            return NotFound();

        ViewData["ClienteId"] = cliente.Id;
        return View(new AparelhoFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(int clienteId, AparelhoFormViewModel formulario, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.BuscarPorIdAsync(clienteId, cancellationToken);

        if (cliente is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewData["ClienteId"] = cliente.Id;
            return View(formulario);
        }

        try
        {
            var aparelho = formulario.ParaAparelho(cliente.Id);
            await aparelhoService.CriarAsync(aparelho, cancellationToken);
            TempData["Sucesso"] = "Aparelho cadastrado com sucesso.";
            return RedirectToAction(nameof(Details), new { id = aparelho.Id });
        }
        catch (RegraDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["ClienteId"] = cliente.Id;
            return View(formulario);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var aparelho = await aparelhoService.BuscarAsync(id, cancellationToken);
        return aparelho is null ? NotFound() : View(AparelhoFormViewModel.DeAparelho(aparelho));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AparelhoFormViewModel formulario, CancellationToken cancellationToken)
    {
        var aparelhoAtual = await aparelhoService.BuscarAsync(id, cancellationToken);

        if (aparelhoAtual is null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(formulario);

        try
        {
            await aparelhoService.AtualizarAsync(formulario.ParaAparelho(aparelhoAtual.ClienteId, id), cancellationToken);
            TempData["Sucesso"] = "Aparelho atualizado com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (RegraDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var aparelho = await aparelhoService.BuscarDetalhesAsync(id, cancellationToken);
        return aparelho is null ? NotFound() : View(new AparelhoDetailsViewModel { Aparelho = aparelho });
    }
}
