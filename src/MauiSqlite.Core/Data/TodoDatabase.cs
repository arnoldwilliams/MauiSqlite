using Microsoft.Data.Sqlite;

namespace MauiSqlite.Core.Data;

/// <summary>
/// Owns the SQLite connection string and creates initialized connections.
/// </summary>
public class TodoDatabase : IAsyncDisposable
{
    private readonly string _connectionString;

    // A shared-cache in-memory database is destroyed once its last connection
    // closes, so hold one open for the lifetime of this instance.
    private readonly SqliteConnection? _keepAliveConnection;

    /// <summary>
    /// Creates a database rooted at <paramref name="databasePath"/>.
    /// </summary>
    /// <param name="databasePath">
    /// Path to the database file, or <c>:memory:</c> for an in-memory database.
    /// </param>
    public TodoDatabase(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        DatabasePath = databasePath;
        IsInMemory = databasePath is ":memory:" or "";

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = IsInMemory ? SqliteOpenMode.Memory : SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
        };

        _connectionString = builder.ToString();

        if (IsInMemory)
        {
            _keepAliveConnection = new SqliteConnection(_connectionString);
            _keepAliveConnection.Open();
        }
    }

    public string DatabasePath { get; }

    public bool IsInMemory { get; }

    public string ConnectionString => _connectionString;

    /// <summary>
    /// Opens a connection and ensures the schema exists.
    /// </summary>
    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_keepAliveConnection is not null)
        {
            await _keepAliveConnection.DisposeAsync().ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS TodoItems (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Title        TEXT    NOT NULL,
                Notes        TEXT    NULL,
                IsCompleted  INTEGER NOT NULL DEFAULT 0,
                Priority     INTEGER NOT NULL DEFAULT 1,
                CreatedUtc   TEXT    NOT NULL,
                DueDateUtc   TEXT    NULL
            );

            CREATE INDEX IF NOT EXISTS IX_TodoItems_IsCompleted
                ON TodoItems (IsCompleted);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
