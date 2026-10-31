using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSqlite.Core.Data;
using MauiSqlite.Core.Models;

namespace MauiSqlite.Core.ViewModels;

/// <summary>
/// Screen model for the to-do list: load, add, toggle, and delete tasks.
/// </summary>
public partial class TodoListViewModel : ObservableObject
{
    private readonly ITodoRepository _repository;

    public TodoListViewModel(ITodoRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public ObservableCollection<TodoItem> Items { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private string _newTitle = string.Empty;

    [ObservableProperty]
    private string? _newNotes;

    [ObservableProperty]
    private TodoPriority _newPriority = TodoPriority.Normal;

    [ObservableProperty]
    private DateTime? _newDueDate;

    [ObservableProperty]
    private bool _isBusy;

    public IReadOnlyList<TodoPriority> Priorities { get; } =
        new[] { TodoPriority.Low, TodoPriority.Normal, TodoPriority.High };

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var items = await _repository.GetAllAsync().ConfigureAwait(true);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        if (!CanAdd())
        {
            return;
        }

        var item = new TodoItem
        {
            Title = NewTitle.Trim(),
            Notes = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes.Trim(),
            Priority = NewPriority,
            DueDateUtc = NewDueDate?.ToUniversalTime(),
            CreatedUtc = DateTime.UtcNow,
        };

        await _repository.AddAsync(item).ConfigureAwait(true);

        Items.Insert(0, item);
        ResetEditor();
    }

    private bool CanAdd() => !string.IsNullOrWhiteSpace(NewTitle);

    [RelayCommand]
    private async Task ToggleAsync(TodoItem? item)
    {
        if (item is null)
        {
            return;
        }

        item.IsCompleted = !item.IsCompleted;
        await _repository.UpdateAsync(item).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task DeleteAsync(TodoItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _repository.DeleteAsync(item.Id).ConfigureAwait(true);
        Items.Remove(item);
    }

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        await _repository.DeleteCompletedAsync().ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    private void ResetEditor()
    {
        NewTitle = string.Empty;
        NewNotes = null;
        NewPriority = TodoPriority.Normal;
        NewDueDate = null;
    }
}
