using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Enums;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Regras críticas do fluxo de Ordens de Serviço: abertura, vínculo
/// cliente/aparelho, valores calculados no servidor, status e histórico.
/// </summary>
public class OrdemServicoServiceTests
{
    private const string Usuario = "balcao@assistencia";

    [Fact]
    public async Task AbrirAsync_GeraNumeroStatusTotalEHistoricoNoServidor()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        ordem.ValorPecas = 120.50m;
        ordem.ValorMaoDeObra = 79.50m;

        // Valores que um formulário adulterado tentaria impor — o servidor ignora todos.
        ordem.ValorTotal = 999_999m;
        ordem.NumeroOS = "0000-FALSO";
        ordem.Status = StatusOrdemServico.Entregue;
        ordem.DataConclusao = DateTime.Now;
        ordem.DataRetirada = DateTime.Now;

        await ordens.AbrirAsync(ordem, Usuario);

        var lida = await ordens.BuscarAsync(ordem.Id);

        Assert.NotNull(lida);
        Assert.Equal(StatusOrdemServico.Recebido, lida.Status);
        Assert.Equal("2026-000001", lida.NumeroOS);
        Assert.Equal(200.00m, lida.ValorTotal);
        Assert.Null(lida.DataConclusao);
        Assert.Null(lida.DataRetirada);

