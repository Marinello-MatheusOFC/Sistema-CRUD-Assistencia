using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SistemaCrudAssistencia.Data;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Enums;

namespace SistemaCrudAssistencia.Services;

/// <summary>
/// Cria o usuário administrativo inicial e, apenas em desenvolvimento,
/// alguns dados de exemplo para navegar pelo sistema.
/// </summary>
public static class SeedExecutor
{
    public static async Task ExecutarAsync(IServiceProvider services, IConfiguration configuration, string ambiente)
    {
        await CriarUsuarioAdministradorAsync(services, configuration);
        await CriarDadosDeExemploAsync(services, ambiente);
    }

    private static async Task CriarUsuarioAdministradorAsync(IServiceProvider services, IConfiguration configuration)
    {
        var contexto = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");

        if (await contexto.Users.AnyAsync())
        {
            logger.LogInformation("Usuários já existem — criação do administrador ignorada.");
            return;
        }

        var email = configuration["ADMIN_EMAIL"]?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning(
                "Nenhum usuário existe e ADMIN_EMAIL não foi definido. Defina ADMIN_EMAIL e ADMIN_PASSWORD para criar o acesso inicial.");
            return;
        }

        var senha = configuration["ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(senha))
            throw new InvalidOperationException(
                "ADMIN_PASSWORD precisa ser definido junto com ADMIN_EMAIL para criar o usuário inicial.");

        var users = services.GetRequiredService<UserManager<Usuario>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roles.RoleExistsAsync("Admin"))
            await roles.CreateAsync(new IdentityRole("Admin"));

