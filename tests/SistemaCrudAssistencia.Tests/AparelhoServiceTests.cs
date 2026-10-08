using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

public class AparelhoServiceTests
{
    [Fact]
    public async Task CriarAsync_AssociaAoClienteExistenteEPermiteImeiVazio()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var clientes = new ClienteService(contexto);
        var aparelhos = new AparelhoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        await clientes.CriarAsync(cliente);
        var aparelho = BancoEmMemoria.NovoAparelho(cliente.Id);
        aparelho.ImeiNumeroSerie = "   ";

        await aparelhos.CriarAsync(aparelho);

        var cadastrados = await aparelhos.ListarPorClienteAsync(cliente.Id);
        Assert.Single(cadastrados);
        Assert.Equal(cliente.Id, cadastrados[0].ClienteId);
        Assert.Null(cadastrados[0].ImeiNumeroSerie);
    }

    [Fact]
    public async Task CriarAsync_RejeitaClienteInexistente()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var aparelhos = new AparelhoService(contexto);
        var aparelho = BancoEmMemoria.NovoAparelho(clienteId: 999);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => aparelhos.CriarAsync(aparelho));

        Assert.Equal("Cliente informado não existe.", erro.Message);
    }

    [Fact]
    public async Task AtualizarAsync_PreservaProprietarioMesmoComClienteInformadoDiferente()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var clientes = new ClienteService(contexto);
        var aparelhos = new AparelhoService(contexto);
        var clienteA = BancoEmMemoria.NovoCliente();
        var clienteB = BancoEmMemoria.NovoCliente("93541134780", "João da Silva");
        await clientes.CriarAsync(clienteA);
        await clientes.CriarAsync(clienteB);
        var aparelho = BancoEmMemoria.NovoAparelho(clienteA.Id);
        await aparelhos.CriarAsync(aparelho);

        aparelho.ClienteId = clienteB.Id;
        aparelho.Modelo = "Galaxy S22";
        await aparelhos.AtualizarAsync(aparelho);

        var atualizado = await aparelhos.BuscarAsync(aparelho.Id);
        Assert.NotNull(atualizado);
        Assert.Equal(clienteA.Id, atualizado.ClienteId);
        Assert.Equal("Galaxy S22", atualizado.Modelo);
    }

    [Fact]
    public async Task CriarAsync_PermiteImeiDuplicadoPoisIndiceNaoEhUnico()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var clientes = new ClienteService(contexto);
        var aparelhos = new AparelhoService(contexto);
        var clienteA = BancoEmMemoria.NovoCliente();
        var clienteB = BancoEmMemoria.NovoCliente("93541134780", "João da Silva");
        await clientes.CriarAsync(clienteA);
        await clientes.CriarAsync(clienteB);
        var primeiro = BancoEmMemoria.NovoAparelho(clienteA.Id, "SERIAL-IGUAL");
        var segundo = BancoEmMemoria.NovoAparelho(clienteB.Id, "SERIAL-IGUAL");

        await aparelhos.CriarAsync(primeiro);
        await aparelhos.CriarAsync(segundo);

        var encontrados = await aparelhos.BuscarPorImeiAsync("SERIAL-IGUAL");
        Assert.Equal(2, encontrados.Count);
    }

    [Fact]
    public async Task BuscarDetalhesAsync_IncluiHistoricoDeOrdensDeServico()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var aparelhos = new AparelhoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);
        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await new OrdemServicoService(contexto).AbrirAsync(ordem, "balcao@assistencia");

        var detalhes = await aparelhos.BuscarDetalhesAsync(aparelho.Id);

        Assert.NotNull(detalhes);
        Assert.Equal(cliente.Id, detalhes.ClienteId);
        var os = Assert.Single(detalhes.OrdensServico);
        Assert.Equal(ordem.Id, os.Id);
    }
}
