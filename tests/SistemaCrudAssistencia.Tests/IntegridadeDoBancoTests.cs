using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Integridade do esquema (Etapa 10): índices únicos de CPF e NumeroOS,
/// IMEI não único (regra aprovada) e DeleteBehavior das chaves estrangeiras.
/// Nenhuma migration nova é criada por esta etapa.
/// </summary>
public class IntegridadeDoBancoTests
{
    [Fact]
    public void ModeloConfiguraIndiceUnicoParaCpfENumeroOsEImeiNaoUnico()
    {
        using var contexto = BancoEmMemoria.Criar();

        var clientes = contexto.Model.FindEntityType(typeof(Cliente))!;
        var indiceCpf = clientes.GetIndexes().Single(i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(Cliente.Cpf));
        Assert.True(indiceCpf.IsUnique);

        var ordens = contexto.Model.FindEntityType(typeof(OrdemServico))!;
        var indiceNumeroOs = ordens.GetIndexes().Single(i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(OrdemServico.NumeroOS));
        Assert.True(indiceNumeroOs.IsUnique);

        var aparelhos = contexto.Model.FindEntityType(typeof(Aparelho))!;
        var indiceImei = aparelhos.GetIndexes().Single(i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(Aparelho.ImeiNumeroSerie));
        Assert.False(indiceImei.IsUnique); // IMEI duplicado é permitido pela regra aprovada
    }

    [Fact]
    public void ModeloConfiguraDeleteBehaviorDasChavesEstrangeiras()
    {
        using var contexto = BancoEmMemoria.Criar();

        var clientes = contexto.Model.FindEntityType(typeof(Cliente))!;
        var aparelhos = contexto.Model.FindEntityType(typeof(Aparelho))!;
        var ordens = contexto.Model.FindEntityType(typeof(OrdemServico))!;
        var historicos = contexto.Model.FindEntityType(typeof(HistoricoOrdemServico))!;

        var osParaCliente = ordens.GetForeignKeys().Single(f => f.PrincipalEntityType == clientes);
        var osParaAparelho = ordens.GetForeignKeys().Single(f => f.PrincipalEntityType == aparelhos);
        var historicoParaOs = historicos.GetForeignKeys().Single(f => f.PrincipalEntityType == ordens);

        Assert.Equal(DeleteBehavior.Restrict, osParaCliente.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, osParaAparelho.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, historicoParaOs.DeleteBehavior);
    }

    [Fact]
    public async Task IndiceUnicoDeCpf_ImpedeSegundoClienteComMesmoCpfNoBanco()
    {
        await using var contexto = BancoEmMemoria.Criar();
        await new ClienteService(contexto).CriarAsync(BancoEmMemoria.NovoCliente("11144477735"));

        // Passe direto pela gravação: a proteção precisa existir no banco,
        // não apenas na pré-verificação do service (corrida entre duas telas).
        contexto.Clientes.Add(BancoEmMemoria.NovoCliente("11144477735", "Outra Pessoa"));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task IndiceUnicoDeNumeroOs_ImpedeDuplicidadeNoBanco()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        var primeira = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        primeira.NumeroOS = "2026-000001";
        contexto.OrdensServico.Add(primeira);
        await contexto.SaveChangesAsync();

        var duplicada = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id, "Outro defeito.");
        duplicada.NumeroOS = "2026-000001";
        contexto.OrdensServico.Add(duplicada);

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task ExcluirClienteComAparelho_EhBloqueadoPelaChaveEstrangeira()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        // O EF bloqueia a remoção no cliente (relação obrigatória) ou o banco
        // recusa via FK Restrict — nos dois casos o aparelho nunca fica órfão.
        var excecao = await Record.ExceptionAsync(async () =>
        {
            contexto.Clientes.Remove(cliente);
            await contexto.SaveChangesAsync();
        });

        Assert.True(
            excecao is InvalidOperationException or DbUpdateException,
            $"Remoção não deveria ser aceita: {excecao?.GetType().Name}");

        Assert.Equal(1, await contexto.Aparelhos.AsNoTracking().CountAsync());
        Assert.NotNull(await contexto.Aparelhos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == aparelho.Id));
    }
}
