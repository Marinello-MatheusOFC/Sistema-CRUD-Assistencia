namespace SistemaCrudAssistencia.Services.Endereco;

public record CepResultado(
    string Cep,
    string Logradouro,
    string? Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado);

public interface ICepService
{
    /// <summary>
    /// Consulta a API gratuita do ViaCEP. Devolve null quando o CEP não existe
    /// ou quando não há internet — o cadastro continua funcionando digitando à mão.
    /// </summary>
    Task<CepResultado?> BuscarAsync(string? cep, CancellationToken cancellationToken = default);
}
