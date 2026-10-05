# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

StrangeSeeker is an ASP.NET Core minimal API targeting **.NET 10** (`net10.0`), with nullable reference types and implicit usings enabled. The solution uses the XML-based `StrangeSeeker.slnx` format, so tooling needs a .NET SDK recent enough to read `.slnx`.

The project is at an early stage: one web project at `src/StrangeSeeker/` and no test project yet.

## Commands

Run these from the repo root:

```bash
dotnet build StrangeSeeker.slnx                                     # build
dotnet run --project src/StrangeSeeker                              # run (http profile, http://localhost:5243)
dotnet run --project src/StrangeSeeker --launch-profile https       # https://localhost:7103 + http://localhost:5243
dotnet watch --project src/StrangeSeeker                            # hot reload
dotnet format StrangeSeeker.slnx                                    # format / lint
```

Once a test project exists, run tests with `dotnet test StrangeSeeker.slnx`. To run a single test, add `--filter "FullyQualifiedName~<TestName>"`.

## Architecture

- `src/StrangeSeeker/Program.cs` is the whole app. It registers services, builds the pipeline (OpenAPI, then HTTPS redirection) and maps endpoints inline as minimal API lambdas, for example `GET /welcome`.
- The OpenAPI document comes from `Microsoft.AspNetCore.OpenApi`. It is served only in the Development environment, at `/openapi/v1.json`. Swagger UI is not configured.
- Both launch profiles set `ASPNETCORE_ENVIRONMENT=Development`.
- `StrangeSeeker.http` is meant for manual requests, but it still points at the template's `/weatherforecast` endpoint, which no longer exists. Update it whenever you add or change endpoints.
