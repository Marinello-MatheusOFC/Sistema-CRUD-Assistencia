using SistemaCrudAssistencia.Models.Enums;

namespace SistemaCrudAssistencia.Models.Filtros;

/// <summary>
/// Critérios de busca da listagem de Ordens de Serviço.
/// </summary>
public class FiltroOrdensServico
{
    /// <summary>Termo livre: número da OS, CPF, nome, telefone ou IMEI/número de série.</summary>
    public string? Busca { get; set; }

    public StatusOrdemServico? Status { get; set; }

    public bool SomenteEmAndamento { get; set; }

    public int? ClienteId { get; set; }

    public DateOnly? De { get; set; }

    public DateOnly? Ate { get; set; }

    /// <summary>True quando algum filtro além da paginação foi aplicado.</summary>
    public bool TemFiltro =>
        !string.IsNullOrWhiteSpace(Busca)
        || Status.HasValue
        || SomenteEmAndamento
        || ClienteId.HasValue
        || De.HasValue
        || Ate.HasValue;

    public void Limpar()
    {
        Busca = null;
        Status = null;
        SomenteEmAndamento = false;
        ClienteId = null;
        De = null;
        Ate = null;
    }
}
