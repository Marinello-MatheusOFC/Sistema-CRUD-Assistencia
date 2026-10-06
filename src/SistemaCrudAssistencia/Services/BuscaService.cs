using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Filtros;

namespace SistemaCrudAssistencia.Services;

/// <summary>
/// Resultado da busca rápida da tela principal.
/// </summary>
public class ResultadoBusca
{
    public string Termo { get; init; } = string.Empty;

    public List<OrdemServico> OrdensServico { get; init; } = [];

    public List<Cliente> Clientes { get; init; } = [];

    public bool EncontrouAlgo => OrdensServico.Count > 0 || Clientes.Count > 0;
}

public interface IBuscaService
{
    /// <summary>
    /// Busca rápida por número da OS, CPF, nome, telefone ou IMEI/número de série.
    /// </summary>
    Task<ResultadoBusca> BuscarAsync(string? termo, CancellationToken cancellationToken = default);
}

public class BuscaService(IClienteService clientes, IOrdemServicoService ordensServico) : IBuscaService
{
    public async Task<ResultadoBusca> BuscarAsync(string? termo, CancellationToken cancellationToken = default)
    {
        var busca = termo?.Trim() ?? string.Empty;

        if (busca.Length == 0)
            return new ResultadoBusca();

        var ordens = await ordensServico.ListarAsync(new FiltroOrdensServico { Busca = busca }, cancellationToken);
        var clientesEncontrados = await clientes.ListarAsync(busca, somenteAtivos: false, cancellationToken);

        return new ResultadoBusca
        {
            Termo = busca,
            OrdensServico = ordens.Take(25).ToList(),
            Clientes = clientesEncontrados.Take(10).ToList()
        };
    }
}
