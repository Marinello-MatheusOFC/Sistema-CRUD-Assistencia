using SistemaCrudAssistencia.Models.Validations;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Valores monetários digitados pelo usuário (Etapa 10): formatações aceitas,
/// teto de segurança, arredondamento e conversão no servidor.
/// </summary>
public class ValoresMonetariosTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("150", 150.00)]
    [InlineData("150,50", 150.50)]
    [InlineData("150.50", 150.50)]
    [InlineData("1.500,50", 1500.50)]
    [InlineData("R$ 150,50", 150.50)]
    public void TentarConverter_AceitaFormatosComunsDeDinheiro(string? entrada, decimal esperado)
    {
        var convertido = ValoresMonetarios.TentarConverter(entrada, out var valor);

        Assert.True(convertido);
        Assert.Equal(esperado, valor);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("1.500.50")] // separador decimal ambíguo sem vírgula
    public void TentarConverter_RejeitaEntradasInvalidas(string entrada) =>
        // O contrato é o bool: o valor fora do intervalo é descartado.
        Assert.False(ValoresMonetarios.TentarConverter(entrada, out _));

    [Fact]
    public void TentarConverter_ArredondaParaDuasCasasAwayFromZero()
    {
        Assert.True(ValoresMonetarios.TentarConverter("150,555", out var valor));
        Assert.Equal(150.56m, valor);
    }

    [Fact]
    public void TentarConverter_AceitaExatamenteOTetoERejeitaUmCentavoAcima()
    {
        Assert.True(ValoresMonetarios.TentarConverter("1000000", out var maximo));
        Assert.Equal(ValoresMonetarios.Maximo, maximo);

        Assert.False(ValoresMonetarios.TentarConverter("1000000,01", out _));
    }

    [Fact]
    public void Converter_DevolveZeroQuandoAEntradaEhInvalida()
    {
        Assert.Equal(0m, ValoresMonetarios.Converter("abc"));
        Assert.Equal(0m, ValoresMonetarios.Converter("-5"));
        Assert.Equal(150.50m, ValoresMonetarios.Converter("150,50"));
    }
}
