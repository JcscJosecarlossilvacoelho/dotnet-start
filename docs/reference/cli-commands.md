---
title: Command reference
description: The dotnet commands worth memorising, grouped by what you are trying to do.
order: 20
---

## Create

```bash
dotnet new list                            # every installed template
dotnet new webapi -n MyApp                 # HTTP API
dotnet new blazor -n MyApp                 # Blazor Web App
dotnet new worker -n MyApp.Worker          # background service
dotnet new classlib -n MyApp.Domain
dotnet new xunit -n MyApp.Tests
dotnet new sln --format slnx -n MyApp
dotnet new gitignore
dotnet new globaljson --sdk-version 10.0.100
dotnet new tool-manifest
```

## Build and run

```bash
dotnet restore                             # explicit; implicit in build/run/test
dotnet build -c Release
dotnet build -v n                          # normal verbosity, for diagnosing failures
dotnet build -bl                           # binary log for the structured log viewer
dotnet run --project src/MyApp.Api
dotnet run --launch-profile https
dotnet watch                               # rebuild and hot reload on change
dotnet clean
```

## Test

```bash
dotnet test
dotnet test --filter "FullyQualifiedName~Orders"
dotnet test --logger "trx;LogFileName=results.trx"
dotnet test --collect:"XPlat Code Coverage"
dotnet test --blame-hang-timeout 60s       # find which test hangs
```

## Packages

```bash
dotnet add package Serilog
dotnet add package Serilog --version 4.0.0
dotnet remove package Serilog
dotnet list package --outdated
dotnet list package --vulnerable --include-transitive
dotnet list package --deprecated
dotnet nuget why MyApp.csproj System.Text.Json
dotnet nuget locals all --clear            # nuclear option for cache problems
```

## Projects and references

```bash
dotnet sln add src/MyApp.Api/MyApp.Api.csproj
dotnet sln list
dotnet add src/MyApp.Api reference src/MyApp.Domain
dotnet list src/MyApp.Api reference
```

## Publish

```bash
dotnet publish -c Release
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
dotnet publish -c Release /t:PublishContainer
dotnet pack -c Release
dotnet nuget push bin/Release/*.nupkg --source nuget.org --api-key $NUGET_KEY
```

## Tools

```bash
dotnet tool install dotnet-ef              # local (needs a tool manifest)
dotnet tool install -g dotnet-counters     # global
dotnet tool restore
dotnet tool list --local
dotnet format
dotnet format --verify-no-changes          # CI check
```

## Entity Framework

```bash
dotnet ef migrations add AddOrderStatus
dotnet ef migrations list
dotnet ef migrations script --idempotent -o migrate.sql
dotnet ef migrations bundle --self-contained -r linux-x64
dotnet ef database update
dotnet ef dbcontext info
dotnet ef dbcontext scaffold "<connection>" Npgsql.EntityFrameworkCore.PostgreSQL
```

## Diagnostics

```bash
dotnet-counters monitor -p <pid> System.Runtime
dotnet-trace collect -p <pid> --profile cpu-sampling
dotnet-dump collect -p <pid>
dotnet-gcdump collect -p <pid>
dotnet-stack report -p <pid>
```

See [Diagnosing a running application](/docs/runtime/diagnostics).
