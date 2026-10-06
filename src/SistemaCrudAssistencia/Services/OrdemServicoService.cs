using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Data;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Enums;
using SistemaCrudAssistencia.Models.Extensions;
using SistemaCrudAssistencia.Models.Filtros;

namespace SistemaCrudAssistencia.Services;

public interface IOrdemServicoService
{
    Task<OrdemServico?> BuscarAsync(int id, CancellationToken cancellationToken = default);

    Task<List<OrdemServico>> ListarAsync(FiltroOrdensServico filtro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre uma nova OS: valida o vínculo cliente/aparelho, calcula o total,
    /// gera o NumeroOS e registra o primeiro item do histórico.
    /// </summary>
    Task<OrdemServico> AbrirAsync(OrdemServico ordemServico, string? usuario, string? observacao = null, CancellationToken cancellationToken = default);

    /// <summary>Atualiza os campos da OS (diagnóstico, serviço, valores). Sempre recalcula o total.</summary>
    Task AtualizarAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default);

    Task AlterarStatusAsync(int id, StatusOrdemServico novoStatus, string? observacao, string? usuario, CancellationToken cancellationToken = default);

    /// <summary>Conclui o reparo: grava a data de conclusão e move a OS para "Pronto".</summary>
    Task FinalizarAsync(int id, string? observacao, string? usuario, CancellationToken cancellationToken = default);

