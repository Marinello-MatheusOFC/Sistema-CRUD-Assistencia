using SistemaCrudAssistencia.Data;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Busca unificada da Etapa 9: número da OS, CPF, nome, telefone e IMEI
/// devolvendo OS, clientes (ativos e inativos) e aparelhos — sempre pelo
/// BuscaService, com a normalização dos Services já existentes.
/// </summary>
public class BuscaServiceTests
{
    private const string Usuario = "balcao@assistencia";

    private static BuscaService Servico(AppDbContext contexto) =>
        new(new ClienteService(contexto), new OrdemServicoService(contexto), new AparelhoService(contexto));

    /// <summary>Cadastra cliente + aparelho (com IMEI) e abre a primeira OS do balcão.</summary>
    private static async Task<OrdemServico> AbrirOrdemPadraoAsync(AppDbContext contexto)
    {
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente, "352099001761481");
        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await new OrdemServicoService(contexto).AbrirAsync(ordem, Usuario);
        return ordem;
    }

    [Fact]
    public async Task BuscarAsync_PorNumeroDaOs_RetornaSomenteAquelaOrdem()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordem = await AbrirOrdemPadraoAsync(contexto);

        var resultado = await Servico(contexto).BuscarAsync("000001");

        Assert.True(resultado.EncontrouAlgo);
        var encontrada = Assert.Single(resultado.OrdensServico);
        Assert.Equal(ordem.NumeroOS, encontrada.NumeroOS);
        Assert.Empty(resultado.Clientes);
        Assert.Empty(resultado.Aparelhos);
    }

    [Fact]
    public async Task BuscarAsync_PorCpfComPontuacao_RetornaClienteEOrdem()
    {
        await using var contexto = BancoEmMemoria.Criar();
        await AbrirOrdemPadraoAsync(contexto);

        var resultado = await Servico(contexto).BuscarAsync("123.456.789-09");

        var cliente = Assert.Single(resultado.Clientes);
        Assert.Equal("12345678909", cliente.Cpf);
        Assert.Single(resultado.OrdensServico);
    }

    [Fact]
    public async Task BuscarAsync_PorNome_RetornaClienteEOrdem()
    {
        await using var contexto = BancoEmMemoria.Criar();
        await AbrirOrdemPadraoAsync(contexto);

        var resultado = await Servico(contexto).BuscarAsync("Maria");

        var cliente = Assert.Single(resultado.Clientes);
        Assert.Equal("Maria Aparecida Souza", cliente.NomeCompleto);
        Assert.Single(resultado.OrdensServico);
    }

    [Fact]
    public async Task BuscarAsync_PorTelefoneDe11Digitos_RetornaClienteEOrdem()
    {
        await using var contexto = BancoEmMemoria.Criar();
        await AbrirOrdemPadraoAsync(contexto);

        // Celular BR tem 11 dígitos — mesmo tamanho do CPF, o ramo certo
        // precisa cobrir os dois documentos.
        var resultado = await Servico(contexto).BuscarAsync("11987654321");

        var cliente = Assert.Single(resultado.Clientes);
        Assert.Equal("11987654321", cliente.Telefone);
        Assert.Single(resultado.OrdensServico);
    }

    [Fact]
    public async Task BuscarAsync_PorImei_RetornaOrdemEAparelho()
    {
        await using var contexto = BancoEmMemoria.Criar();
        await AbrirOrdemPadraoAsync(contexto);

        var resultado = await Servico(contexto).BuscarAsync("352099001761481");

        var aparelho = Assert.Single(resultado.Aparelhos);
        Assert.Equal("352099001761481", aparelho.ImeiNumeroSerie);
        Assert.Single(resultado.OrdensServico);
        Assert.Empty(resultado.Clientes);
    }

    [Fact]
    public async Task BuscarAsync_TermoSemResultado_NaoEncontrouAlgo()
    {
        await using var contexto = BancoEmMemoria.Criar();
        await AbrirOrdemPadraoAsync(contexto);

        var resultado = await Servico(contexto).BuscarAsync("zzz nada parecido");

        Assert.False(resultado.EncontrouAlgo);
        Assert.Empty(resultado.OrdensServico);
        Assert.Empty(resultado.Clientes);
        Assert.Empty(resultado.Aparelhos);

        var emBranco = await Servico(contexto).BuscarAsync("   ");
        Assert.False( emBranco.EncontrouAlgo);
        Assert.Equal(string.Empty, emBranco.Termo);
    }

    [Fact]
    public async Task BuscarAsync_IncluiClienteInativo()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var cliente = BancoEmMemoria.NovoCliente();
        await new ClienteService(contexto).CriarAsync(cliente);
        await new ClienteService(contexto).DesativarAsync(cliente.Id);

        var resultado = await Servico(contexto).BuscarAsync("Maria");

        var encontrado = Assert.Single(resultado.Clientes);
        Assert.False(encontrado.Ativo);
    }
}