        var usuario = new Usuario
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DataCadastro = DateTime.Now
        };

        var resultado = await users.CreateAsync(usuario, senha);

        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(
                "Não foi possível criar o usuário administrativo: "
                + string.Join(" | ", resultado.Errors.Select(e => e.Description)));
        }

        await users.AddToRoleAsync(usuario, "Admin");
        logger.LogWarning("Usuário administrativo inicial criado para o e-mail informado.");
    }

    private static async Task CriarDadosDeExemploAsync(IServiceProvider services, string ambiente)
    {
        if (!string.Equals(ambiente, "Development", StringComparison.OrdinalIgnoreCase))
            return;

        var contexto = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");

        if (await contexto.OrdensServico.AnyAsync())
            return;

        // CPFs válidos apenas para satisfazer o dígito verificador — pessoas fictícias.
        var clientes = new List<Cliente>
        {
            new()
            {
                NomeCompleto = "Maria Aparecida Souza",
                Cpf = "12345678909",
                Telefone = "11987654321",
                Email = "maria.souza@exemplo.com.br",
                Cep = "01001000",
                Logradouro = "Praça da Sé",
                Numero = "150",
                Bairro = "Sé",
                Cidade = "São Paulo",
                Estado = "SP"
            },
            new()
            {
                NomeCompleto = "João Carlos Ferreira",
                Cpf = "11144477735",
                Telefone = "1133334444",
                Cep = "20040030",
                Logradouro = "Avenida Rio Branco",
                Numero = "1250",
                Complemento = "Sala 32",
                Bairro = "Centro",
                Cidade = "Rio de Janeiro",
                Estado = "RJ"
            },
            new()
            {
                NomeCompleto = "Ana Beatriz Lima",
                Cpf = "52998224725",
                Telefone = "11991234567",
                Cep = "30130010",
                Logradouro = "Avenida Afonso Pena",
                Numero = "800",
                Bairro = "Centro",
                Cidade = "Belo Horizonte",
                Estado = "MG"
            }
        };

        var agora = DateTime.Now;

        foreach (var cliente in clientes)
        {
            cliente.DataCadastro = agora.AddDays(-90);
            cliente.Ativo = true;
        }

        contexto.Clientes.AddRange(clientes);
        await contexto.SaveChangesAsync();

        var aparelhoMaria = new Aparelho
        {
            ClienteId = clientes[0].Id,
            TipoAparelho = "Celular",
            Marca = "Samsung",
            Modelo = "Galaxy S21",
            ImeiNumeroSerie = "352099001761481",
            Cor = "Preto",
            DataCadastro = agora.AddDays(-90)
        };

        var tabletMaria = new Aparelho
        {
            ClienteId = clientes[0].Id,
            TipoAparelho = "Tablet",
            Marca = "Apple",
            Modelo = "iPad 9ª geração",
            Cor = "Cinza",
            DataCadastro = agora.AddDays(-30)
        };

        var aparelhoJoao = new Aparelho
        {
            ClienteId = clientes[1].Id,
            TipoAparelho = "Notebook",
            Marca = "Lenovo",
            Modelo = "IdeaPad 330",
            ImeiNumeroSerie = "PF3XK9AB",
            Cor = "Cinza",
            DataCadastro = agora.AddDays(-60)
        };

        var aparelhoAna = new Aparelho
        {
            ClienteId = clientes[2].Id,
            TipoAparelho = "Smart TV",
            Marca = "LG",
            Modelo = "55UN7000",
            Cor = "Preto",
            DataCadastro = agora.AddDays(-10)
        };

        contexto.Aparelhos.AddRange(aparelhoMaria, tabletMaria, aparelhoJoao, aparelhoAna);
        await contexto.SaveChangesAsync();

        var ordens = new List<(OrdemServico Ordem, List<(StatusOrdemServico? Anterior, StatusOrdemServico Novo, string Observacao, int MinutosAtras)> Passos)>
        {
            (CriarOrdem(clientes[0], aparelhoMaria, "Tela quebrada após queda. Touch não responde no canto superior.",
                "Tela com fissura no canto superior e vidro solto", "Aparelho, capa e carregador", agora.AddDays(-2)),
            [
                (null, StatusOrdemServico.Recebido, "Entrada do aparelho na assistência.", 2880),
                (StatusOrdemServico.Recebido, StatusOrdemServico.EmAnalise, "Iniciando a verificação do display.", 2400),
                (StatusOrdemServico.EmAnalise, StatusOrdemServico.AguardandoAprovacao, "Tela precisa ser trocada. Orçamento enviado ao cliente.", 1200),
                (StatusOrdemServico.AguardandoAprovacao, StatusOrdemServico.EmReparo, "Cliente aprovou o orçamento.", 300)
            ]),

            (CriarOrdem(clientes[1], aparelhoJoao, "Notebook não liga, ventoinha gira mas a tela permanece preta.",
                "Riscos na tampa e marca de queda no canto", "Notebook e carregador original", agora.AddDays(-1)),
            [
                (null, StatusOrdemServico.Recebido, "Entrada do aparelho na assistência.", 1440),
                (StatusOrdemServico.Recebido, StatusOrdemServico.EmAnalise, "Suspeita de problema na placa-mãe.", 900),
                (StatusOrdemServico.EmAnalise, StatusOrdemServico.AguardandoPeca, "Encomendado ribbon de vídeo.", 600)
            ]),

            (CriarOrdem(clientes[0], tabletMaria, "Aparelho não carrega. Conector danificado.",
                "Sem avarias visíveis", "Tablet apenas", agora.AddDays(-6)),
            [
                (null, StatusOrdemServico.Recebido, "Entrada do aparelho na assistência.", 8640),
                (StatusOrdemServico.Recebido, StatusOrdemServico.EmReparo, "Conector de carga substituído.", 4000),
                (StatusOrdemServico.EmReparo, StatusOrdemServico.Pronto, "Aparelho testado e pronto para retirada.", 3000),
                (StatusOrdemServico.Pronto, StatusOrdemServico.Entregue, "Cliente retirou o aparelho.", 2000)
            ]),

            (CriarOrdem(clientes[2], aparelhoAna, "Sem som no dispositivo de HDMI.",
                "Sem avarias visíveis", "Controle remoto e cabo HDMI", agora.AddDays(-5)),
            [
                (null, StatusOrdemServico.Recebido, "Entrada do aparelho na assistência.", 7200),
                (StatusOrdemServico.Recebido, StatusOrdemServico.AguardandoAprovacao, "Atualização de firmware resolve. Orçamento enviado.", 3600)
            ])
        };

        foreach (var (ordem, passos) in ordens)
        {
            ordem.Status = passos[^1].Novo;

            if (ordem.Status is StatusOrdemServico.Pronto or StatusOrdemServico.Entregue)
                ordem.DataConclusao = ordem.DataEntrada.AddHours(1);

            if (ordem.Status == StatusOrdemServico.Entregue)
                ordem.DataRetirada = ordem.DataEntrada.AddHours(3);

            ordem.AtualizarValorTotal();
            contexto.OrdensServico.Add(ordem);
            await contexto.SaveChangesAsync();

            ordem.GerarNumeroOS();

            foreach (var (anterior, novo, observacao, minutosAtras) in passos)
            {
                contexto.Historicos.Add(new HistoricoOrdemServico
                {
                    OrdemServicoId = ordem.Id,
                    Data = ordem.DataEntrada.AddMinutes(-minutosAtras).Date.AddHours(9),
                    StatusAnterior = anterior,
                    NovoStatus = novo,
                    Observacao = observacao,
                    Usuario = "seed@exemplo.local"
                });
            }

            await contexto.SaveChangesAsync();
        }

        logger.LogWarning("Dados de exemplo de desenvolvimento criados.");
    }

    private static OrdemServico CriarOrdem(Cliente cliente, Aparelho aparelho, string defeito, string estadoFisico, string acessorios, DateTime entrada) =>
        new()
        {
            ClienteId = cliente.Id,
            AparelhoId = aparelho.Id,
            DataEntrada = entrada,
            DefeitoRelatado = defeito,
            EstadoFisicoEntrada = estadoFisico,
            AcessoriosEntregues = acessorios,
            DiagnosticoTecnico = "Diagnóstico inicial registrado pelo técnico.",
            ValorPecas = 0m,
            ValorMaoDeObra = 0m
        };
}
