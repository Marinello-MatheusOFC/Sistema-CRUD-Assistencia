using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Models.ViewModels;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Controllers;

public class ClientesController(IClienteService clienteService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var clientes = await clienteService.ListarAsync(cancellationToken: cancellationToken);
        return View(new ClientesIndexViewModel { Clientes = clientes });
    }

    [HttpPost]
    public async Task<IActionResult> Pesquisar(string? busca, CancellationToken cancellationToken)
    {
        var clientes = await clienteService.ListarAsync(busca, cancellationToken: cancellationToken);
        return View("Index", new ClientesIndexViewModel { Busca = busca, Clientes = clientes });
    }

    [HttpGet]
    public IActionResult Create() => View(new ClienteFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(ClienteFormViewModel modelo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            var cliente = modelo.ParaCliente();
            await clienteService.CriarAsync(cliente, cancellationToken);
            TempData["Sucesso"] = "Cliente cadastrado com sucesso.";
            return RedirectToAction(nameof(Details), new { id = cliente.Id });
        }
        catch (RegraDeNegocioException ex)
        {
            ModelState.AddModelError(nameof(modelo.Cpf), ex.Message);
            return View(modelo);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.BuscarPorIdAsync(id, cancellationToken);
        return cliente is null ? NotFound() : View(ClienteFormViewModel.DeCliente(cliente));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ClienteFormViewModel modelo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            await clienteService.AtualizarAsync(modelo.ParaCliente(id), cancellationToken);
            TempData["Sucesso"] = "Cliente atualizado com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (RegraDeNegocioException ex)
        {
            ModelState.AddModelError(nameof(modelo.Cpf), ex.Message);
            return View(modelo);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.BuscarDetalhesAsync(id, cancellationToken);
        return cliente is null ? NotFound() : View(new ClienteDetailsViewModel { Cliente = cliente });
    }

    [HttpPost]
    public async Task<IActionResult> Desativar(int id, CancellationToken cancellationToken)
    {
        try
        {
            await clienteService.DesativarAsync(id, cancellationToken);
            TempData["Sucesso"] = "Cliente desativado.";
        }
        catch (RegraDeNegocioException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Reativar(int id, CancellationToken cancellationToken)
    {
        try
        {
            await clienteService.ReativarAsync(id, cancellationToken);
            TempData["Sucesso"] = "Cliente reativado.";
        }
        catch (RegraDeNegocioException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
