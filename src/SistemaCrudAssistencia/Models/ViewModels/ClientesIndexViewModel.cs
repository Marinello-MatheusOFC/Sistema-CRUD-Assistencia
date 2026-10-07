using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Models.ViewModels;

public class ClientesIndexViewModel
{
    public string? Busca { get; init; }

    public IReadOnlyList<Cliente> Clientes { get; init; } = [];
}
