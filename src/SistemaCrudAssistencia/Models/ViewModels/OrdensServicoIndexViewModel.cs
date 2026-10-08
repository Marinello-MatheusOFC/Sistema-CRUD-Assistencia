using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Filtros;

namespace SistemaCrudAssistencia.Models.ViewModels;

public class OrdensServicoIndexViewModel
{
    public FiltroOrdensServico Filtro { get; set; } = new();

    public List<OrdemServico> Ordens { get; set; } = [];
}
