using System.ComponentModel.DataAnnotations;
using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Models.ViewModels;

/// <summary>
/// Estado do wizard de abertura de Nova OS.
/// O CPF nunca vai para a URL: o fluxo inteiro avança por POST e o cliente é
/// identificado no servidor apenas pelo Id. NumeroOS, ValorTotal, DataEntrada
/// e status NÃO existem aqui — todos são definidos pelo OrdemServicoService.
/// </summary>
public class NovaOrdemServicoViewModel
{
    /// <summary>Etapa 1: CPF digitado no balcão (somente dígitos ou formatado).</summary>
    public string? Cpf { get; set; }

    /// <summary>
    /// Id enviado pelo navegador na confirmação. NÃO é fonte confiável:
    /// o servidor recarrega o cliente por este Id e revalida tudo antes de abrir a OS.
    /// </summary>
    public int ClienteId { get; set; }

    /// <summary>Cliente localizado/cadastrado. Presente a partir da etapa 2.</summary>
    public Cliente? Cliente { get; set; }

    /// <summary>Apenas os aparelhos DO CLIENTE CARREGADO no servidor.</summary>
    public List<Aparelho> Aparelhos { get; set; } = [];

    /// <summary>Formulário de cadastro inline, usado somente quando o CPF não existe.</summary>
    public ClienteFormViewModel? CadastroCliente { get; set; }

    /// <summary>"existente" usa <see cref="AparelhoId"/>; "novo" cadastra dentro do fluxo.</summary>
    public string ModoAparelho { get; set; } = "existente";

    public int? AparelhoId { get; set; }

    public AparelhoFormViewModel NovoAparelho { get; set; } = new();

    [StringLength(1000)]
    public string? DefeitoRelatado { get; set; }

    [StringLength(500)]
    public string? EstadoFisicoEntrada { get; set; }

    [StringLength(500)]
    public string? AcessoriosEntregues { get; set; }

    [StringLength(1000)]
    public string? ObservacoesEntrada { get; set; }

    public string? Mensagem { get; set; }

    public string? Erro { get; set; }

    public bool CadastrarAparelhoNovo =>
        string.Equals(ModoAparelho, "novo", StringComparison.OrdinalIgnoreCase);
}
