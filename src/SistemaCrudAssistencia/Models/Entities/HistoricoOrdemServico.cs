using SistemaCrudAssistencia.Models.Enums;
using SistemaCrudAssistencia.Models.Extensions;

namespace SistemaCrudAssistencia.Models.Entities;

/// <summary>
/// Registro de tudo que aconteceu com a OS. Preserva a memória do atendimento
/// mesmo quando o status atual muda várias vezes.
/// </summary>
public class HistoricoOrdemServico
{
    public int Id { get; set; }

    public int OrdemServicoId { get; set; }

    public OrdemServico? OrdemServico { get; set; }

    public DateTime Data { get; set; }

    /// <summary>Status antes da alteração. Nulo quando a OS foi criada.</summary>
    public StatusOrdemServico? StatusAnterior { get; set; }

    public StatusOrdemServico? NovoStatus { get; set; }

    public string? Observacao { get; set; }

    /// <summary>E-mail do usuário autenticado que registrou o evento, quando houver.</summary>
    public string? Usuario { get; set; }

    /// <summary>Descrição pronta para exibição, já no formato do histórico.</summary>
    public string Descricao =>
        StatusAnterior is null
            ? $"OS criada com status {NovoStatus?.Descricao() ?? "—"}."
            : $"Status alterado de {StatusAnterior.Value.Descricao()} para {NovoStatus?.Descricao() ?? "—"}.";
}
