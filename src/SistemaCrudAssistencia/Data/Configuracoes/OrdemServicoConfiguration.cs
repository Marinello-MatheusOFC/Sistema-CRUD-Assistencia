using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Data.Configuracoes;

public class OrdemServicoConfiguration : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> builder)
    {
        builder.ToTable("ordens_servico");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.NumeroOS).HasMaxLength(20);
        builder.Property(o => o.DefeitoRelatado).HasMaxLength(1000).IsRequired();
        builder.Property(o => o.EstadoFisicoEntrada).HasMaxLength(1000);
        builder.Property(o => o.AcessoriosEntregues).HasMaxLength(500);
        builder.Property(o => o.ObservacoesEntrada).HasMaxLength(1000);
        builder.Property(o => o.DiagnosticoTecnico).HasMaxLength(2000);
        builder.Property(o => o.ServicoRealizado).HasMaxLength(2000);
        builder.Property(o => o.PecasUtilizadas).HasMaxLength(1000);
        builder.Property(o => o.ObservacoesInternas).HasMaxLength(1000);

        builder.Property(o => o.DataEntrada).ComoDataLocal();
        builder.Property(o => o.DataConclusao).ComoDataLocal();
        builder.Property(o => o.DataRetirada).ComoDataLocal();

        builder.Property(o => o.ValorPecas).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(o => o.ValorMaoDeObra).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(o => o.ValorTotal).HasPrecision(18, 2).HasDefaultValue(0m);

        builder.HasIndex(o => o.NumeroOS)
            .IsUnique()
            .HasDatabaseName("ix_ordens_servico_numero_os");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("ix_ordens_servico_status");

        builder.HasIndex(o => o.DataEntrada)
            .IsDescending()
            .HasDatabaseName("ix_ordens_servico_data_entrada");

        builder.HasIndex(o => new { o.ClienteId, o.DataEntrada })
            .HasDatabaseName("ix_ordens_servico_cliente_data_entrada");

        builder.HasOne(o => o.Cliente)
            .WithMany(c => c.OrdensServico)
            .HasForeignKey(o => o.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Aparelho)
            .WithMany(a => a.OrdensServico)
            .HasForeignKey(o => o.AparelhoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
