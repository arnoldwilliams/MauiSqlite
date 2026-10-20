# MauiSqlite

A cross-platform to-do app built with **.NET 10 MAUI** (C#) that stores and
retrieves its data with **Microsoft.Data.Sqlite**. It targets **Android** and
**iOS**, and ships with an **xUnit + Moq** unit test project.

## Projects

| Project | Target | Purpose |
| --- | --- | --- |
| `src/MauiSqlite.Core` | `net10.0` | Domain model, SQLite data access, and view models. No MAUI dependency, so it is fully unit testable. |
| `src/MauiSqlite.App` | `net10.0-android`, `net10.0-ios` | MAUI UI: XAML pages, converters, and dependency injection. |
| `tests/MauiSqlite.Tests` | `net10.0` | xUnit tests for the repository (real in-memory SQLite) and the view model (Moq). |

## Features

- Add a task with a title, optional notes, a priority (Low/Normal/High), and an optional due date.
- Mark tasks complete/incomplete, delete a single task, or clear all completed tasks.
- Pull-to-refresh and an empty state.
- Tasks are ordered with incomplete items first, then by priority and recency.

## Data layer

`TodoDatabase` owns the SQLite connection string and creates connections with
the schema applied (`CREATE TABLE IF NOT EXISTS`). `SqliteTodoRepository`
implements `ITodoRepository` with parameterized SQL only - no string
concatenation of user input. On device the database file lives in
`FileSystem.AppDataDirectory`.

## Build and test

```bash
# Run the unit tests
dotnet test tests/MauiSqlite.Tests/MauiSqlite.Tests.csproj

# Build the Android app (requires the Android SDK + JDK)
dotnet build src/MauiSqlite.App/MauiSqlite.App.csproj -f net10.0-android

# Build for iOS (requires macOS with Xcode)
dotnet build src/MauiSqlite.App/MauiSqlite.App.csproj -f net10.0-ios
```

### Prerequisites

```bash
dotnet workload install maui-android   # add maui-ios on macOS
```

Building the iOS target requires macOS with Xcode; the Android target builds on
Windows, macOS, and Linux.

## Architecture notes

- The view model lives in the shared Core library and depends only on the
  `ITodoRepository` interface, which keeps it testable with Moq.
- The SQLite repository is covered by tests that run against an isolated
  in-memory database, so the real SQL is exercised without touching disk.
