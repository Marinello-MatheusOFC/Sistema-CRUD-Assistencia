using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Data.Configuracoes;

public class AparelhoConfiguration : IEntityTypeConfiguration<Aparelho>
{
    public void Configure(EntityTypeBuilder<Aparelho> builder)
    {
        builder.ToTable("aparelhos");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TipoAparelho).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Marca).HasMaxLength(60).IsRequired();
        builder.Property(a => a.Modelo).HasMaxLength(80);
        builder.Property(a => a.ImeiNumeroSerie).HasMaxLength(40);
        builder.Property(a => a.Cor).HasMaxLength(40);
        builder.Property(a => a.Observacoes).HasMaxLength(500);
        builder.Property(a => a.DataCadastro).ComoDataLocal();

        builder.HasIndex(a => a.ImeiNumeroSerie)
            .HasDatabaseName("ix_aparelhos_imei_numero_serie");

        builder.HasOne(a => a.Cliente)
            .WithMany(c => c.Aparelhos)
            .HasForeignKey(a => a.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
