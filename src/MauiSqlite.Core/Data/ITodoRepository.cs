using MauiSqlite.Core.Models;

namespace MauiSqlite.Core.Data;

/// <summary>
/// Persistence contract for <see cref="TodoItem"/> entities.
/// </summary>
public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> AddAsync(TodoItem item, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TodoItem item, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<int> DeleteCompletedAsync(CancellationToken cancellationToken = default);
}
