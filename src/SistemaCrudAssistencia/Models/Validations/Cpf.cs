using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SistemaCrudAssistencia.Models.Validations;

/// <summary>
/// Normaliza, valida e formata CPF brasileiro.
/// Regra central do sistema: o CPF é SEMPRE guardado somente com dígitos,
/// o que elimina cadastros duplicados por diferença de pontuação.
/// </summary>
public static partial class Cpf
{
    [GeneratedRegex(@"\D")]
    private static partial Regex SomenteDigitosRegex();

    /// <summary>
    /// Remove tudo que não for dígito. Null/vazio retorna string vazia.
    /// </summary>
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        return string.Concat(valor.Where(char.IsAsciiDigit));
    }

    /// <summary>
    /// CPF válido: 11 dígitos, não repetidos e com os dois dígitos verificadores corretos.
    /// </summary>
    public static bool EhValido(string? valor)
    {
        var cpf = Normalizar(valor);

        if (cpf.Length != 11)
            return false;

        // Bloqueia 00000000000, 11111111111, 99999999999, etc.
        if (cpf.All(c => c == cpf[0]))
            return false;

        return DigitoVerificador(cpf, 9) == cpf[9]
            && DigitoVerificador(cpf, 10) == cpf[10];
    }

    /// <summary>
    /// Exibe como 000.000.000-00. Retorna o original se não tiver 11 dígitos.
    /// </summary>
    public static string Formatar(string? valor)
    {
        var cpf = Normalizar(valor);

        if (cpf.Length != 11)
            return valor ?? string.Empty;

        return $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";
    }

    /// <summary>
    /// Recorta o CPF para não expor o documento completo em logs.
    /// </summary>
    public static string ParaLog(string? valor)
    {
        var cpf = Normalizar(valor);
        return cpf.Length == 11 ? $"***{cpf[^3..]}" : "***";
    }

    private static int DigitoVerificador(string cpf, int posicao)
    {
        var soma = 0;
        var peso = posicao + 1;

        for (var i = 0; i < posicao; i++)
        {
            soma += (cpf[i] - '0') * peso;
            peso--;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}

/// <summary>
/// Atributo de DataAnnotations para validar e normalizar CPF em ViewModels.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CpfAttribute : ValidationAttribute
{
    public CpfAttribute() => ErrorMessage = "CPF inválido.";

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        return value is string s && Cpf.EhValido(s);
    }
}

/// <summary>
/// Atributo para telefone brasileiro: 10 dígitos (fixo) ou 11 dígitos (celular com 9).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TelefoneAttribute : ValidationAttribute
{
    public TelefoneAttribute() => ErrorMessage = "Telefone inválido. Use DDD + número.";

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        return value is string s && Telefone.EhValido(s);
    }
}

/// <summary>
/// Atributo para CEP brasileiro: 8 dígitos.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CepAttribute : ValidationAttribute
{
    public CepAttribute() => ErrorMessage = "CEP inválido. Deve conter 8 dígitos.";

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        return value is string s && Cep.EhValido(s);
    }
}
