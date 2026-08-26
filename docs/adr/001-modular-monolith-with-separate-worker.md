# ADR-001: Modular monolith with a separate worker process

**Status:** Accepted · 2026-08-26

## Context

Reviewing a pull request takes 20–120 seconds: several GitHub round trips, then
one or more LLM calls. That work cannot happen inside an HTTP request. It must
survive a deployment, be retryable, and be observable.

At the same time this system has exactly one bounded context — reviewing code.
There is no independent business capability arguing to be its own service.

## Decision

One solution with two deployable entry points, `AiCodeReview.Api` and
`AiCodeReview.Worker`, sharing `Application`, `Infrastructure` and `Domain`.

Dependency direction is fixed and enforced by tests in
`AiCodeReview.ArchitectureTests`:

```
Domain          -> (nothing)
Application     -> Domain
Infrastructure  -> Application, Domain
Api / Worker    -> Application, Infrastructure   (composition root only)
```

The API and Worker never reference each other. They communicate through the job
queue (ADR-002), never directly.

## Consequences

**Good.** Review throughput scales independently of request throughput. A review
storm cannot degrade the UI. One shared schema means no distributed transactions.
`docker compose up` starts four containers, not twelve.

**Costs.** Two processes to run locally and two images to build. The Worker
cannot scale to zero on most platforms, which has a deployment cost recorded in
the deployment notes.

**Extraction path.** If a stage ever needs to scale independently, the seam is
`IJobQueue` — a new adapter, not a re-architecture. The process boundary is not
the seam; the port is.

## Alternatives considered

- **Everything in the API process.** Rejected: review work would compete with
  request handling, and an in-flight review would die on every deploy. Retained
  only as a configuration flag for the low-cost hosted demo.
- **Microservices.** Rejected: no independent bounded context, and it would add
  network failure modes, distributed tracing complexity and deployment overhead
  in exchange for nothing this system needs.
