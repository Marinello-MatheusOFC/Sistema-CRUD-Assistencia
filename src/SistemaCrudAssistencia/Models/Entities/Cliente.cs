namespace SistemaCrudAssistencia.Models.Entities;

/// <summary>
/// Cliente da assistência técnica.
/// O CPF é armazenado apenas com dígitos (ver <see cref="Validations.Cpf"/>) e é único.
/// </summary>
public class Cliente
{
    public int Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>Somente dígitos, 11 caracteres.</summary>
    public string Cpf { get; set; } = string.Empty;

    /// <summary>Somente dígitos (10 ou 11 caracteres).</summary>
    public string Telefone { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>Somente dígitos, 8 caracteres.</summary>
    public string Cep { get; set; } = string.Empty;

    public string Logradouro { get; set; } = string.Empty;

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string Bairro { get; set; } = string.Empty;

    public string Cidade { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public DateTime DataCadastro { get; set; }

    /// <summary>Clientes com histórico não são excluídos, apenas desativados.</summary>
    public bool Ativo { get; set; } = true;

    public ICollection<Aparelho> Aparelhos { get; set; } = new List<Aparelho>();

    public ICollection<OrdemServico> OrdensServico { get; set; } = new List<OrdemServico>();
}
