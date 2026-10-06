using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Data;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Services;

namespace SistemaCrudAssistencia.Tests;

/// <summary>
/// Cria um AppDbContext em SQLite (banco relacional em memória) para testar as
/// regras de negócio com constraints reais, sem precisar de um PostgreSQL instalado.
/// </summary>
public static class BancoEmMemoria
{
    public static AppDbContext Criar()
    {
        var conexao = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conexao.Open();

        var contexto = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(conexao)
                .Options);

        contexto.Database.EnsureCreated();

        return contexto;
    }

    public static Cliente NovoCliente(string? cpf = "12345678909", string nome = "Maria Aparecida Souza") =>
        new()
        {
            NomeCompleto = nome,
            Cpf = cpf ?? string.Empty,
            Telefone = "11987654321",
            Cep = "01001000",
            Logradouro = "Praça da Sé",
            Numero = "150",
            Bairro = "Sé",
            Cidade = "São Paulo",
            Estado = "SP",
            Ativo = true
        };

    public static Aparelho NovoAparelho(int clienteId, string? imei = "352099001761481") =>
        new()
        {
            ClienteId = clienteId,
            TipoAparelho = "Celular",
            Marca = "Samsung",
            Modelo = "Galaxy S21",
            ImeiNumeroSerie = imei,
            Cor = "Preto"
        };

    public static OrdemServico NovaOrdem(int clienteId, int aparelhoId, string defeito = "Tela quebrada após queda.") =>
        new()
        {
            ClienteId = clienteId,
            AparelhoId = aparelhoId,
            DataEntrada = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Unspecified),
            DefeitoRelatado = defeito
        };

    /// <summary>
    /// Cadastra um cliente novo e o primeiro aparelho dele, como aconteceria no balcão.
    /// </summary>
    public static async Task<Aparelho> ClienteComAparelhoAsync(
        this AppDbContext contexto, Cliente cliente, string? imei = null)
    {
        await new ClienteService(contexto).CriarAsync(cliente);
        var aparelho = NovoAparelho(cliente.Id, imei);
        await new AparelhoService(contexto).CriarAsync(aparelho);
        return aparelho;
    }
}
