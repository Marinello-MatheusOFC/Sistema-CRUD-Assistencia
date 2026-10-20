using System.ComponentModel.DataAnnotations;
using SistemaCrudAssistencia.Models.ViewModels;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Os limites da ViewModel precisam casar com as colunas do banco
/// (ordens_servico: peças utilizadas e observações internas são varchar(1000)):
/// texto no limite passa; limite + 1 é recusado antes de qualquer persistência.
/// </summary>
public class OrdemServicoFormViewModelLimiteTests
{
    private static List<ValidationResult> Validar(OrdemServicoFormViewModel modelo)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, validateAllProperties: true);
        return resultados;
    }

    private static OrdemServicoFormViewModel ModeloCom(string? pecas, string? observacoesInternas) => new()
    {
        DefeitoRelatado = "Não carrega.",
        ValorPecas = "0,00",
        ValorMaoDeObra = "0,00",
        PecasUtilizadas = pecas,
        ObservacoesInternas = observacoesInternas
    };

    [Fact]
    public void PecasUtilizadas_NoTamanhoMaximoDoBanco_EhAceita()
    {
        var resultados = Validar(ModeloCom(new string('A', 1000), null));

        Assert.DoesNotContain(resultados, r => r.MemberNames.Contains(nameof(OrdemServicoFormViewModel.PecasUtilizadas)));
    }

    [Fact]
    public void PecasUtilizadas_AcimaDoTamanhoMaximoDoBanco_EhRejeitada()
    {
        var resultados = Validar(ModeloCom(new string('A', 1001), null));

        var erro = Assert.Single(resultados, r => r.MemberNames.Contains(nameof(OrdemServicoFormViewModel.PecasUtilizadas)));
        Assert.Equal("Peças utilizadas pode ter no máximo 1000 caracteres.", erro.ErrorMessage);
    }

    [Fact]
    public void ObservacoesInternas_NoTamanhoMaximoDoBanco_EhAceita()
    {
        var resultados = Validar(ModeloCom(null, new string('B', 1000)));

        Assert.DoesNotContain(resultados, r => r.MemberNames.Contains(nameof(OrdemServicoFormViewModel.ObservacoesInternas)));
    }

    [Fact]
    public void ObservacoesInternas_AcimaDoTamanhoMaximoDoBanco_EhRejeitada()
    {
        var resultados = Validar(ModeloCom(null, new string('B', 1001)));

        var erro = Assert.Single(resultados, r => r.MemberNames.Contains(nameof(OrdemServicoFormViewModel.ObservacoesInternas)));
        Assert.Equal("Observações internas pode ter no máximo 1000 caracteres.", erro.ErrorMessage);
    }
}
