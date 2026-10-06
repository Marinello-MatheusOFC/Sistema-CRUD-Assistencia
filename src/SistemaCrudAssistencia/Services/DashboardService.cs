using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Data;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Enums;

namespace SistemaCrudAssistencia.Services;

/// <summary>
/// Números do painel inicial. Apenas o que é útil no dia a dia.
/// </summary>
public class ResumoDashboard
{
    public int Abertas { get; init; }

    public int EmAnalise { get; init; }

    public int AguardandoPeca { get; init; }

    public int EmReparo { get; init; }

    public int ProntasParaRetirada { get; init; }

    public List<OrdemServico> UltimasOrdens { get; init; } = [];
}

public interface IDashboardService
{
    Task<ResumoDashboard> ObterAsync(CancellationToken cancellationToken = default);
}

public class DashboardService(AppDbContext contexto) : IDashboardService
{
    public async Task<ResumoDashboard> ObterAsync(CancellationToken cancellationToken = default)
    {
        var emAndamento = new[]
        {
            StatusOrdemServico.Recebido,
            StatusOrdemServico.EmAnalise,
            StatusOrdemServico.AguardandoAprovacao,
            StatusOrdemServico.AguardandoPeca,
            StatusOrdemServico.EmReparo
        };

        var contagens = await contexto.OrdensServico
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Total, cancellationToken);

        int Total(StatusOrdemServico status) => contagens.GetValueOrDefault(status);

        var ultimas = await contexto.OrdensServico
            .AsNoTracking()
            .Include(o => o.Cliente)
            .Include(o => o.Aparelho)
            .OrderByDescending(o => o.DataEntrada)
            .ThenByDescending(o => o.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new ResumoDashboard
        {
            Abertas = emAndamento.Sum(Total),
            EmAnalise = Total(StatusOrdemServico.EmAnalise),
            AguardandoPeca = Total(StatusOrdemServico.AguardandoPeca),
            EmReparo = Total(StatusOrdemServico.EmReparo),
            ProntasParaRetirada = Total(StatusOrdemServico.Pronto),
            UltimasOrdens = ultimas
        };
    }
}
