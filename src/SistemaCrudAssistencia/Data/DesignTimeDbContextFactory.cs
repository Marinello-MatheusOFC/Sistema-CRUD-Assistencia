using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SistemaCrudAssistencia.Models.Entities;
using Microsoft.Extensions.Configuration;

namespace SistemaCrudAssistencia.Data;

/// <summary>
/// Permite criar migrations sem precisar subir a aplicação.
/// Prioridade da connection string: variável de ambiente
/// ConnectionStrings__Default (a mesma que a aplicação exige em runtime)
/// -> configuration -> placeholder de design-time, usado SOMENTE para
/// montar o modelo da migration (a migration vem do modelo, não do banco).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionStringDesignTime =
        "Host=localhost;Port=5432;Database=assistencia;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = new ConfigurationBuilder().Build()["ConnectionStrings:Default"];

        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = ConnectionStringDesignTime;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName()!.Name);
                npgsql.MigrationsHistoryTable("__migrations_historico");
            })
            .Options;

        return new AppDbContext(options);
    }
}
