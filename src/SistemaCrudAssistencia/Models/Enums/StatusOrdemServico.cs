namespace SistemaCrudAssistencia.Models.Enums;

/// <summary>
/// Situação atual de uma Ordem de Serviço.
/// </summary>
public enum StatusOrdemServico
{
    Recebido = 1,
    EmAnalise = 2,
    AguardandoAprovacao = 3,
    AguardandoPeca = 4,
    EmReparo = 5,
    Pronto = 6,
    Entregue = 7,
    Cancelado = 8
}
