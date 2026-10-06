using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

public class TesteDeCompatibilidadeSqlite
{
    [Fact]
    public async Task GravaELeDatasComNumeroDaOsGerado()
    {
        await using var contexto = BancoEmMemoria.Criar();
        var ordens = new OrdemServicoService(contexto);

        var cliente = BancoEmMemoria.NovoCliente();
        var aparelho = await contexto.ClienteComAparelhoAsync(cliente, "IMEI-TESTE-1");

        var entrada = new DateTime(2026, 3, 10, 14, 35, 0, DateTimeKind.Unspecified);
        var ordem = BancoEmMemoria.NovaOrdem(cliente.Id, aparelho.Id);
        ordem.DataEntrada = entrada;

        await ordens.AbrirAsync(ordem, "teste@local");

        var lida = await ordens.BuscarAsync(ordem.Id);

        Assert.NotNull(lida);
        Assert.Equal(entrada, lida.DataEntrada);
        Assert.Equal("2026-000001", lida.NumeroOS);
        Assert.NotNull(lida.Aparelho);
        Assert.NotNull(lida.Cliente);
        Assert.Single(lida.Historico);
    }
}
