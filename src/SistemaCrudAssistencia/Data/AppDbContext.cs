using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaCrudAssistencia.Data.Configuracoes;
using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Data;

public class AppDbContext : IdentityDbContext<Usuario>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Aparelho> Aparelhos => Set<Aparelho>();

    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();

    public DbSet<HistoricoOrdemServico> Historicos => Set<HistoricoOrdemServico>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<Usuario>()
            .Property(u => u.DataCadastro)
            .HasColumnType(ConfiguracaoColuna.TipoDataLocal);
    }
}
