namespace SistemaCrudAssistencia.Models.ViewModels;

/// <summary>
/// Mensagem em pt-BR para páginas de erro de status HTTP (4xx/5xx).
/// </summary>
public class StatusHttpViewModel
{
    public int Codigo { get; init; }

    public string Titulo { get; init; } = string.Empty;

    public string Mensagem { get; init; } = string.Empty;

    public static StatusHttpViewModel Criar(int? codigo)
    {
        var status = codigo is >= 400 and <= 599 ? codigo.Value : 404;

        var (titulo, mensagem) = status switch
        {
            400 => ("Requisição inválida", "A solicitação enviada não é válida."),
            401 => ("Não autenticado", "É preciso entrar no sistema para acessar esta página."),
            403 => ("Acesso negado", "Você não tem permissão para acessar este recurso."),
            404 => ("Página não encontrada", "O endereço acessado não existe ou foi movido."),
            405 => ("Método não permitido", "Esta operação não é permitida para o endereço informado."),
            408 => ("Tempo esgotado", "A solicitação demorou demais para ser processada."),
            429 => ("Muitas requisições", "Muitas solicitações em pouco tempo. Aguarde alguns instantes e tente novamente."),
            500 => ("Erro interno", "Ocorreu um erro inesperado durante o processamento. Tente novamente."),
            502 => ("Serviço indisponível", "O serviço solicitado retornou uma resposta inválida."),
            503 => ("Serviço indisponível", "O sistema está temporariamente indisponível. Tente novamente em instantes."),
            504 => ("Tempo esgotado", "O serviço solicitado demorou demais para responder."),
            _ => ("Erro", "Ocorreu um erro ao processar a solicitação.")
        };

        return new StatusHttpViewModel { Codigo = status, Titulo = titulo, Mensagem = mensagem };
    }
}
