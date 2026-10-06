using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SistemaCrudAssistencia.Models.Entities;
using Microsoft.Extensions.Configuration;

namespace SistemaCrudAssistencia.Data;

/// <summary>
/// Permite criar migrations sem precisar subir a aplicação e sem conexão com o banco.
/// A connection string usada aqui serve apenas para montar o modelo; a migration
/// é gerada a partir do modelo, não a partir de um banco existente.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionStringDesignTime =
        "Host=localhost;Port=5432;Database=assistencia;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? new ConfigurationBuilder().Build()["ConnectionStrings:Default"]
            ?? ConnectionStringDesignTime;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(
                typeof(AppDbContext).Assembly.GetName()!.Name))
            .Options;

        return new AppDbContext(options);
    }
}
