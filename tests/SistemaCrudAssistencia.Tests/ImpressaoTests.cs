using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Controllers;
using SistemaCrudAssistencia.Data;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Impressão da OS (Etapa 9): ação somente leitura autenticada e página sem
/// observações internas/histórico. As teorias de segurança ficam em SegurancaTests.
/// </summary>
public class ImpressaoTests
{
    private const string Usuario = "balcao@assistencia";

    private static OrdensServicoController Controller(AppDbContext contexto) =>
        new(new OrdemServicoService(contexto), new ClienteService(contexto), new AparelhoService(contexto));

    private static async Task<OrdemServico> AbrirOrdemAsync(AppDbContext contexto)
    {
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);
        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await new OrdemServicoService(contexto).AbrirAsync(ordem, Usuario);
        return ordem;
    }

    [Fact]
    public async Task Imprimir_RetornaAOrdemSolicitada()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordem = await AbrirOrdemAsync(contexto);

        var resultado = await Controller(contexto).Imprimir(ordem.Id, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(resultado);
        var modelo = Assert.IsType<OrdemServico>(view.Model);
        Assert.Equal(ordem.Id, modelo.Id);
        Assert.Equal(ordem.NumeroOS, modelo.NumeroOS);
    }

    [Fact]
    public async Task Imprimir_IdInexistente_RetornaNotFound()
    {
        await using var contexto = BancoEmMemoria.Criar();

        var resultado = await Controller(contexto).Imprimir(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public void PaginaDeImpressao_NaoExibeObservacoesInternasNemHistorico()
    {
        // Sobe de tests/.../bin/Debug/net10.0/ até a raiz do repositório.
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 5 && diretorio is not null; i++)
            diretorio = diretorio.Parent;

        var caminho = Path.Combine(
            diretorio!.FullName, "src", "SistemaCrudAssistencia", "Views", "OrdensServico", "Imprimir.cshtml");

        Assert.True(File.Exists(caminho), $"Arquivo não encontrado: {caminho}");
        var conteudo = File.ReadAllText(caminho);

        Assert.DoesNotContain("ObservacoesInternas", conteudo);
        Assert.DoesNotContain("Historico", conteudo);
        Assert.Contains("window.print()", conteudo);
        Assert.Contains("@media print", conteudo);
    }
}
