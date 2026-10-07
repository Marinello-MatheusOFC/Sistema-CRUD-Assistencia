using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

public class ClienteServiceTests
{
    [Fact]
    public async Task CriarAsync_NormalizaCpfERejeitaDuplicidade()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var service = new ClienteService(contexto);
        var cliente = BancoEmMemoria.NovoCliente("123.456.789-09");

        await service.CriarAsync(cliente);

        Assert.Equal("12345678909", cliente.Cpf);

        var duplicado = BancoEmMemoria.NovoCliente("12345678909", "Outro Cliente");
        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => service.CriarAsync(duplicado));

        Assert.Equal("Já existe um cliente cadastrado com este CPF.", erro.Message);
    }

    [Fact]
    public async Task AtualizarAsync_AlteraDadosESegueNormalizandoDocumentos()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var service = new ClienteService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        await service.CriarAsync(cliente);

        cliente.NomeCompleto = "  Maria Souza Atualizada  ";
        cliente.Cpf = "935.411.347-80";
        cliente.Telefone = "(11) 3333-4444";
        cliente.Cep = "01001-000";
        await service.AtualizarAsync(cliente);

        var atualizado = await service.BuscarPorIdAsync(cliente.Id);
        Assert.NotNull(atualizado);
        Assert.Equal("Maria Souza Atualizada", atualizado.NomeCompleto);
        Assert.Equal("93541134780", atualizado.Cpf);
        Assert.Equal("1133334444", atualizado.Telefone);
        Assert.Equal("01001000", atualizado.Cep);
    }

    [Fact]
    public async Task DesativarEReativarAsync_PreservamCadastroERelacionamentos()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var service = new ClienteService(contexto);
        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente);

        await service.DesativarAsync(cliente.Id);
        var desativado = await service.BuscarDetalhesAsync(cliente.Id);

        Assert.NotNull(desativado);
        Assert.False(desativado.Ativo);
        Assert.Contains(desativado.Aparelhos, a => a.Id == aparelho.Id);

        await service.ReativarAsync(cliente.Id);
        var reativado = await service.BuscarPorIdAsync(cliente.Id);

        Assert.NotNull(reativado);
        Assert.True(reativado.Ativo);
    }
}
