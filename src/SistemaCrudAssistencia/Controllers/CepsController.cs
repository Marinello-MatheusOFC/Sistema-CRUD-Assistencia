using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Models.Validations;
using SistemaCrudAssistencia.Services.Endereco;

namespace SistemaCrudAssistencia.Controllers;

/// <summary>
/// Endpoint mínimo de consulta de CEP para preencher o formulário de cliente.
/// Protegido pela FallbackPolicy (sem AllowAnonymous) e limitado ao ViaCEP:
/// devolve apenas logradouro, bairro, cidade e estado — nunca é um proxy
/// genérico para URLs externas. Falha do ViaCEP vira "encontrado=false",
/// nunca um erro 500: o cadastro manual continua disponível.
/// </summary>
public class CepsController(ICepService cepService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Consultar(string? cep, CancellationToken cancellationToken)
    {
        var normalizado = Cep.Normalizar(cep);

        if (!Cep.EhValido(normalizado))
        {
            return Json(new
            {
                encontrado = false,
                mensagem = "CEP inválido. Confira os 8 dígitos."
            });
        }

        var resultado = await cepService.BuscarAsync(normalizado, cancellationToken);

        if (resultado is null)
        {
            // A interface devolve null tanto para "CEP inexistente" quanto para
            // falha de rede/timeout — a mensagem cobre os dois casos.
            return Json(new
            {
                encontrado = false,
                mensagem = "Não foi possível consultar o CEP. Confira o número ou preencha o endereço manualmente."
            });
        }

        return Json(new
        {
            encontrado = true,
            logradouro = resultado.Logradouro,
            bairro = resultado.Bairro,
            cidade = resultado.Cidade,
            estado = resultado.Estado
        });
    }
}
