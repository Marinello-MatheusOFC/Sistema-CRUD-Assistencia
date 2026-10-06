using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Data.Configuracoes;

public class HistoricoOrdemServicoConfiguration : IEntityTypeConfiguration<HistoricoOrdemServico>
{
    public void Configure(EntityTypeBuilder<HistoricoOrdemServico> builder)
    {
        builder.ToTable("historicos_ordem_servico");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Observacao).HasMaxLength(1000);
        builder.Property(h => h.Usuario).HasMaxLength(256);
        builder.Property(h => h.Data).ComoDataLocal();

        builder.HasIndex(h => new { h.OrdemServicoId, h.Data })
            .HasDatabaseName("ix_historicos_ordem_servico_ordem_data");

        builder.HasOne(h => h.OrdemServico)
            .WithMany(o => o.Historico)
            .HasForeignKey(h => h.OrdemServicoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
