using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Enums;
using SistemaCrudAssistencia.Models.Extensions;
using SistemaCrudAssistencia.Models.Filtros;
using SistemaCrudAssistencia.Models.Validations;
using SistemaCrudAssistencia.Models.ViewModels;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Controllers;

/// <summary>
/// Fluxo das Ordens de Serviço. O controller é fino: valida entrada de
/// formulário e delega TODA regra de negócio aos Services. NumeroOS,
/// ValorTotal, DataEntrada, status e histórico são definidos exclusivamente
/// no servidor. Nenhum campo de texto enviado pelo navegador é aceito como
/// usuário do histórico — sempre a identidade autenticada.
/// </summary>
public class OrdensServicoController(
    IOrdemServicoService ordemServicoService,
    IClienteService clienteService,
    IAparelhoService aparelhoService) : Controller
{
    /// <summary>Usuário da identidade autenticada (nunca vem do formulário).</summary>
    private string? UsuarioAtual =>
        User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

    // ----------------------------------------------------------------
    // Listagem
    // ----------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var ordens = await ordemServicoService.ListarAsync(new FiltroOrdensServico(), cancellationToken);
        return View(new OrdensServicoIndexViewModel { Ordens = ordens });
    }

    [HttpPost]
    public async Task<IActionResult> Pesquisar(OrdensServicoIndexViewModel modelo, CancellationToken cancellationToken)
    {
        modelo.Ordens = await ordemServicoService.ListarAsync(modelo.Filtro, cancellationToken);
        return View(nameof(Index), modelo);
    }

    // ----------------------------------------------------------------
    // Wizard de nova OS: CPF -> cliente -> aparelho -> entrada -> confirmar
    // ----------------------------------------------------------------

    [HttpGet]
    public IActionResult Nova() => View(new NovaOrdemServicoViewModel());

    /// <summary>Etapa 1: localiza o cliente pelo CPF. POST para o CPF nunca aparecer na URL.</summary>
    [HttpPost]
    public async Task<IActionResult> LocalizarCliente(NovaOrdemServicoViewModel modelo, CancellationToken cancellationToken)
    {
        var normalizado = Cpf.Normalizar(modelo.Cpf);

        if (!Cpf.EhValido(normalizado))
        {
            modelo.Erro = "CPF inválido. Verifique os 11 dígitos.";
            return View(nameof(Nova), modelo);
        }

        modelo.Cpf = Cpf.Formatar(normalizado);
        var cliente = await clienteService.BuscarPorCpfAsync(normalizado, cancellationToken);

        if (cliente is null)
        {
            modelo.CadastroCliente = new ClienteFormViewModel { Cpf = modelo.Cpf };
            modelo.Mensagem = "CPF não encontrado na base. Cadastre o cliente agora, sem sair da abertura da OS.";
            return View(nameof(Nova), modelo);
        }

        if (!cliente.Ativo)
        {
            modelo.Erro = $"CPF localizado, mas o cliente {cliente.NomeCompleto} está INATIVO. " +
                          "Reative-o na ficha do cliente antes de abrir uma OS — um cliente inativo " +
                          "nunca gera um segundo cadastro.";
            return View(nameof(Nova), modelo);
        }

        return await MontarEtapaAparelhoAsync(
            modelo,
            cliente,
            "Cliente localizado. Nada precisa ser digitado novamente: selecione o aparelho e informe o defeito.",
            cancellationToken);
    }

    /// <summary>Etapa 1b: cadastro do cliente novo dentro do fluxo, sem abandonar a abertura da OS.</summary>
    [HttpPost]
    public async Task<IActionResult> CadastrarCliente(ClienteFormViewModel cadastro, CancellationToken cancellationToken)
    {
        var modelo = new NovaOrdemServicoViewModel { CadastroCliente = cadastro, Cpf = cadastro.Cpf };

        if (!ModelState.IsValid)
            return View(nameof(Nova), modelo);

        try
        {
            await clienteService.CriarAsync(cadastro.ParaCliente(), cancellationToken);
        }
        catch (RegraDeNegocioException ex)
        {
            // CPF já existe (ativo ou inativo): a regra do ClienteService impede duplicidade.
            var existente = await clienteService.BuscarPorCpfAsync(cadastro.Cpf, cancellationToken);

            if (existente is { Ativo: true })
                return await MontarEtapaAparelhoAsync(
                    modelo,
                    existente,
                    "Este CPF já possui cadastro ativo. Prosseguindo com o cliente existente — nada será duplicado.",
                    cancellationToken);

            modelo.Erro = existente is null
                ? ex.Message
                : "Este CPF pertence a um cliente INATIVO. Reative-o na ficha do cliente e refaça a busca — " +
                  "cadastro duplicado não é permitido.";
            return View(nameof(Nova), modelo);
        }

        var cliente = await clienteService.BuscarPorCpfAsync(cadastro.Cpf, cancellationToken);

        if (cliente is null)
        {
            modelo.Erro = "Cliente cadastrado, mas não foi possível localizá-lo. Refaça a busca pelo CPF.";
            return View(nameof(Nova), modelo);
        }

        return await MontarEtapaAparelhoAsync(
            modelo,
            cliente,
            "Cliente cadastrado com sucesso. Agora informe o aparelho e o defeito.",
            cancellationToken);
    }

    /// <summary>
    /// Confirmação final: tudo recarregado e revalidado no servidor.
    /// ClienteId/AparelhoId vindos do navegador NÃO são confiáveis — o servidor
    /// recarrega ambos, confirma existência, atividade do cliente e que o
    /// aparelho pertence aquele cliente, e só então chama AbrirAsync.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Confirmar(NovaOrdemServicoViewModel modelo, CancellationToken cancellationToken)
    {
        if (!modelo.CadastrarAparelhoNovo)
            RemoverErrosDoNovoAparelho();

        var cliente = await clienteService.BuscarPorIdAsync(modelo.ClienteId, cancellationToken);

        if (cliente is null)
        {
            ReiniciarEtapaCliente(modelo, "Cliente não encontrado. Refaça a busca pelo CPF.");
            return View(nameof(Nova), modelo);
        }

        if (!cliente.Ativo)
        {
            ReiniciarEtapaCliente(modelo, $"Cliente {cliente.NomeCompleto} está inativo. Reative-o antes de abrir uma OS.");
            return View(nameof(Nova), modelo);
        }

        if (string.IsNullOrWhiteSpace(modelo.DefeitoRelatado))
            ModelState.AddModelError(nameof(modelo.DefeitoRelatado), "Descreva o defeito relatado.");

        int aparelhoId;

        if (modelo.CadastrarAparelhoNovo)
        {
            if (!ModelState.IsValid)
                return await ReexibirEtapaAparelhoAsync(modelo, cliente, cancellationToken);

            var novo = modelo.NovoAparelho.ParaAparelho(cliente.Id);

            try
            {
                await aparelhoService.CriarAsync(novo, cancellationToken);
            }
            catch (RegraDeNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return await ReexibirEtapaAparelhoAsync(modelo, cliente, cancellationToken);
            }

            aparelhoId = novo.Id;
        }
        else
        {
            if (!modelo.AparelhoId.HasValue)
                ModelState.AddModelError(nameof(modelo.AparelhoId), "Selecione um aparelho do cliente.");

            if (!ModelState.IsValid)
                return await ReexibirEtapaAparelhoAsync(modelo, cliente, cancellationToken);

            var aparelho = await aparelhoService.BuscarAsync(modelo.AparelhoId!.Value, cancellationToken);

            if (aparelho is null)
            {
                ModelState.AddModelError(nameof(modelo.AparelhoId), "Aparelho não encontrado. Atualize a lista e tente novamente.");
                return await ReexibirEtapaAparelhoAsync(modelo, cliente, cancellationToken);
            }

            // Revalidação servidor: aparelho deve pertencer ao cliente recarregado.
            if (aparelho.ClienteId != cliente.Id)
            {
                ModelState.AddModelError(
                    nameof(modelo.AparelhoId),
                    "Operação recusada: o aparelho selecionado pertence a outro cliente.");
                return await ReexibirEtapaAparelhoAsync(modelo, cliente, cancellationToken);
            }

            aparelhoId = aparelho.Id;
        }

        var ordemServico = new OrdemServico
        {
            ClienteId = cliente.Id,
            AparelhoId = aparelhoId,
            DefeitoRelatado = modelo.DefeitoRelatado!.Trim(),
            EstadoFisicoEntrada = Opcional(modelo.EstadoFisicoEntrada),
            AcessoriosEntregues = Opcional(modelo.AcessoriosEntregues),
            ObservacoesEntrada = Opcional(modelo.ObservacoesEntrada)
        };

        try
        {
            await ordemServicoService.AbrirAsync(ordemServico, UsuarioAtual, cancellationToken: cancellationToken);
        }
        catch (RegraDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReexibirEtapaAparelhoAsync(modelo, cliente, cancellationToken);
        }

        TempData["Sucesso"] = $"OS {ordemServico.NumeroOS} aberta com sucesso.";
        return RedirectToAction(nameof(Details), new { id = ordemServico.Id });
    }

    // ----------------------------------------------------------------
    // Detalhes e edição dos campos de atendimento
    // ----------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var ordemServico = await ordemServicoService.BuscarAsync(id, cancellationToken);
        return ordemServico is null
            ? NotFound()
            : View(new OrdemServicoDetailsViewModel { OrdemServico = ordemServico });
    }

    /// <summary>
    /// Página de impressão da OS. Somente leitura, sem ações: o navegador
    /// imprime o HTML (não há geração de PDF). Continua autenticado pela
    /// FallbackPolicy e NÃO exibe observações internas nem histórico.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Imprimir(int id, CancellationToken cancellationToken)
    {
        var ordemServico = await ordemServicoService.BuscarAsync(id, cancellationToken);
        return ordemServico is null ? NotFound() : View(ordemServico);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var ordemServico = await ordemServicoService.BuscarAsync(id, cancellationToken);

        if (ordemServico is null)
            return NotFound();

        if (!ordemServico.EstaEmAndamento)
        {
            TempData["Aviso"] =
                $"OS {ordemServico.NumeroOS} está {ordemServico.Status.Descricao().ToLowerInvariant()} e não pode ser editada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(OrdemServicoFormViewModel.DeOrdemServico(ordemServico));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, OrdemServicoFormViewModel formulario, CancellationToken cancellationToken)
    {
        if (!ValoresMonetarios.TentarConverter(formulario.ValorPecas, out _))
            ModelState.AddModelError(nameof(formulario.ValorPecas), "Valor de peças inválido.");

        if (!ValoresMonetarios.TentarConverter(formulario.ValorMaoDeObra, out _))
            ModelState.AddModelError(nameof(formulario.ValorMaoDeObra), "Valor de mão de obra inválido.");

        if (!ModelState.IsValid)
            return View(formulario);

        try
        {
            var paraAtualizar = formulario.ParaOrdemServico();
            paraAtualizar.Id = id;
            await ordemServicoService.AtualizarAsync(paraAtualizar, cancellationToken);
            TempData["Sucesso"] = "Ordem de serviço atualizada. O valor total foi recalculado no servidor.";
        }
        catch (RegraDeNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(formulario);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // ----------------------------------------------------------------
    // Status — sempre via OrdemServicoService, sempre POST, sempre com histórico
    // ----------------------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> AlterarStatus(int id, string? novoStatus, string? observacao, CancellationToken cancellationToken)
    {
        var status = StatusOrdemServicoExtensions.Parse(novoStatus);

        if (status is null)
        {
            TempData["Erro"] = "Status informado é inválido.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await ordemServicoService.AlterarStatusAsync(id, status.Value, observacao, UsuarioAtual, cancellationToken);
            TempData["Sucesso"] = $"Status alterado para {status.Value.Descricao()}.";
        }
        catch (RegraDeNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Finalizar(int id, string? observacao, CancellationToken cancellationToken)
    {
        try
        {
            await ordemServicoService.FinalizarAsync(id, observacao, UsuarioAtual, cancellationToken);
            TempData["Sucesso"] = "Reparo finalizado: OS em Pronto, aguardando retirada.";
        }
        catch (RegraDeNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RegistrarRetirada(int id, string? observacao, CancellationToken cancellationToken)
    {
        try
        {
            await ordemServicoService.RegistrarRetiradaAsync(id, observacao, UsuarioAtual, cancellationToken);
            TempData["Sucesso"] = "Retirada registrada: OS Entregue.";
        }
        catch (RegraDeNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancelar(int id, string? observacao, CancellationToken cancellationToken)
    {
        try
        {
            await ordemServicoService.AlterarStatusAsync(
                id, StatusOrdemServico.Cancelado, observacao, UsuarioAtual, cancellationToken);
            TempData["Sucesso"] = "OS cancelada.";
        }
        catch (RegraDeNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // ----------------------------------------------------------------
    // Auxiliares (apenas apresentação, sem regra de negócio)
    // ----------------------------------------------------------------

    private async Task<IActionResult> MontarEtapaAparelhoAsync(
        NovaOrdemServicoViewModel modelo, Cliente cliente, string mensagem, CancellationToken cancellationToken)
    {
        modelo.Cliente = cliente;
        modelo.ClienteId = cliente.Id;
        modelo.CadastroCliente = null;
        modelo.Cpf = Cpf.Formatar(cliente.Cpf);
        modelo.Aparelhos = await aparelhoService.ListarPorClienteAsync(cliente.Id, cancellationToken);
        modelo.Mensagem = mensagem;
        modelo.Erro = null;
        return View(nameof(Nova), modelo);
    }

    private async Task<IActionResult> ReexibirEtapaAparelhoAsync(
        NovaOrdemServicoViewModel modelo, Cliente cliente, CancellationToken cancellationToken)
    {
        modelo.Cliente = cliente;
        modelo.ClienteId = cliente.Id;
        modelo.CadastroCliente = null;
        modelo.Cpf = Cpf.Formatar(cliente.Cpf);
        modelo.Aparelhos = await aparelhoService.ListarPorClienteAsync(cliente.Id, cancellationToken);
        return View(nameof(Nova), modelo);
    }

    private void ReiniciarEtapaCliente(NovaOrdemServicoViewModel modelo, string erro)
    {
        modelo.Cliente = null;
        modelo.ClienteId = 0;
        modelo.CadastroCliente = null;
        modelo.Aparelhos = [];
        modelo.Mensagem = null;
        modelo.Erro = erro;
    }

    private void RemoverErrosDoNovoAparelho()
    {
        var chaves = ModelState.Keys
            .Where(k => k.StartsWith("NovoAparelho", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var chave in chaves)
            ModelState.Remove(chave);
    }

    private static string? Opcional(string? valor)
    {
        var texto = valor?.Trim();
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }
}
