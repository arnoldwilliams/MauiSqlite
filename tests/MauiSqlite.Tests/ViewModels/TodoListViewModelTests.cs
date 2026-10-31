using MauiSqlite.Core.Data;
using MauiSqlite.Core.Models;
using MauiSqlite.Core.ViewModels;
using Moq;

namespace MauiSqlite.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="TodoListViewModel"/> using a mocked repository.
/// </summary>
public class TodoListViewModelTests
{
    private readonly Mock<ITodoRepository> _repository = new(MockBehavior.Strict);

    private TodoListViewModel CreateSut() => new(_repository.Object);

    [Fact]
    public void Constructor_RejectsNullRepository()
    {
        Assert.Throws<ArgumentNullException>(() => new TodoListViewModel(null!));
    }

    [Fact]
    public async Task LoadAsync_PopulatesItemsFromRepository()
    {
        var stored = new List<TodoItem>
        {
            new() { Id = 1, Title = "First" },
            new() { Id = 2, Title = "Second" },
        };

        _repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var sut = CreateSut();
        await sut.LoadAsync();

        Assert.Equal(2, sut.Items.Count);
        Assert.Equal("First", sut.Items[0].Title);
        Assert.Equal("Second", sut.Items[1].Title);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_ReplacesExistingItems()
    {
        _repository
            .SetupSequence(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TodoItem> { new() { Id = 1, Title = "Old" } })
            .ReturnsAsync(new List<TodoItem> { new() { Id = 2, Title = "New" } });

        var sut = CreateSut();
        await sut.LoadAsync();
        await sut.LoadAsync();

        Assert.Single(sut.Items);
        Assert.Equal("New", sut.Items[0].Title);
    }

    [Fact]
    public void AddCommand_DisabledWhenTitleIsBlank()
    {
        var sut = CreateSut();

        Assert.False(sut.AddCommand.CanExecute(null));

        sut.NewTitle = "   ";

        Assert.False(sut.AddCommand.CanExecute(null));

        sut.NewTitle = "Real task";

        Assert.True(sut.AddCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddAsync_TrimsInput_PersistsAndInsertsAtTop()
    {
        TodoItem? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()))
            .Callback<TodoItem, CancellationToken>((item, _) =>
            {
                // Mirror the real repository, which assigns the generated key.
                item.Id = 7;
                captured = item;
            })
            .ReturnsAsync(7);

        var sut = CreateSut();
        sut.NewTitle = "  Water plants  ";
        sut.NewNotes = "  front and back  ";
        sut.NewPriority = TodoPriority.High;

        await sut.AddCommand.ExecuteAsync(null);

        Assert.NotNull(captured);
        Assert.Equal("Water plants", captured!.Title);
        Assert.Equal("front and back", captured.Notes);
        Assert.Equal(TodoPriority.High, captured.Priority);
        Assert.Equal(7, captured.Id);
        Assert.Single(sut.Items);
        Assert.Equal("Water plants", sut.Items[0].Title);
    }

    [Fact]
    public async Task AddAsync_ClearsEditorAfterSuccess()
    {
        _repository
            .Setup(r => r.AddAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();
        sut.NewTitle = "Task";
        sut.NewNotes = "note";
        sut.NewPriority = TodoPriority.Low;

        await sut.AddCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, sut.NewTitle);
        Assert.Null(sut.NewNotes);
        Assert.Equal(TodoPriority.Normal, sut.NewPriority);
        Assert.Null(sut.NewDueDate);
    }

    [Fact]
    public async Task AddAsync_BlankNotesAreStoredAsNull()
    {
        TodoItem? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()))
            .Callback<TodoItem, CancellationToken>((item, _) => captured = item)
            .ReturnsAsync(1);

        var sut = CreateSut();
        sut.NewTitle = "Task";
        sut.NewNotes = "   ";

        await sut.AddCommand.ExecuteAsync(null);

        Assert.Null(captured!.Notes);
    }

    [Fact]
    public async Task AddAsync_DoesNotPersistWhenTitleIsBlank()
    {
        var sut = CreateSut();
        sut.NewTitle = "   ";

        await sut.AddCommand.ExecuteAsync(null);

        _repository.Verify(
            r => r.AddAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ToggleAsync_FlipsCompletionPersistsAndReloads()
    {
        var item = new TodoItem { Id = 3, Title = "Toggle me", IsCompleted = false };

        _repository
            .Setup(r => r.UpdateAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TodoItem> { item });

        var sut = CreateSut();
        await sut.ToggleCommand.ExecuteAsync(item);

        Assert.True(item.IsCompleted);
        _repository.Verify(
            r => r.UpdateAsync(It.Is<TodoItem>(i => i.IsCompleted), It.IsAny<CancellationToken>()),
            Times.Once);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleAsync_NullItem_DoesNothing()
    {
        var sut = CreateSut();

        await sut.ToggleCommand.ExecuteAsync(null);

        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_RemovesItemFromRepositoryAndCollection()
    {
        var item = new TodoItem { Id = 5, Title = "Delete me" };

        _repository
            .Setup(r => r.DeleteAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        sut.Items.Add(item);

        await sut.DeleteCommand.ExecuteAsync(item);

        Assert.Empty(sut.Items);
        _repository.Verify(r => r.DeleteAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NullItem_DoesNothing()
    {
        var sut = CreateSut();

        await sut.DeleteCommand.ExecuteAsync(null);

        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ClearCompletedAsync_DeletesThenReloads()
    {
        _repository
            .Setup(r => r.DeleteCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TodoItem>());

        var sut = CreateSut();
        await sut.ClearCompletedCommand.ExecuteAsync(null);

        _repository.Verify(r => r.DeleteCompletedAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Priorities_ExposesAllValues()
    {
        var sut = CreateSut();

        Assert.Equal(
            new[] { TodoPriority.Low, TodoPriority.Normal, TodoPriority.High },
            sut.Priorities);
    }
}
