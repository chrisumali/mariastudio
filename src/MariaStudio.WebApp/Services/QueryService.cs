using System.Diagnostics;
using MariaStudio.WebApp.Models;
using MySqlConnector;

namespace MariaStudio.WebApp.Services
{
    public sealed class QueryService(MySqlDataSource dataSource)
    {
        public const int MaxRows = 500;

        public async Task<QueryResponse> ExecuteAsync(string sql, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return new QueryResponse(0, [], 0, "SQL is empty.");

            var clock = Stopwatch.StartNew();
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = 30;

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                var results = new List<ResultSetDto>();

                do
                {
                    if (reader.FieldCount == 0)
                        continue;

                    var columns = Enumerable.Range(0, reader.FieldCount)
                        .Select(reader.GetName)
                        .ToArray();

                    var rows = new List<IReadOnlyList<object?>>();
                    var truncated = false;
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        if (rows.Count >= MaxRows)
                        {
                            truncated = true;
                            break;
                        }

                        var row = new object?[reader.FieldCount];
                        for (var i = 0; i < reader.FieldCount; i++)
                            row[i] = await reader.IsDBNullAsync(i, cancellationToken) ? null : BoxValue(reader.GetValue(i));
                        rows.Add(row);
                    }

                    results.Add(new ResultSetDto(columns, rows, truncated));
                }
                while (await reader.NextResultAsync(cancellationToken));

                var affected = reader.RecordsAffected < 0 ? 0 : reader.RecordsAffected;
                return new QueryResponse(clock.ElapsedMilliseconds, results, affected, null);
            }
            catch (Exception ex) when (ex is MySqlException or OperationCanceledException)
            {
                return new QueryResponse(clock.ElapsedMilliseconds, [], 0, ex.Message);
            }
        }

        private static object BoxValue(object value) => value switch
        {
            byte[] bytes => Convert.ToBase64String(bytes),
            DateTime or DateTimeOffset or TimeSpan or Guid or string or bool or decimal
                or byte or sbyte or short or ushort or int or uint or long or ulong or float or double => value,
            _ => value.ToString() ?? ""
        };
    }
}
