# EventsPlatform

EventsPlatform is an events platform built with a .NET (net10.0) ASP.NET Core API, PostgreSQL via EF Core, and a React/TypeScript/Vite client.

## Main parts

- `Domain` – the `Event` entity.
- `Application` – placeholder only.
- `Persistence` – `AppDbContext`, migrations, and `DbInitializer` seeding.
- `API` – controllers (`EventsController`: GET list and GET by id under `api/events`); migrates and seeds the database on startup.
- `client/` – React/Vite client that fetches `/api/events`.
- `tests/EventsPlatform.Tests` – xUnit tests.
- `docs/` – architecture and setup notes, starting at [docs/README.md](docs/README.md).

## Running tests

Requires the .NET 10 SDK. From the repo root:

```bash
dotnet test
```

This runs the xUnit project in `tests/EventsPlatform.Tests` via `EventsPlatform-DotNet-React.slnx`. To run only that project: `dotnet test tests/EventsPlatform.Tests`.
