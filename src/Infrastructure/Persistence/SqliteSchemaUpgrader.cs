using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace gis_photo_sharing_app.Infrastructure.Persistence;

public static class SqliteSchemaUpgrader
{
    public static async Task<IReadOnlyList<string>> UpgradeAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var applied = new List<string>();
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            var existingColumns = await GetColumnsAsync(connection, cancellationToken);

            await AddColumnIfMissingAsync(
                connection,
                existingColumns,
                "LocationName",
                "TEXT NULL",
                applied,
                cancellationToken);

            await AddColumnIfMissingAsync(
                connection,
                existingColumns,
                "Category",
                "TEXT NOT NULL DEFAULT 'General'",
                applied,
                cancellationToken);

            await AddColumnIfMissingAsync(
                connection,
                existingColumns,
                "IsPublic",
                "INTEGER NOT NULL DEFAULT 1",
                applied,
                cancellationToken);

            await CreateIndexIfMissingAsync(
                connection,
                "IX_Photos_CreatedByUserId",
                "CREATE INDEX IF NOT EXISTS \"IX_Photos_CreatedByUserId\" ON \"Photos\" (\"CreatedByUserId\")",
                applied,
                cancellationToken);

            await CreateIndexIfMissingAsync(
                connection,
                "IX_Photos_Created",
                "CREATE INDEX IF NOT EXISTS \"IX_Photos_Created\" ON \"Photos\" (\"Created\")",
                applied,
                cancellationToken);

            await CreateIndexIfMissingAsync(
                connection,
                "IX_Photos_Category",
                "CREATE INDEX IF NOT EXISTS \"IX_Photos_Category\" ON \"Photos\" (\"Category\")",
                applied,
                cancellationToken);

            await CreateIndexIfMissingAsync(
                connection,
                "IX_Photos_IsPublic",
                "CREATE INDEX IF NOT EXISTS \"IX_Photos_IsPublic\" ON \"Photos\" (\"IsPublic\")",
                applied,
                cancellationToken);

            await CreateIndexIfMissingAsync(
                connection,
                "IX_Photos_Latitude_Longitude",
                "CREATE INDEX IF NOT EXISTS \"IX_Photos_Latitude_Longitude\" ON \"Photos\" (\"Latitude\", \"Longitude\")",
                applied,
                cancellationToken);

            if (applied.Count > 0)
            {
                logger.LogInformation(
                    "SQLite schema upgrade applied {Count} change(s): {Changes}",
                    applied.Count,
                    string.Join(", ", applied));
            }
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }

        return applied;
    }

    private static async Task<HashSet<string>> GetColumnsAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        var columns = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Photos');";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }

    private static async Task AddColumnIfMissingAsync(
        System.Data.Common.DbConnection connection,
        HashSet<string> existingColumns,
        string columnName,
        string definition,
        List<string> applied,
        CancellationToken cancellationToken)
    {
        if (existingColumns.Contains(columnName))
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE \"Photos\" ADD COLUMN \"{columnName}\" {definition};";
        await command.ExecuteNonQueryAsync(cancellationToken);

        existingColumns.Add(columnName);
        applied.Add($"column {columnName}");
    }

    private static async Task CreateIndexIfMissingAsync(
        System.Data.Common.DbConnection connection,
        string indexName,
        string sql,
        List<string> applied,
        CancellationToken cancellationToken)
    {
        if (await IndexExistsAsync(connection, indexName, cancellationToken))
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);

        applied.Add($"index {indexName}");
    }

    private static async Task<bool> IndexExistsAsync(
        System.Data.Common.DbConnection connection,
        string indexName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'index' AND name = $name LIMIT 1;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = indexName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null && result != System.DBNull.Value;
    }
}
