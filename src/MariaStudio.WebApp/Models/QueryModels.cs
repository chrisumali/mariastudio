namespace MariaStudio.WebApp.Models
{
    public sealed record QueryRequest(string Sql);

    public sealed record QueryResponse(
        long ElapsedMs,
        IReadOnlyList<ResultSetDto> Results,
        int RowsAffected,
        string? Error);

    public sealed record ResultSetDto(
        IReadOnlyList<string> Columns,
        IReadOnlyList<IReadOnlyList<object?>> Rows,
        bool Truncated);

    public sealed record SchemaNode(string Name, string Kind, IReadOnlyList<SchemaNode>? Children);

    public sealed record CompletionItemDto(string Name, string Kind, string? Detail);
}
