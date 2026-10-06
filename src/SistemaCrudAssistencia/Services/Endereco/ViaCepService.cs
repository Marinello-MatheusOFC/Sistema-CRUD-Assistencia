using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SistemaCrudAssistencia.Models.Validations;

namespace SistemaCrudAssistencia.Services.Endereco;

/// <summary>
/// Consulta de CEP pelo ViaCEP (https://viacep.com.br) — gratuito e sem chave de API.
/// Chamada apenas no servidor; o navegador só recebe o endereço já preenchido.
/// </summary>
public class ViaCepService(HttpClient http, ILogger<ViaCepService> logger) : ICepService
{
    private sealed record ViaCepResposta(
        [property: JsonPropertyName("cep")] string Cep,
        [property: JsonPropertyName("logradouro")] string Logradouro,
        [property: JsonPropertyName("complemento")] string? Complemento,
        [property: JsonPropertyName("bairro")] string Bairro,
        [property: JsonPropertyName("localidade")] string Localidade,
        [property: JsonPropertyName("uf")] string Uf);

    public async Task<CepResultado?> BuscarAsync(string? cep, CancellationToken cancellationToken = default)
    {
        var normalizado = Cep.Normalizar(cep);

        if (normalizado.Length != 8)
            return null;

        try
        {
            var resposta = await http.GetFromJsonAsync<ViaCepResposta>(
                $"https://viacep.com.br/ws/{normalizado}/json/", cancellationToken);

            if (resposta is null || string.IsNullOrWhiteSpace(resposta.Localidade))
                return null;

            return new CepResultado(
                Cep: normalizado,
                Logradouro: resposta.Logradouro ?? string.Empty,
                Numero: null,
                Complemento: string.IsNullOrWhiteSpace(resposta.Complemento) ? null : resposta.Complemento,
                Bairro: resposta.Bairro ?? string.Empty,
                Cidade: resposta.Localidade,
                Estado: resposta.Uf ?? string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Falha de rede não pode impedir o cadastro: o usuário digita o endereço.
            logger.LogWarning(ex, "Falha ao consultar o CEP no ViaCEP.");
            return null;
        }
    }
}
