namespace SistemaCrudAssistencia.Services;

/// <summary>
/// Erro de regra de negócio (CPF já cadastrado, aparelho de outro cliente, status inválido...).
/// Os controllers transformam estas mensagens em erro de formulário.
/// </summary>
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem)
    {
    }
}
