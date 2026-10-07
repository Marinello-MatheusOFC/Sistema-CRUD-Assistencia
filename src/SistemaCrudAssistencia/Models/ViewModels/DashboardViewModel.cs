using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Models.ViewModels;

public class DashboardViewModel
{
    public DashboardViewModel(ResumoDashboard resumo)
    {
        Abertas = resumo.Abertas;
        EmAnalise = resumo.EmAnalise;
        AguardandoPeca = resumo.AguardandoPeca;
        EmReparo = resumo.EmReparo;
        ProntasParaRetirada = resumo.ProntasParaRetirada;
        UltimasOrdens = resumo.UltimasOrdens;
    }

    public int Abertas { get; }
    public int EmAnalise { get; }
    public int AguardandoPeca { get; }
    public int EmReparo { get; }
    public int ProntasParaRetirada { get; }
    public IReadOnlyList<OrdemServico> UltimasOrdens { get; }
}
