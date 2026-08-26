# ADR-010: The persistence port exposes DbSets, not repositories

**Status:** Accepted · 2026-08-26

## Context

The Application layer needs to read and write data without depending on the
Infrastructure project. The textbook answer is a repository interface per
aggregate. The pragmatic answer is to expose the `DbContext` behind an interface.

The deciding factor is what the read side looks like. Dashboard and analytics
queries project directly to DTOs, filter on several columns, paginate by keyset
and aggregate. Expressing those through repository methods produces either a
combinatorial explosion of `GetByXAndYOrderedByZ` methods, or a repository that
returns `IQueryable` — which is the DbContext again, with an extra layer.

## Decision

`IAppDbContext` in `Application/Abstractions/Persistence` exposes
`DbSet<TEntity>` plus `SaveChangesAsync`. `AppDbContext` in Infrastructure
implements it.

Application therefore references `Microsoft.EntityFrameworkCore` but **never a
provider**. Knowing about EF Core is a deliberate coupling; knowing about
PostgreSQL would make the database impossible to change without editing use
cases. An architecture test enforces the distinction.

Repositories are not banned — they are simply not created pre-emptively. If an
aggregate develops loading rules complex enough to justify one, it gets one.

## Consequences

**Good.** No boilerplate that only forwards calls. Read handlers use the full
expressiveness of LINQ. Change tracking works normally for writes, so the domain
state machines behave as designed.

**Costs.** Application can write an inefficient query, and nothing structurally
prevents it — this trades a compile-time guardrail for review discipline and the
integration tests. It is also harder to unit test a handler in isolation; the
answer is to test handlers against a real database (Testcontainers) rather than
against a fake `IQueryable`, which is more honest anyway.

## Alternatives considered

- **Repository per aggregate.** Rejected for now: real cost today, speculative
  benefit. Can be introduced per aggregate later without a rewrite.
- **Generic `IRepository<T>`.** Rejected: it is `DbSet<T>` with fewer features
  and a different name.
- **Application-side specifications.** Rejected as premature; it solves query
  reuse, which we do not yet have enough queries to need.
