using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Validations;

namespace SistemaCrudAssistencia.Models.ViewModels;

/// <summary>
/// Edição dos campos de atendimento da OS (entrada, diagnóstico, serviço,
/// peças e valores). Não contém NumeroOS, ValorTotal, DataEntrada, ClienteId,
/// AparelhoId nem status: tudo isso é definido exclusivamente no servidor.
/// </summary>
public class OrdemServicoFormViewModel
{
    /// <summary>Usado apenas para montar a URL da ação; não é campo do formulário.</summary>
    public int Id { get; set; }

    [Display(Name = "Defeito relatado")]
    [Required(ErrorMessage = "Descreva o defeito relatado.")]
    [StringLength(1000)]
    public string DefeitoRelatado { get; set; } = string.Empty;

    [Display(Name = "Estado físico na entrada")]
    [StringLength(500)]
    public string? EstadoFisicoEntrada { get; set; }

    [Display(Name = "Acessórios entregues")]
    [StringLength(500)]
    public string? AcessoriosEntregues { get; set; }

    [Display(Name = "Observações de entrada")]
    [StringLength(1000)]
    public string? ObservacoesEntrada { get; set; }

    [Display(Name = "Diagnóstico técnico")]
    [StringLength(2000)]
    public string? DiagnosticoTecnico { get; set; }

    [Display(Name = "Serviço realizado")]
    [StringLength(2000)]
    public string? ServicoRealizado { get; set; }

    [Display(Name = "Peças utilizadas")]
    [StringLength(2000)]
    public string? PecasUtilizadas { get; set; }

    [Display(Name = "Observações internas")]
    [StringLength(2000)]
    public string? ObservacoesInternas { get; set; }

    /// <summary>Texto digitado pelo usuário; convertido SOMENTE no servidor.</summary>
    [Display(Name = "Valor de peças")]
    [Required(ErrorMessage = "Informe o valor das peças (pode ser 0).")]
    public string ValorPecas { get; set; } = "0,00";

    /// <summary>Texto digitado pelo usuário; convertido SOMENTE no servidor.</summary>
    [Display(Name = "Mão de obra")]
    [Required(ErrorMessage = "Informe o valor da mão de obra (pode ser 0).")]
    public string ValorMaoDeObra { get; set; } = "0,00";

    /// <summary>Monta a entidade com os valores já convertidos e validados no servidor.</summary>
    public OrdemServico ParaOrdemServico() => new()
    {
        DefeitoRelatado = DefeitoRelatado,
        EstadoFisicoEntrada = EstadoFisicoEntrada,
        AcessoriosEntregues = AcessoriosEntregues,
        ObservacoesEntrada = ObservacoesEntrada,
        DiagnosticoTecnico = DiagnosticoTecnico,
        ServicoRealizado = ServicoRealizado,
        PecasUtilizadas = PecasUtilizadas,
        ObservacoesInternas = ObservacoesInternas,
        ValorPecas = ValoresMonetarios.Converter(ValorPecas),
        ValorMaoDeObra = ValoresMonetarios.Converter(ValorMaoDeObra)
    };

    public static OrdemServicoFormViewModel DeOrdemServico(OrdemServico ordemServico) => new()
    {
        Id = ordemServico.Id,
        DefeitoRelatado = ordemServico.DefeitoRelatado,
        EstadoFisicoEntrada = ordemServico.EstadoFisicoEntrada,
        AcessoriosEntregues = ordemServico.AcessoriosEntregues,
        ObservacoesEntrada = ordemServico.ObservacoesEntrada,
        DiagnosticoTecnico = ordemServico.DiagnosticoTecnico,
        ServicoRealizado = ordemServico.ServicoRealizado,
        PecasUtilizadas = ordemServico.PecasUtilizadas,
        ObservacoesInternas = ordemServico.ObservacoesInternas,
        ValorPecas = ordemServico.ValorPecas.ToString("0.00", CultureInfo.GetCultureInfo("pt-BR")),
        ValorMaoDeObra = ordemServico.ValorMaoDeObra.ToString("0.00", CultureInfo.GetCultureInfo("pt-BR"))
    };
}
