using System.ComponentModel.DataAnnotations;

namespace SistemaCrudAssistencia.Models.ViewModels;

/// <summary>
/// Dados de autenticação. A senha nunca é devolvida ao cliente
/// e o processamento é feito exclusivamente pelo ASP.NET Core Identity.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail/usuário.")]
    [Display(Name = "E-mail ou usuário")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Display(Name = "Manter conectado")]
    public bool ManterConectado { get; set; }

    public string? ReturnUrl { get; set; }
}
