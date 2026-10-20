using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Models.ViewModels;

public class AparelhosIndexViewModel
{
    public string? Busca { get; init; }

    public IReadOnlyList<Aparelho> Aparelhos { get; init; } = [];
}
