using System.ComponentModel.DataAnnotations;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Validations;

namespace SistemaCrudAssistencia.Models.ViewModels;

public class ClienteFormViewModel
{
    [Display(Name = "Nome completo")]
    [Required(ErrorMessage = "Informe o nome completo.")]
    [StringLength(150)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Display(Name = "CPF")]
    [Required(ErrorMessage = "Informe o CPF.")]
    [Cpf]
    public string Cpf { get; set; } = string.Empty;

    [Display(Name = "Telefone")]
    [Required(ErrorMessage = "Informe o telefone.")]
    [Telefone]
    public string Telefone { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(256)]
    public string? Email { get; set; }

    [Display(Name = "CEP")]
    [Required(ErrorMessage = "Informe o CEP.")]
    [Cep]
    public string Cep { get; set; } = string.Empty;

    [Display(Name = "Logradouro")]
    [Required(ErrorMessage = "Informe o logradouro.")]
    [StringLength(150)]
    public string Logradouro { get; set; } = string.Empty;

    [Display(Name = "Número")]
    [StringLength(20)]
    public string? Numero { get; set; }

    [Display(Name = "Complemento")]
    [StringLength(100)]
    public string? Complemento { get; set; }

    [Display(Name = "Bairro")]
    [Required(ErrorMessage = "Informe o bairro.")]
    [StringLength(100)]
    public string Bairro { get; set; } = string.Empty;

    [Display(Name = "Cidade")]
    [Required(ErrorMessage = "Informe a cidade.")]
    [StringLength(100)]
    public string Cidade { get; set; } = string.Empty;

    [Display(Name = "Estado")]
    [Required(ErrorMessage = "Informe o estado.")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Informe a sigla do estado com 2 letras.")]
    public string Estado { get; set; } = string.Empty;

    public Cliente ParaCliente(int id = 0) => new()
    {
        Id = id,
        NomeCompleto = NomeCompleto,
        Cpf = Cpf,
        Telefone = Telefone,
        Email = Email,
        Cep = Cep,
        Logradouro = Logradouro,
        Numero = Numero,
        Complemento = Complemento,
        Bairro = Bairro,
        Cidade = Cidade,
        Estado = Estado
    };

    public static ClienteFormViewModel DeCliente(Cliente cliente) => new()
    {
        NomeCompleto = cliente.NomeCompleto,
        Cpf = SistemaCrudAssistencia.Models.Validations.Cpf.Formatar(cliente.Cpf),
        Telefone = SistemaCrudAssistencia.Models.Validations.Telefone.Formatar(cliente.Telefone),
        Email = cliente.Email,
        Cep = SistemaCrudAssistencia.Models.Validations.Cep.Formatar(cliente.Cep),
        Logradouro = cliente.Logradouro,
        Numero = cliente.Numero,
        Complemento = cliente.Complemento,
        Bairro = cliente.Bairro,
        Cidade = cliente.Cidade,
        Estado = cliente.Estado
    };
}
