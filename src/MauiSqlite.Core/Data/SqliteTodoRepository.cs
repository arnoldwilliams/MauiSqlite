using System.Globalization;
using MauiSqlite.Core.Models;
using Microsoft.Data.Sqlite;

namespace MauiSqlite.Core.Data;

/// <summary>
/// SQLite-backed <see cref="ITodoRepository"/> using Microsoft.Data.Sqlite.
/// </summary>
public class SqliteTodoRepository : ITodoRepository
{
    private const string SelectColumns =
        "Id, Title, Notes, IsCompleted, Priority, CreatedUtc, DueDateUtc";

    private readonly TodoDatabase _database;

    public SqliteTodoRepository(TodoDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {SelectColumns}
            FROM TodoItems
            ORDER BY IsCompleted, Priority DESC, CreatedUtc DESC;
            """;

        var items = new List<TodoItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(Map(reader));
        }

        return items;
    }

    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {SelectColumns}
            FROM TodoItems
            WHERE Id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Map(reader) : null;
    }

    public async Task<int> AddAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        Validate(item);

        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO TodoItems (Title, Notes, IsCompleted, Priority, CreatedUtc, DueDateUtc)
            VALUES ($title, $notes, $isCompleted, $priority, $createdUtc, $dueDateUtc);
            SELECT last_insert_rowid();
            """;

        AddEntityParameters(command, item);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        item.Id = Convert.ToInt32(result, CultureInfo.InvariantCulture);
        return item.Id;
    }

    public async Task<bool> UpdateAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        Validate(item);

        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE TodoItems
            SET Title = $title,
                Notes = $notes,
                IsCompleted = $isCompleted,
                Priority = $priority,
                CreatedUtc = $createdUtc,
                DueDateUtc = $dueDateUtc
            WHERE Id = $id;
            """;

        AddEntityParameters(command, item);
        command.Parameters.AddWithValue("$id", item.Id);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM TodoItems WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<int> DeleteCompletedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM TodoItems WHERE IsCompleted = 1;";
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddEntityParameters(SqliteCommand command, TodoItem item)
    {
        command.Parameters.AddWithValue("$title", item.Title);
        command.Parameters.AddWithValue("$notes", (object?)item.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$isCompleted", item.IsCompleted ? 1 : 0);
        command.Parameters.AddWithValue("$priority", (int)item.Priority);
        command.Parameters.AddWithValue("$createdUtc", ToStorage(item.CreatedUtc));
        command.Parameters.AddWithValue(
            "$dueDateUtc",
            item.DueDateUtc is { } due ? ToStorage(due) : DBNull.Value);
    }

    private static TodoItem Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Title = reader.GetString(1),
        Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
        IsCompleted = reader.GetInt32(3) != 0,
        Priority = (TodoPriority)reader.GetInt32(4),
        CreatedUtc = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        DueDateUtc = reader.IsDBNull(6)
            ? null
            : DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
    };

    private static string ToStorage(DateTime value) =>
        value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static void Validate(TodoItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Title))
        {
            throw new ArgumentException("Title is required.", nameof(item));
        }
    }
}
