using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Regras puras da entidade OS (Etapa 10): geração determinística do NumeroOS
/// e soma em decimal — sem banco e sem depender do relógio.
/// </summary>
public class OrdemServicoEntidadeTests
{
    [Fact]
    public void GerarNumeroOS_UsaAnoDaDataDeEntradaEIdComSeisDigitos()
    {
        var ordem = new OrdemServico { Id = 42, DataEntrada = new DateTime(2026, 3, 10) };
        ordem.GerarNumeroOS();
        Assert.Equal("2026-000042", ordem.NumeroOS);

        var outra = new OrdemServico { Id = 7, DataEntrada = new DateTime(2024, 12, 31) };
        outra.GerarNumeroOS();
        Assert.Equal("2024-000007", outra.NumeroOS);
    }

    [Fact]
    public void CalcularValorTotal_UsaDecimalSemErroDePontoFlutuante()
    {
        var ordem = new OrdemServico { ValorPecas = 0.10m, ValorMaoDeObra = 0.20m };

        // Em double isto daria 0.30000000000000004; em decimal, 0.30.
        Assert.Equal(0.30m, ordem.CalcularValorTotal());

        ordem.AtualizarValorTotal();
        Assert.Equal(0.30m, ordem.ValorTotal);
    }
}
