using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Controllers;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Controles de segurança (Etapa 10): nenhum AllowAnonymous fora do
/// AccountController, operações mutadoras somente POST e proteções globais
/// do Program.cs intactas.
/// </summary>
public class SegurancaTests
{
    [Theory]
    [InlineData(typeof(HomeController))]
    [InlineData(typeof(ClientesController))]
    [InlineData(typeof(AparelhosController))]
    [InlineData(typeof(OrdensServicoController))]
    [InlineData(typeof(BuscaController))]
    [InlineData(typeof(CepsController))]
    public void ControladoresDeDominio_NaoPermitemAcessoAnonimo(Type controller)
    {
        Assert.False(
            controller.GetCustomAttributes(inherit: true).OfType<AllowAnonymousAttribute>().Any(),
            $"{controller.Name} está marcado com AllowAnonymous.");

        var metodosAnonimos = controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes(inherit: true).OfType<AllowAnonymousAttribute>().Any())
            .Select(m => m.Name)
            .ToList();

        Assert.Empty(metodosAnonimos);
    }

    [Theory]
    [InlineData(typeof(ClientesController), "Create")]
    [InlineData(typeof(ClientesController), "Edit")]
    [InlineData(typeof(ClientesController), "Desativar")]
    [InlineData(typeof(ClientesController), "Reativar")]
    [InlineData(typeof(ClientesController), "Pesquisar")]
    [InlineData(typeof(AparelhosController), "Create")]
    [InlineData(typeof(AparelhosController), "Edit")]
    [InlineData(typeof(OrdensServicoController), "LocalizarCliente")]
    [InlineData(typeof(OrdensServicoController), "CadastrarCliente")]
    [InlineData(typeof(OrdensServicoController), "Confirmar")]
    [InlineData(typeof(OrdensServicoController), "Edit")]
    [InlineData(typeof(OrdensServicoController), "AlterarStatus")]
    [InlineData(typeof(OrdensServicoController), "Finalizar")]
    [InlineData(typeof(OrdensServicoController), "RegistrarRetirada")]
    [InlineData(typeof(OrdensServicoController), "Cancelar")]
    [InlineData(typeof(BuscaController), "Pesquisar")]
    public void OperacaoQueAlteraOuBuscaPorCpf_TemAcaoPost(Type controller, string acao)
    {
        var metodos = controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == acao)
            .ToList();

        Assert.NotEmpty(metodos);
        Assert.Contains(metodos, m =>
            m.GetCustomAttributes(inherit: true).OfType<HttpPostAttribute>().Any());
    }

    [Theory]
    [InlineData(typeof(ClientesController), "Desativar")]
    [InlineData(typeof(ClientesController), "Reativar")]
    [InlineData(typeof(ClientesController), "Pesquisar")]
    [InlineData(typeof(OrdensServicoController), "LocalizarCliente")]
    [InlineData(typeof(OrdensServicoController), "CadastrarCliente")]
    [InlineData(typeof(OrdensServicoController), "Confirmar")]
    [InlineData(typeof(OrdensServicoController), "AlterarStatus")]
    [InlineData(typeof(OrdensServicoController), "Finalizar")]
    [InlineData(typeof(OrdensServicoController), "RegistrarRetirada")]
    [InlineData(typeof(OrdensServicoController), "Cancelar")]
    [InlineData(typeof(BuscaController), "Pesquisar")]
    public void AcaoSensivel_NaoPossuiVersaoGet(Type controller, string acao)
    {
        var metodos = controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == acao)
            .ToList();

        Assert.NotEmpty(metodos);
        Assert.DoesNotContain(metodos, m =>
            m.GetCustomAttributes(inherit: true).OfType<HttpGetAttribute>().Any());
    }

    [Fact]
    public void ProgramaMantemPoliticaDeFallbackEAntiforgeryGlobais()
    {
        // A aplicação exige connection string para subir, então não há host de
        // teste simples: as proteções são conferidas no arquivo de composição.
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 5 && diretorio is not null; i++)
            diretorio = diretorio.Parent;

        var caminho = Path.Combine(
            diretorio!.FullName, "src", "SistemaCrudAssistencia", "Program.cs");

        Assert.True(File.Exists(caminho), $"Arquivo não encontrado: {caminho}");
        var conteudo = File.ReadAllText(caminho);

        Assert.Contains("FallbackPolicy", conteudo);
        Assert.Contains("RequireAuthenticatedUser", conteudo);
        Assert.Contains("AutoValidateAntiforgeryTokenAttribute", conteudo);
    }
}
