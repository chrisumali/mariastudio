using MariaStudio.WebApp.Models;
using MySqlConnector;

namespace MariaStudio.WebApp.Services
{
    public sealed class SchemaService(MySqlDataSource dataSource)
    {
        public async Task<IReadOnlyList<SchemaNode>> GetTreeAsync(CancellationToken cancellationToken)
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT TABLE_NAME
                FROM information_schema.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                ORDER BY TABLE_NAME
                """;

            var tables = new List<SchemaNode>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                tables.Add(new SchemaNode(reader.GetString(0), "table", null));
            return tables;
        }

        public async Task<IReadOnlyList<CompletionItemDto>> CompleteAsync(
            string prefix, CancellationToken cancellationToken)
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT TABLE_NAME AS Name, 'table' AS Kind, TABLE_TYPE AS Detail
                FROM information_schema.TABLES
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME LIKE @prefix
                UNION ALL
                SELECT COLUMN_NAME, 'column', TABLE_NAME
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE() AND COLUMN_NAME LIKE @prefix
                ORDER BY Kind, Name
                LIMIT 50
                """;
            command.Parameters.AddWithValue("@prefix", prefix + "%");

            var items = new List<CompletionItemDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                items.Add(new CompletionItemDto(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            return items;
        }
    }
}
