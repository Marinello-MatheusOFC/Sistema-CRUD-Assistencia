using SistemaCrudAssistencia.Models.Validations;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Regra central de CPF (Etapa 10): validação de dígito verificador,
/// normalização para somente dígitos e mascaramento em logs.
/// Todos os CPFs abaixo são fictícios, criados apenas para teste.
/// </summary>
public class CpfTests
{
    [Theory]
    [InlineData("12345678909")]
    [InlineData("123.456.789-09")]
    public void EhValido_AceitaCpfFicticioComOuSemPontuacao(string valor) =>
        Assert.True(Cpf.EhValido(valor));

    [Theory]
    [InlineData("12345678901")] // dígito verificador incorreto
    [InlineData("11111111111")] // todos os dígitos repetidos
    [InlineData("123")] // menos de 11 dígitos
    [InlineData("123456789091")] // mais de 11 dígitos
    [InlineData("")]
    [InlineData(null)]
    public void EhValido_RejeitaEntradasInvalidas(string? valor) =>
        Assert.False(Cpf.EhValido(valor));

    [Fact]
    public void Normalizar_RemovePontuacaoEEntradasVazias()
    {
        Assert.Equal("12345678909", Cpf.Normalizar("123.456.789-09"));
        Assert.Equal(string.Empty, Cpf.Normalizar(null));
        Assert.Equal(string.Empty, Cpf.Normalizar("   "));
    }

    [Fact]
    public void Formatar_RetornaMascaraPadraoSomenteComOnzeDigitos()
    {
        Assert.Equal("123.456.789-09", Cpf.Formatar("12345678909"));
        Assert.Equal("123", Cpf.Formatar("123")); // sem 11 dígitos: devolve o original
    }

    [Fact]
    public void ParaLog_MascaraONumeroCompletoParaOsLogs()
    {
        Assert.Equal("***909", Cpf.ParaLog("123.456.789-09"));
        Assert.Equal("***", Cpf.ParaLog("123"));
    }
}
