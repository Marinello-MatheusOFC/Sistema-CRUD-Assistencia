using System.ComponentModel.DataAnnotations;
using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Models.ViewModels;

public class AparelhoFormViewModel
{
    [Display(Name = "Tipo do aparelho")]
    [Required(ErrorMessage = "Informe o tipo do aparelho.")]
    [StringLength(50)]
    public string TipoAparelho { get; set; } = string.Empty;

    [Display(Name = "Marca")]
    [Required(ErrorMessage = "Informe a marca.")]
    [StringLength(60)]
    public string Marca { get; set; } = string.Empty;

    [Display(Name = "Modelo")]
    [Required(ErrorMessage = "Informe o modelo.")]
    [StringLength(80)]
    public string Modelo { get; set; } = string.Empty;

    [Display(Name = "IMEI ou número de série")]
    [StringLength(40)]
    public string? ImeiNumeroSerie { get; set; }

    [Display(Name = "Cor")]
    [StringLength(40)]
    public string? Cor { get; set; }

    [Display(Name = "Observações")]
    [StringLength(500)]
    public string? Observacoes { get; set; }

    public Aparelho ParaAparelho(int clienteId, int id = 0) => new()
    {
        Id = id,
        ClienteId = clienteId,
        TipoAparelho = TipoAparelho,
        Marca = Marca,
        Modelo = Modelo,
        ImeiNumeroSerie = ImeiNumeroSerie,
        Cor = Cor,
        Observacoes = Observacoes
    };

    public static AparelhoFormViewModel DeAparelho(Aparelho aparelho) => new()
    {
        TipoAparelho = aparelho.TipoAparelho,
        Marca = aparelho.Marca,
        Modelo = aparelho.Modelo,
        ImeiNumeroSerie = aparelho.ImeiNumeroSerie,
        Cor = aparelho.Cor,
        Observacoes = aparelho.Observacoes
    };
}
