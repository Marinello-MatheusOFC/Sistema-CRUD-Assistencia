using System.Globalization;
using SistemaCrudAssistencia.Models.Enums;
using SistemaCrudAssistencia.Models.Extensions;

namespace SistemaCrudAssistencia.Models.Entities;

/// <summary>
/// Ordem de Serviço: o documento central do sistema.
/// Nunca é excluída — o encerramento é feito pelo <see cref="Status"/>.
/// </summary>
public class OrdemServico
{
    public int Id { get; set; }

    /// <summary>
    /// Identificador legível no formato 2026-000042 (ano + sequencial), com índice único.
    /// Nulo apenas durante a criação: o número depende do Id, gerado pelo banco no INSERT.
    /// </summary>
    public string? NumeroOS { get; set; }

    public int ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    public int AparelhoId { get; set; }

    public Aparelho? Aparelho { get; set; }

    // ---------- Entrada do aparelho ----------

    public DateTime DataEntrada { get; set; }

    public string DefeitoRelatado { get; set; } = string.Empty;

    public string? EstadoFisicoEntrada { get; set; }

    public string? AcessoriosEntregues { get; set; }

    public string? ObservacoesEntrada { get; set; }

    // ---------- Service / orçamento ----------

    public string? DiagnosticoTecnico { get; set; }

    public string? ServicoRealizado { get; set; }

    public string? PecasUtilizadas { get; set; }

    public decimal ValorPecas { get; set; }

    public decimal ValorMaoDeObra { get; set; }

    /// <summary>Sempre calculado pelo servidor a partir de <see cref="ValorPecas"/> + <see cref="ValorMaoDeObra"/>.</summary>
    public decimal ValorTotal { get; set; }

    // ---------- Situação ----------

    public StatusOrdemServico Status { get; set; } = StatusOrdemServico.Recebido;

    public DateTime? DataConclusao { get; set; }

    public DateTime? DataRetirada { get; set; }

    public string? ObservacoesInternas { get; set; }

    public ICollection<HistoricoOrdemServico> Historico { get; set; } = new List<HistoricoOrdemServico>();

    /// <summary>O aparelho ainda está na assistência?</summary>
    public bool EstaEmAndamento => Status.EstaEmAndamento();

    /// <summary>Soma de peças e mão de obra, arredondada para 2 casas.</summary>
    public decimal CalcularValorTotal() =>
        Math.Round(ValorPecas + ValorMaoDeObra, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Recalcula e grava o total. Chamado sempre pelo service layer, nunca pelo formulário.
    /// </summary>
    public void AtualizarValorTotal() => ValorTotal = CalcularValorTotal();

    /// <summary>
    /// Monta o número da OS a partir do Id gerado pelo banco.
    /// Ex.: Id 42 em 2026 =&gt; "2026-000042". Determinístico e sem risco de duplicidade.
    /// </summary>
    public void GerarNumeroOS() =>
        NumeroOS = $"{DataEntrada.Year}-{Id.ToString("D6", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Texto curto do aparelho, usado em listas e no topo da OS.
    /// </summary>
    public string DescricaoAparelho =>
        Aparelho is null
            ? "—"
            : string.IsNullOrWhiteSpace(Aparelho.Modelo)
                ? Aparelho.Marca
                : $"{Aparelho.Marca} {Aparelho.Modelo}".Trim();
}