        var evento = Assert.Single(lida.Historico);
        Assert.Null(evento.StatusAnterior);
        Assert.Equal(StatusOrdemServico.Recebido, evento.NovoStatus);
        Assert.Equal(Usuario, evento.Usuario);
    }

    [Fact]
    public async Task AbrirAsync_RejeitaAparelhoQuePertenceAOutroCliente()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var clientes = new ClienteService(contexto);
        var aparelhos = new AparelhoService(contexto);
        var ordens = new OrdemServicoService(contexto);

        var clienteA = BancoEmMemoria.NovoCliente();
        await clientes.CriarAsync(clienteA);
        var clienteB = BancoEmMemoria.NovoCliente("93541134780", "João da Silva");
        await clientes.CriarAsync(clienteB);
        var aparelhoB = BancoEmMemoria.NovoAparelho(clienteB.Id);
        await aparelhos.CriarAsync(aparelhoB);

        var ordem = BancoEmMemoria.NovaOrdem(clienteA.Id, aparelhoB.Id);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => ordens.AbrirAsync(ordem, Usuario));

        Assert.Equal("O aparelho selecionado pertence a outro cliente.", erro.Message);
        Assert.Empty(contexto.OrdensServico);
    }

    [Fact]
    public async Task AbrirAsync_RejeitaClienteInativo()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var clientes = new ClienteService(contexto);
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        await clientes.DesativarAsync(cliente.Id);
        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => ordens.AbrirAsync(ordem, Usuario));

        Assert.Equal("Cliente está inativo. Reative-o antes de abrir uma nova OS.", erro.Message);
        Assert.Empty(contexto.OrdensServico);
    }

    [Fact]
    public async Task AbrirAsync_RejeitaClienteInexistente()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(clienteId: 999, aparelho.Id);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => ordens.AbrirAsync(ordem, Usuario));

        Assert.Equal("Cliente informado não existe.", erro.Message);
    }

    [Fact]
    public async Task AtualizarAsync_RecalculaValorTotalNoServidor()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);

        var edicao = new OrdemServico
        {
            Id = ordem.Id,
            DefeitoRelatado = "Troca de display",
            DiagnosticoTecnico = "Display original danificado.",
            ServicoRealizado = "Substituição do display.",
            ValorPecas = 300.10m,
            ValorMaoDeObra = 150.20m,
            ValorTotal = 1m // adulterado — o service ignora e recalcula
        };

        await ordens.AtualizarAsync(edicao);

        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal("Troca de display", lida.DefeitoRelatado);
        Assert.Equal(300.10m, lida.ValorPecas);
        Assert.Equal(150.20m, lida.ValorMaoDeObra);
        Assert.Equal(450.30m, lida.ValorTotal);
        Assert.Equal(StatusOrdemServico.Recebido, lida.Status);
    }

    [Fact]
    public async Task AtualizarAsync_NaoAlteraOsQueJaTerminou()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);
        await ordens.AlterarStatusAsync(ordem.Id, StatusOrdemServico.Cancelado, null, Usuario);

        var edicao = new OrdemServico { Id = ordem.Id, DefeitoRelatado = "Tentativa de edição" };

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => ordens.AtualizarAsync(edicao));

        Assert.Contains("Não é possível editar", erro.Message);
        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal(StatusOrdemServico.Cancelado, lida.Status);
        Assert.Equal("Tela quebrada após queda.", lida.DefeitoRelatado);
    }

    [Fact]
    public async Task AlterarStatusAsync_GeraHistoricoComUsuarioDaAutenticacao()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);
        await ordens.AlterarStatusAsync(ordem.Id, StatusOrdemServico.EmAnalise, "Verificar placa", Usuario);

        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal(StatusOrdemServico.EmAnalise, lida.Status);
        Assert.Equal(2, lida.Historico.Count);

        var evento = lida.Historico.Single(h => h.NovoStatus == StatusOrdemServico.EmAnalise);
        Assert.Equal(StatusOrdemServico.Recebido, evento.StatusAnterior);
        Assert.Equal("Verificar placa", evento.Observacao);
        Assert.Equal(Usuario, evento.Usuario);
    }

    [Fact]
    public async Task FinalizarAsync_MoveParaProntoEMarcaConclusao()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);
        await ordens.FinalizarAsync(ordem.Id, "Peça substituída.", Usuario);

        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal(StatusOrdemServico.Pronto, lida.Status);
        Assert.NotNull(lida.DataConclusao);
        Assert.Null(lida.DataRetirada);

        var evento = lida.Historico.Single(h => h.NovoStatus == StatusOrdemServico.Pronto);
        Assert.Equal(StatusOrdemServico.Recebido, evento.StatusAnterior);
        Assert.Equal("Peça substituída.", evento.Observacao);
        Assert.Equal(Usuario, evento.Usuario);
    }

    [Theory]
    [InlineData(StatusOrdemServico.Recebido)]
    [InlineData(StatusOrdemServico.EmAnalise)]
    [InlineData(StatusOrdemServico.EmReparo)]
    public async Task RegistrarRetiradaAsync_RejeitaQuandoNaoEstaPronto(StatusOrdemServico atual)
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);

        if (atual != StatusOrdemServico.Recebido)
            await ordens.AlterarStatusAsync(ordem.Id, atual, null, Usuario);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => ordens.RegistrarRetiradaAsync(ordem.Id, null, Usuario));

        Assert.Contains("Só é possível registrar a retirada quando a OS estiver com o status Pronto.", erro.Message);
        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal(atual, lida.Status);
        Assert.Null(lida.DataRetirada);
    }

    [Fact]
    public async Task RegistrarRetiradaAsync_DeProntoParaEntregue()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);
        await ordens.FinalizarAsync(ordem.Id, null, Usuario);
        await ordens.RegistrarRetiradaAsync(ordem.Id, "Retirado pelo cliente.", Usuario);

        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal(StatusOrdemServico.Entregue, lida.Status);
        Assert.NotNull(lida.DataConclusao);
        Assert.NotNull(lida.DataRetirada);
        Assert.Equal(StatusOrdemServico.Pronto, lida.Historico.Single(h => h.NovoStatus == StatusOrdemServico.Entregue).StatusAnterior);
    }

    [Fact]
    public async Task EntregueESaoCanceladoSaoSituacoesTerminais()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        // OS 1 até Entregue
        var ordem1 = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id, "Não carrega.");
        await ordens.AbrirAsync(ordem1, Usuario);
        await ordens.FinalizarAsync(ordem1.Id, null, Usuario);
        await ordens.RegistrarRetiradaAsync(ordem1.Id, null, Usuario);

        var erro1 = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => ordens.AlterarStatusAsync(ordem1.Id, StatusOrdemServico.EmReparo, null, Usuario));
        Assert.Contains("não aceita mais alterações de status", erro1.Message);

        // OS 2 até Cancelado
        var ordem2 = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id, "Fora de garantia.");
        await ordens.AbrirAsync(ordem2, Usuario);
        await ordens.AlterarStatusAsync(ordem2.Id, StatusOrdemServico.Cancelado, null, Usuario);

        var erro2 = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => ordens.AlterarStatusAsync(ordem2.Id, StatusOrdemServico.EmAnalise, null, Usuario));
        Assert.Contains("não aceita mais alterações de status", erro2.Message);

        var lida1 = await ordens.BuscarAsync(ordem1.Id);
        var lida2 = await ordens.BuscarAsync(ordem2.Id);
        Assert.Equal(StatusOrdemServico.Entregue, lida1!.Status);
        Assert.Equal(StatusOrdemServico.Cancelado, lida2!.Status);
    }
}
