using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Lacunas da matriz Etapa 10 em ClienteService: duplicidade de CPF na edição,
/// busca por e-mail (tradução EF em SQLite), localização por CPF formatado e
/// preservação das OS ao desativar. CPFs fictícios apenas para teste.
/// </summary>
public class ClienteRegrasCriticasTests
{
    private const string Usuario = "balcao@assistencia";

    [Fact]
    public async Task AtualizarAsync_RejeitaCpfQueJaPertenceAOutroCliente()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var service = new ClienteService(contexto);
        var primeiro = BancoEmMemoria.NovoCliente("12345678909");
        var segundo = BancoEmMemoria.NovoCliente("11144477735", "João da Silva");
        await service.CriarAsync(primeiro);
        await service.CriarAsync(segundo);

        var carga = await service.BuscarPorIdAsync(segundo.Id);
        Assert.NotNull(carga);
        carga.Cpf = "123.456.789-09"; // CPF já usado pelo primeiro, com pontuação

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => service.AtualizarAsync(carga));

        Assert.Equal("Já existe um cliente cadastrado com este CPF.", erro.Message);
        var noBanco = await contexto.Clientes.AsNoTracking().SingleAsync(c => c.Id == segundo.Id);
        Assert.Equal("11144477735", noBanco.Cpf);
    }

    [Fact]
    public async Task ListarAsync_PorEmail_EncontraClienteIndependentementeDeMaiusculas()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var service = new ClienteService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        cliente.Email = "maria.souza@exemplo.com";
        await service.CriarAsync(cliente);

        // Termo com '@': cai no ramo de e-mail — que não bate com nome nem CPF.
        var encontrados = await service.ListarAsync("MARIA.SOUZA@EXEMPLO.COM");

        var unico = Assert.Single(encontrados);
        Assert.Equal(cliente.Id, unico.Id);

        Assert.Empty(await service.ListarAsync("inexistente@exemplo.com"));
    }

    [Fact]
    public async Task BuscarPorCpfAsync_NormalizaPontuacaoECpfCurtoRetornaNulo()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var service = new ClienteService(contexto);
        var cliente = BancoEmMemoria.NovoCliente("11144477735");
        await service.CriarAsync(cliente);

        var porCpfFormatado = await service.BuscarPorCpfAsync("111.444.777-35");

        Assert.NotNull(porCpfFormatado);
        Assert.Equal(cliente.Id, porCpfFormatado.Id);

        Assert.Null(await service.BuscarPorCpfAsync("111")); // menos de 11 dígitos: não consulta
    }

    [Fact]
    public async Task DesativarAsync_PreservaOrdensDeServicoDoCliente()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var service = new ClienteService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);
        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        await new OrdemServicoService(contexto).AbrirAsync(ordem, Usuario);

        await service.DesativarAsync(cliente.Id);

        var detalhes = await service.BuscarDetalhesAsync(cliente.Id);

        Assert.NotNull(detalhes);
        Assert.False(detalhes.Ativo);
        var os = Assert.Single(detalhes.OrdensServico);
        Assert.Equal(ordem.Id, os.Id);
    }
}
