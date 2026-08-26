# AI-Powered Code Review Assistant

A code review platform that analyses GitHub pull requests with a combination of
deterministic static analysis and LLM-based review, producing structured,
explainable findings and a reproducible risk score.

> **Status: in active development.** The architecture, guardrails and database
> are in place; the review pipeline is being built milestone by milestone — see
> [Roadmap](#roadmap). Screenshots replace this note once the review screen lands.

## Why this is not a chatbot wrapper

The interesting engineering is deliberately *not* the model call:

- **The model proposes findings; the system prices them.** Risk scores are
  computed by a pure, unit-tested function in the Domain, never by the LLM
  ([ADR-008](docs/adr/008-deterministic-risk-scoring.md)).
- **Model output is untrusted until validated.** Every finding is checked
  against the real diff — the file must exist in the pull request, the line
  numbers must fall inside changed hunks — before it can be stored.
- **Repository code is data, never instruction.** Prompts separate system
  instructions from untrusted repository content structurally, and the only
  channel back from the model is a JSON schema.
- **Untrusted code is never compiled or executed.** Package restore and Roslyn
  source generators both run attacker-controlled code; we parse instead
  ([ADR-005](docs/adr/005-never-compile-untrusted-repository-code.md)).

## Architecture

```
Domain          -> (nothing)
Application     -> Domain
Infrastructure  -> Application, Domain
Api / Worker    -> Application, Infrastructure   (composition root only)
```

Enforced by tests, not convention — see `tests/AiCodeReview.ArchitectureTests`.
Detail in [docs/architecture/overview.md](docs/architecture/overview.md).

| Project | Responsibility |
|---------|----------------|
| `AiCodeReview.Domain` | Entities, state machines, risk scoring, `Result`/`Error`. Zero package references. |
| `AiCodeReview.Application` | Use cases and the ports Infrastructure implements. |
| `AiCodeReview.Infrastructure` | EF Core, PostgreSQL, GitHub adapter, job queue, token protection. |
| `AiCodeReview.Api` | REST endpoints, authentication, ProblemDetails. |
| `AiCodeReview.Worker` | Background review pipeline. |

## Database

Nine tables in PostgreSQL 17 with EF Core migrations. Design notes, the ERD and
the reasoning behind each index are in
[docs/architecture/database.md](docs/architecture/database.md).

Two invariants live in the schema rather than in C#:

- A **partial unique index** makes two in-flight reviews of the same commit
  impossible, so a double-clicked button cannot buy two sets of model calls.
- A **partial index on pending jobs** keeps the dispatcher's constant polling
  query cheap however much completed history accumulates.

## Technology

.NET 10 · ASP.NET Core Minimal APIs · EF Core 10 · PostgreSQL 17 · Angular ·
Serilog · xUnit v3 · Testcontainers · NetArchTest · Docker

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) (version
pinned in `global.json`) and Docker.

Start the database and apply migrations:

```bash
docker compose up -d postgres
```

```bash
dotnet tool restore
```

```bash
dotnet dotnet-ef database update --project src/AiCodeReview.Infrastructure --startup-project src/AiCodeReview.Infrastructure
```

Run the API:

```bash
dotnet run --project src/AiCodeReview.Api --urls http://localhost:5080
```

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Liveness — is the process up? Checks no dependencies by design. |
| `GET /health/ready` | Readiness — includes a database check. |
| `GET /scalar/v1` | Interactive API reference (Development only). |
| `GET /openapi/v1.json` | OpenAPI document (Development only). |

Run the Worker:

```bash
dotnet run --project src/AiCodeReview.Worker
```

Full application containers arrive in M10; until then the two hosts run from the
SDK against the Compose database.

## Testing

```bash
dotnet test --solution AiCodeReview.slnx
```

| Suite | Asserts |
|-------|---------|
| `AiCodeReview.UnitTests` | Domain behaviour — `Result` invariants, review and job state machines. |
| `AiCodeReview.IntegrationTests` | Schema invariants against real PostgreSQL in a throwaway container. |
| `AiCodeReview.ArchitectureTests` | The dependency graph matches the ADRs. |

The EF in-memory provider is deliberately unused: it enforces no unique index,
no partial index and no foreign key, so it cannot verify the invariants this
schema encodes. Integration tests **skip with an explanation** when Docker is
unavailable rather than failing.

## Architecture Decision Records

Every significant decision is recorded with its context, cost and the
alternatives rejected — [docs/adr](docs/adr/README.md).

| ADR | Decision |
|-----|----------|
| [001](docs/adr/001-modular-monolith-with-separate-worker.md) | Modular monolith with a separate worker process |
| [002](docs/adr/002-postgres-backed-job-queue.md) | PostgreSQL-backed job queue instead of a broker |
| [003](docs/adr/003-result-pattern-for-expected-failures.md) | Result pattern for expected failures |
| [004](docs/adr/004-cookie-sessions-not-browser-jwt.md) | Cookie sessions instead of browser-held JWTs |
| [005](docs/adr/005-never-compile-untrusted-repository-code.md) | Never compile or execute untrusted repository code |
| [006](docs/adr/006-github-as-sole-identity-provider.md) | GitHub as the sole identity provider |
| [007](docs/adr/007-cqrs-without-a-mediator-library.md) | CQRS without a mediator library |
| [008](docs/adr/008-deterministic-risk-scoring.md) | Deterministic risk scoring; the model never sets the score |
| [009](docs/adr/009-review-run-is-the-review.md) | A review run is the review; no separate review aggregate |
| [010](docs/adr/010-persistence-port-exposes-dbsets.md) | The persistence port exposes DbSets, not repositories |

## Roadmap

Built as a thin vertical slice first, then thickened one layer at a time.

| Milestone | Scope | Status |
|-----------|-------|--------|
| M0 | Solution, layering guardrails, `Result`, ProblemDetails, health, ADRs | ✅ Done |
| M1 | PostgreSQL schema, EF Core, migrations, Testcontainers | ✅ Done |
| M2 | GitHub OAuth sign-in, encrypted token storage | Next |
| M3 | GitHub repository / pull request / diff ingestion |  |
| M4 | Job queue and Worker pipeline (no AI yet) |  |
| M5 | First real AI review with schema-constrained output |  |
| M6 | Finding validation and deterministic risk scoring |  |
| M7 | Angular shell — repositories and pull requests |  |
| M8 | The review screen |  |
| M9 | Finding statuses, review history, rerun |  |
| M10 | Docker Compose, CI, documentation — **v1.0** |  |

Deferred by design, each with a seam already in place: static analysis, large-PR
chunking, a second AI provider, RAG-based false-positive suppression, bounded
agentic tool calling, analytics, GitHub comment publishing, cloud deployment.

## Security

Security notes live alongside the code they describe. The load-bearing
decisions so far:

- Exception detail is logged server-side and never returned to a caller.
- Inbound correlation identifiers are validated before they reach a log.
- GitHub tokens are stored encrypted and never appear in any response model.
- Repository content is never compiled, executed, shell-interpolated, or used
  to build a file path or URL without validation.
- No secret is committed. Local development uses the Compose database
  credentials; every other environment supplies `ConnectionStrings__AppDb`
  through the platform.
