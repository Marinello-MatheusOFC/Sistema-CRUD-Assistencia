using SistemaCrudAssistencia.Models.Enums;

namespace SistemaCrudAssistencia.Models.Extensions;

public static class StatusOrdemServicoExtensions
{
    public static string Descricao(this StatusOrdemServico status) => status switch
    {
        StatusOrdemServico.Recebido => "Recebido",
        StatusOrdemServico.EmAnalise => "Em análise",
        StatusOrdemServico.AguardandoAprovacao => "Aguardando aprovação",
        StatusOrdemServico.AguardandoPeca => "Aguardando peça",
        StatusOrdemServico.EmReparo => "Em reparo",
        StatusOrdemServico.Pronto => "Pronto",
        StatusOrdemServico.Entregue => "Entregue",
        StatusOrdemServico.Cancelado => "Cancelado",
        _ => "Desconhecido"
    };

    /// <summary>
    /// Classe CSS do badge Bootstrap usada para o status na interface.
    /// </summary>
    public static string BadgeCss(this StatusOrdemServico status) => status switch
    {
        StatusOrdemServico.Recebido => "text-bg-secondary",
        StatusOrdemServico.EmAnalise => "text-bg-info",
        StatusOrdemServico.AguardandoAprovacao => "text-bg-warning",
        StatusOrdemServico.AguardandoPeca => "text-bg-danger",
        StatusOrdemServico.EmReparo => "text-bg-primary",
        StatusOrdemServico.Pronto => "text-bg-success",
        StatusOrdemServico.Entregue => "text-bg-dark",
        StatusOrdemServico.Cancelado => "text-bg-light border",
        _ => "text-bg-secondary"
    };

    /// <summary>
    /// A OS ainda está com o aparelho na assistência?
    /// </summary>
    public static bool EstaEmAndamento(this StatusOrdemServico status) =>
        status is not (StatusOrdemServico.Entregue or StatusOrdemServico.Cancelado);

    /// <summary>
    /// O aparelho pode ser retirado pelo cliente neste status?
    /// </summary>
    public static bool PermiteRetirada(this StatusOrdemServico status) =>
        status is StatusOrdemServico.Pronto or StatusOrdemServico.Entregue;

    public static StatusOrdemServico[] Todos() =>
    [
        StatusOrdemServico.Recebido,
        StatusOrdemServico.EmAnalise,
        StatusOrdemServico.AguardandoAprovacao,
        StatusOrdemServico.AguardandoPeca,
        StatusOrdemServico.EmReparo,
        StatusOrdemServico.Pronto,
        StatusOrdemServico.Entregue,
        StatusOrdemServico.Cancelado
    ];

    /// <summary>
    /// Status a partir do número do formulário. Retorna null se inválido.
    /// </summary>
    public static StatusOrdemServico? Parse(string? valor) =>
        Enum.TryParse<StatusOrdemServico>(valor, ignoreCase: true, out var status) && Todos().Contains(status)
            ? status
            : null;
}
