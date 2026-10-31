using MauiSqlite.Core.Data;
using MauiSqlite.Core.Models;

namespace MauiSqlite.Tests.Data;

/// <summary>
/// Exercises the real SQLite repository against an isolated in-memory database.
/// </summary>
public sealed class SqliteTodoRepositoryTests : IAsyncLifetime
{
    private TodoDatabase _database = null!;
    private SqliteTodoRepository _repository = null!;

    public Task InitializeAsync()
    {
        // A shared-cache in-memory database keeps tests isolated and fast.
        _database = new TodoDatabase(":memory:");
        _repository = new SqliteTodoRepository(_database);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    private static TodoItem NewItem(
        string title = "Write tests",
        TodoPriority priority = TodoPriority.Normal,
        string? notes = null,
        bool completed = false) => new()
        {
            Title = title,
            Notes = notes,
            Priority = priority,
            IsCompleted = completed,
            CreatedUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        };

    [Fact]
    public void Constructor_RejectsNullDatabase()
    {
        Assert.Throws<ArgumentNullException>(() => new SqliteTodoRepository(null!));
    }

    [Fact]
    public async Task AddAsync_AssignsIncrementingIds()
    {
        var firstId = await _repository.AddAsync(NewItem("First"));
        var secondId = await _repository.AddAsync(NewItem("Second"));

        Assert.True(firstId > 0);
        Assert.True(secondId > firstId);
    }

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsAllFields()
    {
        var due = new DateTime(2026, 5, 6, 7, 8, 9, DateTimeKind.Utc);
        var item = NewItem("Buy milk", TodoPriority.High, "2% only");
        item.DueDateUtc = due;

        var id = await _repository.AddAsync(item);
        var loaded = await _repository.GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal(id, loaded!.Id);
        Assert.Equal("Buy milk", loaded.Title);
        Assert.Equal("2% only", loaded.Notes);
        Assert.Equal(TodoPriority.High, loaded.Priority);
        Assert.False(loaded.IsCompleted);
        Assert.Equal(item.CreatedUtc, loaded.CreatedUtc);
        Assert.Equal(due, loaded.DueDateUtc);
    }

    [Fact]
    public async Task AddAsync_NullNotesAndDueDate_AreStoredAsNull()
    {
        var id = await _repository.AddAsync(NewItem(notes: null));
        var loaded = await _repository.GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Null(loaded!.Notes);
        Assert.Null(loaded.DueDateUtc);
    }

    [Fact]
    public async Task AddAsync_BlankTitle_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddAsync(NewItem("   ")));
    }

    [Fact]
    public async Task AddAsync_NullItem_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        Assert.Null(await _repository.GetByIdAsync(12345));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryItem()
    {
        await _repository.AddAsync(NewItem("A"));
        await _repository.AddAsync(NewItem("B"));
        await _repository.AddAsync(NewItem("C"));

        var all = await _repository.GetAllAsync();

        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task GetAllAsync_OrdersIncompleteBeforeComplete()
    {
        await _repository.AddAsync(NewItem("Done", completed: true));
        await _repository.AddAsync(NewItem("Not done"));

        var all = await _repository.GetAllAsync();

        Assert.False(all[0].IsCompleted);
        Assert.True(all[1].IsCompleted);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByPriorityDescendingWithinGroup()
    {
        await _repository.AddAsync(NewItem("Low", TodoPriority.Low));
        await _repository.AddAsync(NewItem("High", TodoPriority.High));
        await _repository.AddAsync(NewItem("Normal", TodoPriority.Normal));

        var all = await _repository.GetAllAsync();

        Assert.Equal(TodoPriority.High, all[0].Priority);
        Assert.Equal(TodoPriority.Normal, all[1].Priority);
        Assert.Equal(TodoPriority.Low, all[2].Priority);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var id = await _repository.AddAsync(NewItem("Draft"));

        var item = await _repository.GetByIdAsync(id);
        item!.Title = "Final";
        item.IsCompleted = true;
        item.Priority = TodoPriority.Low;
        item.Notes = "updated";

        var updated = await _repository.UpdateAsync(item);

        Assert.True(updated);
        var reloaded = await _repository.GetByIdAsync(id);
        Assert.Equal("Final", reloaded!.Title);
        Assert.True(reloaded.IsCompleted);
        Assert.Equal(TodoPriority.Low, reloaded.Priority);
        Assert.Equal("updated", reloaded.Notes);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsFalse()
    {
        var item = NewItem();
        item.Id = 9999;

        Assert.False(await _repository.UpdateAsync(item));
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var id = await _repository.AddAsync(NewItem());

        var deleted = await _repository.DeleteAsync(id);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        Assert.False(await _repository.DeleteAsync(42));
    }

    [Fact]
    public async Task DeleteCompletedAsync_RemovesOnlyCompletedItems()
    {
        await _repository.AddAsync(NewItem("Keep 1"));
        await _repository.AddAsync(NewItem("Remove", completed: true));
        await _repository.AddAsync(NewItem("Keep 2"));
        await _repository.AddAsync(NewItem("Remove too", completed: true));

        var removed = await _repository.DeleteCompletedAsync();
        var remaining = await _repository.GetAllAsync();

        Assert.Equal(2, removed);
        Assert.Equal(2, remaining.Count);
        Assert.All(remaining, item => Assert.False(item.IsCompleted));
    }

    [Fact]
    public async Task Data_PersistsAcrossConnections()
    {
        var id = await _repository.AddAsync(NewItem("Survives"));

        // A brand new repository over the same database must see the row.
        var secondRepository = new SqliteTodoRepository(_database);
        var loaded = await secondRepository.GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("Survives", loaded!.Title);
    }
}
