# ADR-003: Result pattern for expected failures

**Status:** Accepted · 2026-08-26

## Context

Most failures in this system are expected, not exceptional: a pull request that
does not exist, a GitHub token that expired, an AI response that failed schema
validation, a review already running for the same commit. Modelling those as
exceptions makes them invisible in a method signature and makes control flow
depend on stack unwinding.

## Decision

`Application` methods return `Result` or `Result<T>` (`AiCodeReview.Domain.Common`).
A failure carries an `Error` with a stable machine-readable `Code`, a
caller-safe `Message`, and an `ErrorType` that classifies it without any
knowledge of HTTP.

Exceptions are reserved for programming defects and genuine infrastructure
faults. Those are caught by `GlobalExceptionHandler` and returned as an opaque
500 — the exception message is logged but never sent to the caller, because
exception text routinely contains connection strings, file paths and SQL.

`ErrorType` is translated to a status code in exactly one place,
`ResultExtensions.ToProblem`, so no endpoint invents its own mapping.

## Consequences

**Good.** Failure modes are visible in signatures. Error codes are stable enough
for the Angular client to branch on. Testing an error path needs no exception
plumbing. The status-code mapping cannot drift between endpoints.

**Costs.** More verbose than throwing. `Result` must be threaded through call
chains. Callers can ignore a returned `Result` — the compiler will not stop
them — so review discipline matters.

## Alternatives considered

- **Exceptions everywhere.** Rejected: expected outcomes should not cost a stack
  unwind, and control flow becomes invisible at the call site.
- **A library such as OneOf or LanguageExt.** Rejected for now: `Result` is
  roughly eighty lines we fully control, and a functional-programming dependency
  raises the bar for anyone reading the codebase.
