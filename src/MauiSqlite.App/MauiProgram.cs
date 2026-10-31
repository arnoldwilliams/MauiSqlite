using Microsoft.Extensions.Logging;
using MauiSqlite.Core.ViewModels;
using MauiSqlite.App.Views;
using MauiSqlite.Core.Data;

namespace MauiSqlite.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "todos.db3");
        builder.Services.AddSingleton(new TodoDatabase(databasePath));
        builder.Services.AddSingleton<ITodoRepository, SqliteTodoRepository>();

        builder.Services.AddTransient<TodoListViewModel>();
        builder.Services.AddTransient<TodoListPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
