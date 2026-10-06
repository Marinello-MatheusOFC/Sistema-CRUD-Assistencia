namespace SistemaCrudAssistencia.Models.Entities;

/// <summary>
/// Aparelho de um cliente. Um cliente pode ter vários aparelhos.
/// </summary>
public class Aparelho
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    /// <summary>Ex.: Celular, Tablet, Notebook, Console, Smart TV.</summary>
    public string TipoAparelho { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    /// <summary>IMEI (celular) ou número de série. Opcional: nem todo aparelho tem.</summary>
    public string? ImeiNumeroSerie { get; set; }

    public string? Cor { get; set; }

    public string? Observacoes { get; set; }

    public DateTime DataCadastro { get; set; }

    public ICollection<OrdemServico> OrdensServico { get; set; } = new List<OrdemServico>();

    /// <summary>Texto usado nas listas: "Samsung Galaxy S21 - (IMEI 123456789012345)".</summary>
    public string Descricao =>
        string.IsNullOrWhiteSpace(Modelo)
            ? Marca
            : $"{Marca} {Modelo}".Trim();
}
