using System.Globalization;

namespace SistemaCrudAssistencia.Models.Validations;

/// <summary>
/// Conversão de valores monetários digitados pelo usuário.
/// Aceita "1.500,50", "150,50", "150.50", "R$ 150,50" e "150".
/// A conversão é sempre feita no servidor — o JavaScript é apenas conveniência.
/// </summary>
public static class ValoresMonetarios
{
    /// <summary>
    /// Teto de segurança para qualquer valor monetário do sistema (R$ 1.000.000,00).
    /// </summary>
    public const decimal Maximo = 1_000_000m;

    public static bool TentarConverter(string? entrada, out decimal valor)
    {
        valor = 0m;

        if (string.IsNullOrWhiteSpace(entrada))
            return true;

        var texto = entrada.Trim()
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Replace(" ", string.Empty)
            .Replace('\u00A0'.ToString(), string.Empty)
            .Trim();

        if (texto.Length == 0)
            return true;

        // "1.500,50" -> vírgula é decimal e ponto é separador de milhar
        if (texto.Contains(','))
            texto = texto.Replace(".", string.Empty).Replace(',', '.');

        if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var convertido))
            return false;

        // Arredonda para 2 casas, como dinheiro
        valor = Math.Round(convertido, 2, MidpointRounding.AwayFromZero);

        if (valor < 0 || valor > Maximo)
            return false;

        return true;
    }

    /// <summary>
    /// Versão com valor padrão: devolve 0 quando o texto é vazio ou inválido.
    /// </summary>
    public static decimal Converter(string? entrada) =>
        TentarConverter(entrada, out var valor) ? valor : 0m;

    public static string Formatar(decimal valor) => valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
}
