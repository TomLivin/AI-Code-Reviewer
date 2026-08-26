# ADR-007: CQRS without a mediator library

**Status:** Accepted · 2026-08-26

## Context

Reads and writes in this system have genuinely different shapes. Writing a
review run is an aggregate operation; the analytics and dashboard reads are flat
projections that would be slow and awkward through the same model. That is a
real argument for CQRS.

It is not, by itself, an argument for an in-process mediator library. MediatR is
the reflex choice and moved to a commercial licence in 2025.

## Decision

CQRS as a design idea, with hand-written dispatch:

```csharp
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
```

Endpoints inject the concrete handler interface directly. Cross-cutting concerns
(validation, logging, transaction scope) are applied with Scrutor decorators
registered once, rather than with runtime pipeline behaviours.

## Consequences

**Good.** Dispatch is verified at compile time — a missing handler is a build
error, not a runtime one. "Go to definition" on a handler interface lands on the
implementation. No licensing exposure. Roughly ten lines of infrastructure that
we own and can explain.

**Costs.** Registering handlers is explicit (mitigated by an assembly scan).
No free `IPipelineBehavior` — decorators do the same job with slightly more
ceremony per concern.

## Alternatives considered

- **MediatR.** Rejected on licensing plus the indirection cost, which is real
  for someone reading the codebase for the first time.
- **Wolverine.** Capable, but it wants to own messaging and hosting too, which
  conflicts with ADR-002.
- **No CQRS at all.** Rejected: the analytics read model genuinely does not fit
  the write model.
