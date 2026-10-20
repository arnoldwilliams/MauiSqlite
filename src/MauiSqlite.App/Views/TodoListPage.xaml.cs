using MauiSqlite.Core.ViewModels;

namespace MauiSqlite.App.Views;

public partial class TodoListPage : ContentPage
{
    private readonly TodoListViewModel _viewModel;

    public TodoListPage(TodoListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
