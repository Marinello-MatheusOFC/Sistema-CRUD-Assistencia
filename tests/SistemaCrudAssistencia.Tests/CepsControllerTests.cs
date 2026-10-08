using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Controllers;
using SistemaCrudAssistencia.Services.Endereco;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Endpoint mínimo de CEP da Etapa 9: valida com a normalização existente,
/// devolve só o que o formulário precisa e transforma falha externa em
/// mensagem — nunca em exceção/500.
/// </summary>
public class CepsControllerTests
{
    private sealed class CepServiceFalso : ICepService
    {
        public int Chamadas { get; private set; }

        public CepResultado? Retorno { get; set; }

        public Task<CepResultado?> BuscarAsync(string? cep, CancellationToken cancellationToken = default)
        {
            Chamadas++;
            return Task.FromResult(Retorno);
        }
    }

    private static JsonDocument Serializar(object? valor)
        => JsonDocument.Parse(JsonSerializer.Serialize(valor));

    [Fact]
    public async Task Consultar_CepInvalido_NaoConsultaOServicoExterno()
    {
        var externo = new CepServiceFalso();
        var controller = new CepsController(externo);

        var resultado = await controller.Consultar("123", CancellationToken.None);

        var json = Assert.IsType<JsonResult>(resultado);
        using var documento = Serializar(json.Value);
        Assert.False(documento.RootElement.GetProperty("encontrado").GetBoolean());
        Assert.True(documento.RootElement.TryGetProperty("mensagem", out _));
        Assert.Equal(0, externo.Chamadas);
    }

    [Fact]
    public async Task Consultar_CepValido_DevolveApenasOsCamposDoFormulario()
    {
        var externo = new CepServiceFalso
        {
            Retorno = new CepResultado(
                "01001000", "Praça da Sé", "150", "Loja 2", "Sé", "São Paulo", "SP")
        };
        var controller = new CepsController(externo);

        var resultado = await controller.Consultar("01001-000", CancellationToken.None);

        var json = Assert.IsType<JsonResult>(resultado);
        using var documento = Serializar(json.Value);
        var raiz = documento.RootElement;

        Assert.True(raiz.GetProperty("encontrado").GetBoolean());
        Assert.Equal("Praça da Sé", raiz.GetProperty("logradouro").GetString());
        Assert.Equal("Sé", raiz.GetProperty("bairro").GetString());
        Assert.Equal("São Paulo", raiz.GetProperty("cidade").GetString());
        Assert.Equal("SP", raiz.GetProperty("estado").GetString());

        // Nunca vaza campos que não são do formulário (número, complemento...).
        var propriedades = raiz.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToList();
        Assert.Equal(
            ["bairro", "cidade", "encontrado", "estado", "logradouro"],
            propriedades);
        Assert.Equal(1, externo.Chamadas);
    }

    [Fact]
    public async Task Consultar_FalhaOuCepInexistente_DevolveMensagemSemExcecao()
    {
        var externo = new CepServiceFalso { Retorno = null };
        var controller = new CepsController(externo);

        var resultado = await controller.Consultar("99999999", CancellationToken.None);

        var json = Assert.IsType<JsonResult>(resultado);
        using var documento = Serializar(json.Value);
        var raiz = documento.RootElement;

        Assert.False(raiz.GetProperty("encontrado").GetBoolean());
        var mensagem = raiz.GetProperty("mensagem").GetString();
        Assert.False(string.IsNullOrWhiteSpace(mensagem));
        Assert.Equal(1, externo.Chamadas);
    }
}
