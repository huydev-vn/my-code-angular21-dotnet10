# Backend capabilities catalog

Package versions live in `Directory.Packages.props`. A version listed there is
not the same as a capability being enabled. Add a `PackageReference` only when
the feature is implemented, configured, and tested.

| Capability | Packages | Project | When to enable | Trade-off | Enable with |
|---|---|---|---|---|---|
| PostgreSQL + EF Core | `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design` | Infrastructure | Default persistence | ORM overhead vs productivity | Already referenced |
| ASP.NET Core Identity | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Infrastructure | Self-hosted users/roles | Identity schema + conventions | Already referenced |
| JWT bearer | `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.IdentityModel.JsonWebTokens` | Api + Infrastructure | API access tokens | Key/rotation/claim design required | Already referenced |
| Validation | `FluentValidation`, `FluentValidation.DependencyInjectionExtensions` | Application | Request validation | Extra types per use case | Already referenced |
| Dapper | `Dapper` | Infrastructure | Profiled SQL that EF cannot express well | Dual data access styles | `dotnet add Infrastructure package Dapper` |
| Mapster | `Mapster`, `Mapster.DependencyInjection` | Infrastructure or Api | Repeated DTO mapping | Hidden mapping rules | `dotnet add Infrastructure package Mapster` |
| Serilog | `Serilog.AspNetCore`, `Serilog.Sinks.Console` | Api | Structured logs, enrichers, correlation | Replaces default providers | `dotnet add api package Serilog.AspNetCore` |
| File logs | `Serilog.Sinks.File` | Api | Local troubleshooting only | Not ideal in containers | `dotnet add api package Serilog.Sinks.File` |
| Bogus | `Bogus` | Test/seed project | Fake data in tests | Never reference from runtime | `dotnet add Tests package Bogus` |
| MediatR / CQRS | `MediatR` | Application | Many use cases need a dispatcher | Not required by Clean Architecture | Do not add until orchestration pain is real |
| Redis | `Microsoft.Extensions.Caching.StackExchangeRedis` | Infrastructure | Distributed cache, lock, or rate-limit | Invalidation and failure modes | `dotnet add Infrastructure package Microsoft.Extensions.Caching.StackExchangeRedis` |
| Background jobs | `Hangfire.AspNetCore`, `Hangfire.PostgreSql` | Api + Infrastructure | Durable jobs with retry/dashboard | Extra storage and ops | `dotnet add api package Hangfire.AspNetCore` |

Do not add `Microsoft.EntityFrameworkCore.Tools` to a project. This repository
uses the local `dotnet-ef` tool in `dotnet-tools.json`.
