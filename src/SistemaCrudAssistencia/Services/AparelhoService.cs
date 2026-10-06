using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Data;
using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Services;

public interface IAparelhoService
{
    Task<Aparelho?> BuscarAsync(int id, CancellationToken cancellationToken = default);

    Task<List<Aparelho>> ListarPorClienteAsync(int clienteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Localiza um aparelho pelo IMEI/número de série, independentemente do cliente.
    /// Usado pela busca rápida.
    /// </summary>
    Task<List<Aparelho>> BuscarPorImeiAsync(string? imei, CancellationToken cancellationToken = default);

    Task CriarAsync(Aparelho aparelho, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Aparelho aparelho, CancellationToken cancellationToken = default);
}

public class AparelhoService(AppDbContext contexto) : IAparelhoService
{
    public Task<Aparelho?> BuscarAsync(int id, CancellationToken cancellationToken = default) =>
        contexto.Aparelhos.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<List<Aparelho>> ListarPorClienteAsync(int clienteId, CancellationToken cancellationToken = default) =>
        contexto.Aparelhos
            .AsNoTracking()
            .Where(a => a.ClienteId == clienteId)
            .OrderByDescending(a => a.Id)
            .ToListAsync(cancellationToken);

    public Task<List<Aparelho>> BuscarPorImeiAsync(string? imei, CancellationToken cancellationToken = default)
    {
        var termo = imei?.Trim();

        if (string.IsNullOrWhiteSpace(termo))
            return Task.FromResult(new List<Aparelho>());

        return contexto.Aparelhos
            .AsNoTracking()
            .Where(a => a.ImeiNumeroSerie != null
                        && (a.ImeiNumeroSerie == termo
                            || a.ImeiNumeroSerie.ToLower().Contains(termo.ToLower())))
            .Include(a => a.Cliente)
            .OrderByDescending(a => a.Id)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task CriarAsync(Aparelho aparelho, CancellationToken cancellationToken = default)
    {
        if (!await ClienteExisteAsync(aparelho.ClienteId, cancellationToken))
            throw new RegraDeNegocioException("Cliente informado não existe.");

        Preparar(aparelho);
        aparelho.DataCadastro = DateTime.Now;

        contexto.Aparelhos.Add(aparelho);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Aparelho aparelho, CancellationToken cancellationToken = default)
    {
        var atual = await contexto.Aparelhos.FirstOrDefaultAsync(a => a.Id == aparelho.Id, cancellationToken)
            ?? throw new RegraDeNegocioException("Aparelho não encontrado.");

        if (!await ClienteExisteAsync(aparelho.ClienteId, cancellationToken))
            throw new RegraDeNegocioException("Cliente informado não existe.");

        Preparar(aparelho);

        atual.TipoAparelho = aparelho.TipoAparelho;
        atual.Marca = aparelho.Marca;
        atual.Modelo = aparelho.Modelo ?? string.Empty;
        atual.ImeiNumeroSerie = aparelho.ImeiNumeroSerie;
        atual.Cor = aparelho.Cor;
        atual.Observacoes = aparelho.Observacoes;

        await contexto.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> ClienteExisteAsync(int clienteId, CancellationToken cancellationToken) =>
        contexto.Clientes.AnyAsync(c => c.Id == clienteId, cancellationToken);

    private static void Preparar(Aparelho aparelho)
    {
        aparelho.TipoAparelho = aparelho.TipoAparelho.Trim();
        aparelho.Marca = aparelho.Marca.Trim();
        aparelho.Modelo = (aparelho.Modelo ?? string.Empty).Trim();
        aparelho.ImeiNumeroSerie = Opcional(aparelho.ImeiNumeroSerie);
        aparelho.Cor = Opcional(aparelho.Cor);
        aparelho.Observacoes = Opcional(aparelho.Observacoes);
    }

    private static string? Opcional(string? valor)
    {
        var texto = valor?.Trim();
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }
}
