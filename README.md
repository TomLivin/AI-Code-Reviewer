# AI-Powered Code Review Assistant

A code review platform that analyses GitHub pull requests with a combination of
deterministic static analysis and LLM-based review, producing structured,
explainable findings and a reproducible risk score.

> **Status: in active development.** The architecture, guardrails and both hosts
> are in place. The review pipeline is being built milestone by milestone — see
> [Roadmap](#roadmap). This section will be replaced with screenshots once the
> review screen lands.

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

These rules are enforced by tests, not convention — see
`tests/AiCodeReview.ArchitectureTests`. Full detail in
[docs/architecture/overview.md](docs/architecture/overview.md).

| Project | Responsibility |
|---------|----------------|
| `AiCodeReview.Domain` | Entities, risk scoring, `Result`/`Error`. Zero dependencies. |
| `AiCodeReview.Application` | Use cases and the ports that Infrastructure implements. |
| `AiCodeReview.Infrastructure` | EF Core, GitHub adapter, job queue, token protection. |
| `AiCodeReview.Api` | REST endpoints, authentication, ProblemDetails. |
| `AiCodeReview.Worker` | Background review pipeline. |

## Technology

.NET 10 · ASP.NET Core Minimal APIs · EF Core · PostgreSQL · Angular ·
Serilog · xUnit v3 · NetArchTest · Docker

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) (the exact
version is pinned in `global.json`).

```bash
dotnet restore
dotnet build
dotnet test
```

Run the API:

```bash
dotnet run --project src/AiCodeReview.Api --urls http://localhost:5080
```

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Liveness — is the process up? Checks no dependencies by design. |
| `GET /health/ready` | Readiness — can this instance serve traffic? |
| `GET /scalar/v1` | Interactive API reference (Development only). |
| `GET /openapi/v1.json` | OpenAPI document (Development only). |

Run the Worker:

```bash
dotnet run --project src/AiCodeReview.Worker
```

Docker Compose arrives in M10; until then the two hosts run directly.

## Testing

```bash
dotnet test
```

| Suite | Asserts |
|-------|---------|
| `AiCodeReview.UnitTests` | Domain behaviour — `Result` invariants, entity identity. |
| `AiCodeReview.ArchitectureTests` | The dependency graph matches the ADRs. |

Integration tests against a real PostgreSQL container arrive in M1. The
in-memory EF provider is deliberately not used: it does not enforce the
constraints that encode our invariants.

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

## Roadmap

Built as a thin vertical slice first, then thickened one layer at a time.

| Milestone | Scope | Status |
|-----------|-------|--------|
| M0 | Solution, layering guardrails, `Result`, ProblemDetails, health, ADRs | ✅ Done |
| M1 | PostgreSQL schema, EF Core, migrations, Testcontainers | Next |
| M2 | GitHub OAuth sign-in, encrypted token storage |  |
| M3 | GitHub repository / pull request / diff ingestion |  |
| M4 | Job queue and Worker pipeline (no AI yet) |  |
| M5 | First real AI review with schema-constrained output |  |
| M6 | Finding validation and deterministic risk scoring |  |
| M7 | Angular shell — repositories and pull requests |  |
| M8 | The review screen |  |
| M9 | Finding statuses, review history, rerun |  |
| M10 | Docker Compose, CI, documentation — **v1.0** |  |

Deferred by design, each with a seam already in place: static analysis,
large-PR chunking, a second AI provider, RAG-based false-positive suppression,
bounded agentic tool calling, analytics, GitHub comment publishing, cloud
deployment.

## Security

Security notes live alongside the code they describe. The load-bearing
decisions so far:

- Exception detail is logged server-side and never returned to a caller.
- Inbound correlation identifiers are validated before they reach a log.
- Repository content is never compiled, executed, shell-interpolated, or used
  to build a file path or URL without validation.
- No secret is committed. Configuration comes from user secrets locally and
  environment variables or a platform vault elsewhere.
