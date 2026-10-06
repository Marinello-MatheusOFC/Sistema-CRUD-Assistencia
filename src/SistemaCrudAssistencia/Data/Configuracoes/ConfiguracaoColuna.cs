using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SistemaCrudAssistencia.Data.Configuracoes;

/// <summary>
/// Datas e horários da assistência são horários locais (data de entrada do aparelho,
/// data de retirada), não UTC. "timestamp without time zone" deixa isso explícito
/// no banco e evita deslocamentos de fuso na leitura.
/// </summary>
internal static class ConfiguracaoColuna
{
    public const string TipoDataLocal = "timestamp without time zone";

    public static PropertyBuilder<DateTime> ComoDataLocal(this PropertyBuilder<DateTime> builder) =>
        builder.HasColumnType(TipoDataLocal);

    public static PropertyBuilder<DateTime?> ComoDataLocal(this PropertyBuilder<DateTime?> builder) =>
        builder.HasColumnType(TipoDataLocal);
}
