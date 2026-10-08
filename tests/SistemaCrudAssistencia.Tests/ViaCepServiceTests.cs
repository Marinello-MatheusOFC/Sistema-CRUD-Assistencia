using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaCrudAssistencia.Services.Endereco;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// ViaCEP determinístico (Etapa 10): HttpMessageHandler falso no lugar da rede —
/// nenhum teste acessa a internet real.
/// </summary>
public class ViaCepServiceTests
{
    private sealed class ManipuladorFalso : HttpMessageHandler
    {
        public int Chamadas { get; private set; }
        public Uri? UltimoEndereco { get; private set; }
        public string CorpoJson { get; set; } = "{}";
        public bool DerrubarRede { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Chamadas++;
            UltimoEndereco = request.RequestUri;

            if (DerrubarRede)
                throw new HttpRequestException("sem conexão com a rede");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CorpoJson, Encoding.UTF8, "application/json")
            });
        }
    }

    private static ViaCepService NovoServico(ManipuladorFalso manipulador) =>
        new(new HttpClient(manipulador), NullLogger<ViaCepService>.Instance);

    [Fact]
    public async Task BuscarAsync_CepComTamanhoInvalido_NaoEnviaRequisicao()
    {
        var manipulador = new ManipuladorFalso();
        var servico = NovoServico(manipulador);

        var resultado = await servico.BuscarAsync("12345-67");

        Assert.Null(resultado);
        Assert.Equal(0, manipulador.Chamadas);
    }

    [Fact]
    public async Task BuscarAsync_RespostaValida_MapeiaEnderecoEUsaCepNormalizadoNaUri()
    {
        var manipulador = new ManipuladorFalso
        {
            CorpoJson = """
                {"cep":"01001-000","logradouro":"Praça da Sé","complemento":"lado ímpar",
                 "bairro":"Sé","localidade":"São Paulo","uf":"SP"}
                """
        };
        var servico = NovoServico(manipulador);

        var resultado = await servico.BuscarAsync("01001-000");

        Assert.NotNull(resultado);
        Assert.Equal("01001000", resultado.Cep);
        Assert.Equal("Praça da Sé", resultado.Logradouro);
        Assert.Equal("lado ímpar", resultado.Complemento);
        Assert.Equal("Sé", resultado.Bairro);
        Assert.Equal("São Paulo", resultado.Cidade);
        Assert.Equal("SP", resultado.Estado);
        Assert.Equal(1, manipulador.Chamadas);
        Assert.Contains("/01001000/", manipulador.UltimoEndereco!.ToString());
    }

    [Fact]
    public async Task BuscarAsync_CepInexistente_DevolveNullSemExcecao()
    {
        var manipulador = new ManipuladorFalso { CorpoJson = """{"erro":true}""" };
        var servico = NovoServico(manipulador);

        var resultado = await servico.BuscarAsync("99999999");

        Assert.Null(resultado);
        Assert.Equal(1, manipulador.Chamadas);
    }

    [Fact]
    public async Task BuscarAsync_FalhaDeRede_NaoImpedeOCadastro()
    {
        var manipulador = new ManipuladorFalso { DerrubarRede = true };
        var servico = NovoServico(manipulador);

        var resultado = await servico.BuscarAsync("01001-000");

        Assert.Null(resultado);
        Assert.Equal(1, manipulador.Chamadas);
    }
}
