using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Enums;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Lacunas da matriz Etapa 10 no fluxo de OS: aparelho inexistente,
/// preenchimento da data de entrada, estados terminais, status intermediários
/// e acumulação do histórico após várias mudanças.
/// </summary>
public class OrdemServicoRegrasCriticasTests
{
    private const string Usuario = "balcao@assistencia";

    [Fact]
    public async Task AbrirAsync_RejeitaAparelhoInexistente()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        await new ClienteService(contexto).CriarAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelhoId: 999);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => ordens.AbrirAsync(ordem, Usuario));

        Assert.Equal("Aparelho informado não existe.", erro.Message);
        Assert.Empty(contexto.OrdensServico);
    }

    [Fact]
    public async Task AbrirAsync_PreencheDataDeEntradaQuandoNaoInformada()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        ordem.DataEntrada = default; // formulário adulterado/omisso

        await ordens.AbrirAsync(ordem, Usuario);

        Assert.NotEqual(default, ordem.DataEntrada); // preenchida pela aplicação
        Assert.True(ordem.DataEntrada <= DateTime.Now.AddMinutes(1));
    }

    [Fact]
    public async Task FinalizarAsync_OrdemCancelada_NaoAceitaMudanca()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);
        await ordens.AlterarStatusAsync(ordem.Id, StatusOrdemServico.Cancelado, null, Usuario);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => ordens.FinalizarAsync(ordem.Id, null, Usuario));

        Assert.Contains("não aceita mais alterações de status", erro.Message);
        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal(StatusOrdemServico.Cancelado, lida.Status);
        Assert.Null(lida.DataConclusao);
    }

    [Fact]
    public async Task Historico_AcumulaTodosOsEventosSemPerderOPrimeiro()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);
        await ordens.AlterarStatusAsync(ordem.Id, StatusOrdemServico.EmAnalise, null, Usuario);
        await ordens.AlterarStatusAsync(ordem.Id, StatusOrdemServico.EmReparo, null, Usuario);

        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal(3, lida.Historico.Count);

        var abertura = lida.Historico.Single(h => h.StatusAnterior == null);
        Assert.Equal(StatusOrdemServico.Recebido, abertura.NovoStatus);

        var meio = lida.Historico.Single(h => h.NovoStatus == StatusOrdemServico.EmAnalise);
        Assert.Equal(StatusOrdemServico.Recebido, meio.StatusAnterior);

        var fim = lida.Historico.Single(h => h.NovoStatus == StatusOrdemServico.EmReparo);
        Assert.Equal(StatusOrdemServico.EmAnalise, fim.StatusAnterior);
    }

    [Theory]
    [InlineData(StatusOrdemServico.Entregue)]
    [InlineData(StatusOrdemServico.Cancelado)]
    public async Task EstadosTerminais_NaoVoltamParaNenhumStatusEmAndamento(StatusOrdemServico terminal)
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);

        if (terminal == StatusOrdemServico.Entregue)
        {
            await ordens.FinalizarAsync(ordem.Id, null, Usuario);
            await ordens.RegistrarRetiradaAsync(ordem.Id, null, Usuario);
        }
        else
        {
            await ordens.AlterarStatusAsync(ordem.Id, terminal, null, Usuario);
        }

        var alvos = new[]
        {
            StatusOrdemServico.Recebido,
            StatusOrdemServico.EmAnalise,
            StatusOrdemServico.EmReparo,
            StatusOrdemServico.Pronto
        };

        foreach (var alvo in alvos)
        {
            var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
                () => ordens.AlterarStatusAsync(ordem.Id, alvo, null, Usuario));
            Assert.Contains("não aceita mais alterações de status", erro.Message);
        }

        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal(terminal, lida.Status);
    }

    [Theory]
    [InlineData(StatusOrdemServico.EmAnalise, StatusOrdemServico.AguardandoAprovacao, false)]
    [InlineData(StatusOrdemServico.AguardandoAprovacao, StatusOrdemServico.AguardandoPeca, false)]
    [InlineData(StatusOrdemServico.AguardandoPeca, StatusOrdemServico.EmReparo, false)]
    [InlineData(StatusOrdemServico.Pronto, StatusOrdemServico.EmReparo, true)]
    public async Task TransicaoDentroDoFluxoEmAndamento_EhPermitida(
        StatusOrdemServico atual, StatusOrdemServico destino, bool chegarPeloFinalizar)
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await ordens.AbrirAsync(ordem, Usuario);

        if (chegarPeloFinalizar)
            await ordens.FinalizarAsync(ordem.Id, null, Usuario);
        else
            await ordens.AlterarStatusAsync(ordem.Id, atual, null, Usuario);

        await ordens.AlterarStatusAsync(ordem.Id, destino, null, Usuario);

        var lida = await ordens.BuscarAsync(ordem.Id);
        Assert.NotNull(lida);
        Assert.Equal(destino, lida.Status);
        Assert.Equal(3, lida.Historico.Count);

        var evento = lida.Historico.Single(h => h.NovoStatus == destino);
        Assert.Equal(atual, evento.StatusAnterior);
    }
}
