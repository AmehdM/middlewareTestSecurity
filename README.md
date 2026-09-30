# MiddlewareDemo

Minimal API on .NET 8 with a custom API-key middleware, for scanner testing.

- Set the key first: `$env:Security__ApiKey = "<your-secret>"` (PowerShell) or `export Security__ApiKey=...`.
- Optional: `Cors__AllowedOrigin` (default `https://localhost:3000`).
- `dotnet restore` then `dotnet test`.
- `dotnet run --project src/MiddlewareDemo.Api`; send the key in the `X-Api-Key` header.
- `/health` needs no key. `/api/items` (GET, POST) does.
- The app refuses to start without `Security:ApiKey`.
