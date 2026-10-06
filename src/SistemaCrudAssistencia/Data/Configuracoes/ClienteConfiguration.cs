using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaCrudAssistencia.Models.Entities;

namespace SistemaCrudAssistencia.Data.Configuracoes;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NomeCompleto).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Cpf).HasMaxLength(11).IsRequired();
        builder.Property(c => c.Telefone).HasMaxLength(11).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.Cep).HasMaxLength(8).IsRequired();
        builder.Property(c => c.Logradouro).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Numero).HasMaxLength(20);
        builder.Property(c => c.Complemento).HasMaxLength(100);
        builder.Property(c => c.Bairro).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Cidade).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Estado).HasMaxLength(2).IsRequired();
        builder.Property(c => c.Ativo).HasDefaultValue(true);
        builder.Property(c => c.DataCadastro).ComoDataLocal();

        builder.HasIndex(c => c.Cpf)
            .IsUnique()
            .HasDatabaseName("ix_clientes_cpf");

        builder.HasIndex(c => c.NomeCompleto)
            .HasDatabaseName("ix_clientes_nome");

        builder.HasIndex(c => c.Telefone)
            .HasDatabaseName("ix_clientes_telefone");
    }
}