    /// <summary>Registra a retirada: grava a data e move a OS para "Entregue".</summary>
    Task RegistrarRetiradaAsync(int id, string? observacao, string? usuario, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma anotação ao histórico sem mudar o status.</summary>
    Task RegistrarAnotacaoAsync(int id, string observacao, string? usuario, CancellationToken cancellationToken = default);
}

public class OrdemServicoService(AppDbContext contexto) : IOrdemServicoService
{
    public Task<OrdemServico?> BuscarAsync(int id, CancellationToken cancellationToken = default) =>
        contexto.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Aparelho)
            .Include(o => o.Historico.OrderByDescending(h => h.Data))
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<List<OrdemServico>> ListarAsync(FiltroOrdensServico filtro, CancellationToken cancellationToken = default)
    {
        var consulta = contexto.OrdensServico
            .AsNoTracking()
            .Include(o => o.Cliente)
            .Include(o => o.Aparelho)
            .AsQueryable();

        if (filtro.SomenteEmAndamento)
            consulta = consulta.Where(o =>
                o.Status != StatusOrdemServico.Entregue &&
                o.Status != StatusOrdemServico.Cancelado);

        if (filtro.Status.HasValue)
            consulta = consulta.Where(o => o.Status == filtro.Status.Value);

        if (filtro.ClienteId.HasValue)
            consulta = consulta.Where(o => o.ClienteId == filtro.ClienteId.Value);

        if (filtro.De.HasValue)
        {
            var inicio = filtro.De.Value.ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(o => o.DataEntrada >= inicio);
        }

        if (filtro.Ate.HasValue)
        {
            var fim = filtro.Ate.Value.ToDateTime(TimeOnly.MaxValue);
            consulta = consulta.Where(o => o.DataEntrada <= fim);
        }

        var termo = filtro.Busca?.Trim();

        if (!string.IsNullOrWhiteSpace(termo))
            consulta = AplicarBusca(consulta, termo);

        return await consulta
            .OrderByDescending(o => o.DataEntrada)
            .ThenByDescending(o => o.Id)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrdemServico> AbrirAsync(OrdemServico ordemServico, string? usuario, string? observacao = null, CancellationToken cancellationToken = default)
    {
        var cliente = await contexto.Clientes.FirstOrDefaultAsync(c => c.Id == ordemServico.ClienteId, cancellationToken)
            ?? throw new RegraDeNegocioException("Cliente informado não existe.");

        if (!cliente.Ativo)
            throw new RegraDeNegocioException("Cliente está inativo. Reative-o antes de abrir uma nova OS.");

        var aparelho = await contexto.Aparelhos.FirstOrDefaultAsync(a => a.Id == ordemServico.AparelhoId, cancellationToken)
            ?? throw new RegraDeNegocioException("Aparelho informado não existe.");

        // Impede vincular um aparelho de outro cliente à OS.
        if (aparelho.ClienteId != cliente.Id)
            throw new RegraDeNegocioException("O aparelho selecionado pertence a outro cliente.");

        ordemServico.DataEntrada = ordemServico.DataEntrada == default
            ? DateTime.Now
            : ordemServico.DataEntrada;

        ordemServico.DefeitoRelatado = (ordemServico.DefeitoRelatado ?? string.Empty).Trim();
        ordemServico.EstadoFisicoEntrada = Opcional(ordemServico.EstadoFisicoEntrada);
        ordemServico.AcessoriosEntregues = Opcional(ordemServico.AcessoriosEntregues);
        ordemServico.ObservacoesEntrada = Opcional(ordemServico.ObservacoesEntrada);

        ordemServico.Status = StatusOrdemServico.Recebido;
        ordemServico.DataConclusao = null;
        ordemServico.DataRetirada = null;
        ordemServico.AtualizarValorTotal();

        contexto.OrdensServico.Add(ordemServico);
        await contexto.SaveChangesAsync(cancellationToken);

        // O NumeroOS depende do Id gerado pelo banco: agora que ele existe, gravamos o número.
        ordemServico.GerarNumeroOS();

        contexto.Historicos.Add(new HistoricoOrdemServico
        {
            OrdemServicoId = ordemServico.Id,
            Data = DateTime.Now,
            StatusAnterior = null,
            NovoStatus = StatusOrdemServico.Recebido,
            Observacao = string.IsNullOrWhiteSpace(observacao)
                ? "Entrada do aparelho na assistência."
                : observacao.Trim(),
            Usuario = usuario
        });

        await contexto.SaveChangesAsync(cancellationToken);

        return ordemServico;
    }

    public async Task AtualizarAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
    {
        var atual = await contexto.OrdensServico
            .Include(o => o.Historico)
            .FirstOrDefaultAsync(o => o.Id == ordemServico.Id, cancellationToken)
            ?? throw new RegraDeNegocioException("Ordem de Serviço não encontrada.");

        if (!atual.Status.EstaEmAndamento())
            throw new RegraDeNegocioException(
                $"Não é possível editar uma OS {atual.Status.Descricao().ToLowerInvariant()}.");

        atual.DefeitoRelatado = (ordemServico.DefeitoRelatado ?? string.Empty).Trim();
        atual.EstadoFisicoEntrada = Opcional(ordemServico.EstadoFisicoEntrada);
        atual.AcessoriosEntregues = Opcional(ordemServico.AcessoriosEntregues);
        atual.ObservacoesEntrada = Opcional(ordemServico.ObservacoesEntrada);

        atual.DiagnosticoTecnico = Opcional(ordemServico.DiagnosticoTecnico);
        atual.ServicoRealizado = Opcional(ordemServico.ServicoRealizado);
        atual.PecasUtilizadas = Opcional(ordemServico.PecasUtilizadas);
        atual.ObservacoesInternas = Opcional(ordemServico.ObservacoesInternas);

        // Os valores vêm convertidos e validados pelo controller; o total é sempre recalculado aqui.
        atual.ValorPecas = ordemServico.ValorPecas;
        atual.ValorMaoDeObra = ordemServico.ValorMaoDeObra;
        atual.AtualizarValorTotal();

        await contexto.SaveChangesAsync(cancellationToken);
    }

    public Task AlterarStatusAsync(int id, StatusOrdemServico novoStatus, string? observacao, string? usuario, CancellationToken cancellationToken = default) =>
        MudarStatusAsync(id, novoStatus, observacao, usuario, false, cancellationToken);

    public Task FinalizarAsync(int id, string? observacao, string? usuario, CancellationToken cancellationToken = default) =>
        MudarStatusAsync(id, StatusOrdemServico.Pronto, observacao, usuario, true, cancellationToken);

    public Task RegistrarRetiradaAsync(int id, string? observacao, string? usuario, CancellationToken cancellationToken = default) =>
        MudarStatusAsync(id, StatusOrdemServico.Entregue, observacao, usuario, true, cancellationToken);

    public async Task RegistrarAnotacaoAsync(int id, string observacao, string? usuario, CancellationToken cancellationToken = default)
    {
        var ordemServico = await BuscarParaAlteracaoAsync(id, cancellationToken);

        contexto.Historicos.Add(new HistoricoOrdemServico
        {
            OrdemServicoId = ordemServico.Id,
            Data = DateTime.Now,
            StatusAnterior = ordemServico.Status,
            NovoStatus = ordemServico.Status,
            Observacao = observacao.Trim(),
            Usuario = usuario
        });

        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task MudarStatusAsync(int id, StatusOrdemServico novoStatus, string? observacao, string? usuario, bool marcarDatas, CancellationToken cancellationToken)
    {
        var ordemServico = await BuscarParaAlteracaoAsync(id, cancellationToken);

        if (!ordemServico.Status.EstaEmAndamento())
            throw new RegraDeNegocioException(
                $"Esta OS já está {ordemServico.Status.Descricao().ToLowerInvariant()} e não aceita mais alterações de status.");

        if (novoStatus == StatusOrdemServico.Entregue && !ordemServico.Status.PermiteRetirada())
            throw new RegraDeNegocioException(
                "Só é possível registrar a retirada quando a OS estiver com o status Pronto.");

        var statusAnterior = ordemServico.Status;

        ordemServico.Status = novoStatus;

        if (marcarDatas)
        {
            if (novoStatus == StatusOrdemServico.Pronto)
                ordemServico.DataConclusao ??= DateTime.Now;

            if (novoStatus == StatusOrdemServico.Entregue)
                ordemServico.DataRetirada ??= DateTime.Now;
        }
        else if (novoStatus == StatusOrdemServico.Pronto)
        {
            ordemServico.DataConclusao ??= DateTime.Now;
        }

        contexto.Historicos.Add(new HistoricoOrdemServico
        {
            OrdemServicoId = ordemServico.Id,
            Data = DateTime.Now,
            StatusAnterior = statusAnterior,
            NovoStatus = novoStatus,
            Observacao = Opcional(observacao),
            Usuario = usuario
        });

        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task<OrdemServico> BuscarParaAlteracaoAsync(int id, CancellationToken cancellationToken) =>
        await contexto.OrdensServico.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
        ?? throw new RegraDeNegocioException("Ordem de Serviço não encontrada.");

    /// <summary>
    /// Busca unificada da tela principal: número da OS, CPF, nome, telefone e IMEI/número de série.
    /// </summary>
    private static IQueryable<OrdemServico> AplicarBusca(IQueryable<OrdemServico> consulta, string termo)
    {
        var digitos = string.Concat(termo.Where(char.IsAsciiDigit));
        var texto = termo.ToLower();

        return consulta.Where(o =>
            (digitos.Length >= 2 && o.NumeroOS != null && o.NumeroOS.EndsWith(digitos))
            || (digitos.Length == 11 && o.Cliente!.Cpf == digitos)
            || (digitos.Length >= 3 && o.Cliente!.Telefone.Contains(digitos))
            || (digitos.Length >= 3 && o.Aparelho!.ImeiNumeroSerie != null
                && o.Aparelho.ImeiNumeroSerie.Contains(digitos))
            || o.Cliente!.NomeCompleto.ToLower().Contains(texto));
    }

    private static string? Opcional(string? valor)
    {
        var texto = valor?.Trim();
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }
}
