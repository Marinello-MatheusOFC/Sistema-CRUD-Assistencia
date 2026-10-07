using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Data;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.Validations;

namespace SistemaCrudAssistencia.Services;

public interface IClienteService
{
    Task<Cliente?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Carrega a ficha do cliente com aparelhos e ordens somente para consulta.</summary>
    Task<Cliente?> BuscarDetalhesAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Busca pelo CPF já normalizado (somente dígitos).</summary>
    Task<Cliente?> BuscarPorCpfAsync(string? cpf, CancellationToken cancellationToken = default);

    Task<List<Cliente>> ListarAsync(string? busca = null, bool somenteAtivos = false, CancellationToken cancellationToken = default);

    Task CriarAsync(Cliente cliente, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Cliente cliente, CancellationToken cancellationToken = default);

    /// <summary>Desativa o cliente preservando todo o histórico. Não exclui.</summary>
    Task DesativarAsync(int id, CancellationToken cancellationToken = default);

    Task ReativarAsync(int id, CancellationToken cancellationToken = default);
}

public class ClienteService(AppDbContext contexto) : IClienteService
{
    public Task<Cliente?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        contexto.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Cliente?> BuscarDetalhesAsync(int id, CancellationToken cancellationToken = default) =>
        contexto.Clientes
            .AsNoTracking()
            .Include(c => c.Aparelhos)
            .Include(c => c.OrdensServico)
                .ThenInclude(o => o.Aparelho)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Cliente?> BuscarPorCpfAsync(string? cpf, CancellationToken cancellationToken = default)
    {
        var normalizado = Cpf.Normalizar(cpf);

        if (normalizado.Length != 11)
            return Task.FromResult<Cliente?>(null);

        return contexto.Clientes.FirstOrDefaultAsync(c => c.Cpf == normalizado, cancellationToken);
    }

    public Task<List<Cliente>> ListarAsync(string? busca = null, bool somenteAtivos = false, CancellationToken cancellationToken = default)
    {
        var consulta = contexto.Clientes.AsNoTracking().AsQueryable();

        if (somenteAtivos)
            consulta = consulta.Where(c => c.Ativo);

        var termo = busca?.Trim();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var digitos = string.Concat(termo.Where(char.IsAsciiDigit));

            consulta = termo.Contains('@')
                ? consulta.Where(c => c.Email != null && ContemTexto(c.Email, termo))
                : PorCpfOuNome(consulta, digitos, termo);
        }

        return consulta
            .OrderByDescending(c => c.Ativo)
            .ThenBy(c => c.NomeCompleto)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task CriarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        cliente.Cpf = Cpf.Normalizar(cliente.Cpf);
        cliente.Telefone = Telefone.Normalizar(cliente.Telefone);
        cliente.Cep = Cep.Normalizar(cliente.Cep);
        cliente.NomeCompleto = cliente.NomeCompleto.Trim();
        cliente.Email = NormalizarOpcional(cliente.Email);
        cliente.Logradouro = cliente.Logradouro.Trim();
        cliente.Numero = NormalizarOpcional(cliente.Numero);
        cliente.Complemento = NormalizarOpcional(cliente.Complemento);
        cliente.Bairro = cliente.Bairro.Trim();
        cliente.Cidade = cliente.Cidade.Trim();
        cliente.Estado = cliente.Estado.Trim().ToUpperInvariant();
        cliente.DataCadastro = DateTime.Now;

        await GarantirCpfLivreAsync(cliente.Cpf, cliente.Id, cancellationToken);

        contexto.Clientes.Add(cliente);

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EhViolacaoDeCpfUnico(ex))
        {
            throw new RegraDeNegocioException("Já existe um cliente cadastrado com este CPF.");
        }
    }

    public async Task AtualizarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        var atual = await contexto.Clientes.FirstOrDefaultAsync(c => c.Id == cliente.Id, cancellationToken)
            ?? throw new RegraDeNegocioException("Cliente não encontrado.");

        cliente.Cpf = Cpf.Normalizar(cliente.Cpf);
        cliente.Telefone = Telefone.Normalizar(cliente.Telefone);
        cliente.Cep = Cep.Normalizar(cliente.Cep);
        cliente.NomeCompleto = cliente.NomeCompleto.Trim();

        await GarantirCpfLivreAsync(cliente.Cpf, cliente.Id, cancellationToken);

        atual.NomeCompleto = cliente.NomeCompleto;
        atual.Cpf = cliente.Cpf;
        atual.Telefone = cliente.Telefone;
        atual.Email = NormalizarOpcional(cliente.Email);
        atual.Cep = cliente.Cep;
        atual.Logradouro = cliente.Logradouro.Trim();
        atual.Numero = NormalizarOpcional(cliente.Numero);
        atual.Complemento = NormalizarOpcional(cliente.Complemento);
        atual.Bairro = cliente.Bairro.Trim();
        atual.Cidade = cliente.Cidade.Trim();
        atual.Estado = cliente.Estado.Trim().ToUpperInvariant();

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EhViolacaoDeCpfUnico(ex))
        {
            throw new RegraDeNegocioException("Já existe um cliente cadastrado com este CPF.");
        }
    }

    public async Task DesativarAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await contexto.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new RegraDeNegocioException("Cliente não encontrado.");

        cliente.Ativo = false;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ReativarAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await contexto.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new RegraDeNegocioException("Cliente não encontrado.");

        cliente.Ativo = true;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    private async Task GarantirCpfLivreAsync(string cpf, int ignorarId, CancellationToken cancellationToken)
    {
        if (await contexto.Clientes.AnyAsync(c => c.Cpf == cpf && c.Id != ignorarId, cancellationToken))
            throw new RegraDeNegocioException("Já existe um cliente cadastrado com este CPF.");
    }

    private static IQueryable<Cliente> PorCpfOuNome(IQueryable<Cliente> consulta, string digitos, string termo) =>
        digitos.Length switch
        {
            11 => consulta.Where(c => c.Cpf == digitos),
            >= 3 => consulta.Where(c => c.Telefone.Contains(digitos) || ContemTexto(c.NomeCompleto, termo)),
            _ => consulta.Where(c => ContemTexto(c.NomeCompleto, termo))
        };

    /// <summary>
    /// "Contém texto" sem diferenciar maiúsculas de minúsculas, traduzido para SQL
    /// em PostgreSQL e SQLite (nada de ILike, que é específico do PostgreSQL).
    /// </summary>
    private static bool ContemTexto(string coluna, string termo) =>
        coluna.ToLower().Contains(termo.ToLower());

    private static string? NormalizarOpcional(string? valor)
    {
        var texto = valor?.Trim();
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }

    /// <summary>
    /// Detecta a violação do índice único de CPF (PostgreSQL pelo nome do índice,
    /// SQLite pela coluna). Trata a corrida entre duas telas abertos ao mesmo tempo.
    /// </summary>
    private static bool EhViolacaoDeCpfUnico(DbUpdateException exception)
    {
        var mensagem = exception.InnerException?.Message ?? string.Empty;

        return mensagem.Contains("ix_clientes_cpf", StringComparison.OrdinalIgnoreCase)
            || mensagem.Contains("clientes.Cpf", StringComparison.OrdinalIgnoreCase)
            || mensagem.Contains("clientes_cpf", StringComparison.OrdinalIgnoreCase);
    }
}
