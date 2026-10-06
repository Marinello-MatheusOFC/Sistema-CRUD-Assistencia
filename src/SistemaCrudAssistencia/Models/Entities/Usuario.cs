using Microsoft.AspNetCore.Identity;

namespace SistemaCrudAssistencia.Models.Entities;

/// <summary>
/// Usuário do sistema (administrador ou técnico).
/// A senha nunca é salva em texto puro: o ASP.NET Core Identity aplica PBKDF2 com salt.
/// </summary>
public class Usuario : IdentityUser
{
    public DateTime DataCadastro { get; set; }
}
