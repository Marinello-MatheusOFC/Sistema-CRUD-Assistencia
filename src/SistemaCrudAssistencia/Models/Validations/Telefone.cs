namespace SistemaCrudAssistencia.Models.Validations;

/// <summary>
/// Telefone brasileiro: DDD (2 dígitos) + número (8 ou 9 dígitos).
/// Armazenado somente com dígitos.
/// </summary>
public static class Telefone
{
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        return string.Concat(valor.Where(char.IsAsciiDigit));
    }

    public static bool EhValido(string? valor)
    {
        var telefone = Normalizar(valor);

        return telefone.Length is 10 or 11
            && !telefone[..2].All(c => c == '0');
    }

    public static bool EhCelular(string? valor) => Normalizar(valor).Length == 11;

    /// <summary>
    /// Formata como (11) 99999-9999 ou (11) 3333-3333.
    /// </summary>
    public static string Formatar(string? valor)
    {
        var telefone = Normalizar(valor);

        return telefone.Length switch
        {
            11 => $"({telefone[..2]}) {telefone[2..7]}-{telefone[7..]}",
            10 => $"({telefone[..2]}) {telefone[2..6]}-{telefone[6..]}",
            _ => valor ?? string.Empty
        };
    }

    /// <summary>
    /// Recorta o telefone para não expor o número completo em logs.
    /// </summary>
    public static string ParaLog(string? valor)
    {
        var telefone = Normalizar(valor);
        return telefone.Length >= 4 ? $"***{telefone[^4..]}" : "***";
    }
}

/// <summary>
/// CEP brasileiro: 8 dígitos.
/// </summary>
public static class Cep
{
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        return string.Concat(valor.Where(char.IsAsciiDigit));
    }

    public static bool EhValido(string? valor) => Normalizar(valor).Length == 8;

    public static string Formatar(string? valor)
    {
        var cep = Normalizar(valor);
        return cep.Length == 8 ? $"{cep[..5]}-{cep[5..]}" : valor ?? string.Empty;
    }
}
