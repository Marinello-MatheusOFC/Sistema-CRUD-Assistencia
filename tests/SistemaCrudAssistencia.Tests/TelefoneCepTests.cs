using SistemaCrudAssistencia.Models.Validations;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Telefone e CEP (Etapa 10): DDD + número (10 ou 11 dígitos) e CEP de
/// exatamente 8 dígitos, com normalização, formatação e mascaramento.
/// </summary>
public class TelefoneCepTests
{
    [Theory]
    [InlineData("1133334444")] // fixo: 10 dígitos
    [InlineData("(11) 98765-4321")] // celular: 11 dígitos com pontuação
    public void Telefone_EhValido_AceitaFixoECelular(string valor) =>
        Assert.True(Telefone.EhValido(valor));

    [Theory]
    [InlineData("123456789")] // 9 dígitos
    [InlineData("1234")] // curto demais
    [InlineData("(00) 3333-4444")] // DDD 00 não existe
    [InlineData(null)]
    public void Telefone_EhValido_RejeitaEntradasInvalidas(string? valor) =>
        Assert.False(Telefone.EhValido(valor));

    [Fact]
    public void Telefone_NormalizarEFormatar_GeramDigitosEMascara()
    {
        Assert.Equal("11987654321", Telefone.Normalizar("(11) 98765-4321"));
        Assert.Equal("(11) 98765-4321", Telefone.Formatar("11987654321"));
        Assert.Equal("(11) 3333-4444", Telefone.Formatar("11 3333-4444"));
    }

    [Fact]
    public void Telefone_ParaLog_MascaraONumeroCompletoParaOsLogs()
    {
        Assert.Equal("***4321", Telefone.ParaLog("(11) 98765-4321"));
        Assert.Equal("***", Telefone.ParaLog("11"));
    }

    [Theory]
    [InlineData("01001-000", true)]
    [InlineData("01001000", true)]
    [InlineData("0100100", false)] // 7 dígitos
    [InlineData("010010000", false)] // 9 dígitos
    [InlineData(null, false)]
    public void Cep_EhValido_ExigeExatamenteOitoDigitos(string? valor, bool esperado) =>
        Assert.Equal(esperado, Cep.EhValido(valor));

    [Fact]
    public void Cep_NormalizarEFormatar()
    {
        Assert.Equal("01001000", Cep.Normalizar("01001-000"));
        Assert.Equal("01001-000", Cep.Formatar("01001000"));
        Assert.Equal("0100100", Cep.Formatar("0100100")); // inválido: devolve o original
    }
}
