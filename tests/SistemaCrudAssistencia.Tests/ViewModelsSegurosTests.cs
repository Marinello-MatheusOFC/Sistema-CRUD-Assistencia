using System.Reflection;
using SistemaCrudAssistencia.Models.ViewModels;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Sobrepostagem de parâmetros (Etapa 10): os ViewModels de formulário não
/// expõem campos calculados ou identificadores que somente o servidor define.
/// </summary>
public class ViewModelsSegurosTests
{
    private static string[] Propriedades(Type tipo) =>
        tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

    [Fact]
    public void ClienteFormViewModel_NaoExpoeIdStatusOuDataDeCadastro()
    {
        var nomes = Propriedades(typeof(ClienteFormViewModel));

        Assert.DoesNotContain("Id", nomes);
        Assert.DoesNotContain("Ativo", nomes);
        Assert.DoesNotContain("DataCadastro", nomes);
    }

    [Fact]
    public void AparelhoFormViewModel_NaoExpoeClienteIdNemDataDeCadastro()
    {
        var nomes = Propriedades(typeof(AparelhoFormViewModel));

        Assert.DoesNotContain("Id", nomes);
        Assert.DoesNotContain("ClienteId", nomes);
        Assert.DoesNotContain("DataCadastro", nomes);
    }

    [Fact]
    public void OrdemServicoFormViewModel_NaoExpoeCamposGeradosPeloServidor()
    {
        var nomes = Propriedades(typeof(OrdemServicoFormViewModel));

        Assert.DoesNotContain("NumeroOS", nomes);
        Assert.DoesNotContain("ValorTotal", nomes);
        Assert.DoesNotContain("DataEntrada", nomes);
        Assert.DoesNotContain("ClienteId", nomes);
        Assert.DoesNotContain("AparelhoId", nomes);
        Assert.DoesNotContain("Status", nomes);

        // O Id existe só para montar a URL; a conversão nunca o transfere.
        var modelo = new OrdemServicoFormViewModel { Id = 999, DefeitoRelatado = "Não carrega" };
        var ordem = modelo.ParaOrdemServico();
        Assert.Equal(0, ordem.Id);
    }

    [Fact]
    public void NovaOrdemServicoViewModel_NaoExpoeCamposGeradosPeloService()
    {
        var nomes = Propriedades(typeof(NovaOrdemServicoViewModel));

        Assert.DoesNotContain("NumeroOS", nomes);
        Assert.DoesNotContain("ValorTotal", nomes);
        Assert.DoesNotContain("DataEntrada", nomes);
        Assert.DoesNotContain("Status", nomes);
        // ClienteId existe, mas o servidor recarrega e revalida
        // (coberto em OrdemServicoServiceTests: cliente inexistente/inativo).
    }
}
