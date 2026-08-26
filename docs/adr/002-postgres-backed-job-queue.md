# ADR-002: PostgreSQL-backed job queue instead of a message broker

**Status:** Accepted · 2026-08-26

## Context

The API must hand review work to the Worker durably: a job accepted with `202`
has to survive a process restart. We already depend on PostgreSQL for the domain
data.

## Decision

A `background_jobs` table, claimed with `SELECT ... FOR UPDATE SKIP LOCKED`,
behind an `IJobQueue` abstraction owned by the Application layer.

The job row and the domain row it refers to are inserted **in the same
transaction**. This is the transactional outbox pattern: either both exist or
neither does, so there are no lost jobs and no jobs pointing at rows that were
rolled back.

Claiming sets a lease (`locked_by`, `locked_until`) rather than deleting the
row, so a Worker that crashes mid-job has its work reclaimed when the lease
expires.

## Consequences

**Good.** No extra infrastructure to run, install or explain. Job state is
queryable next to domain state, so "why is this review stuck?" is one SQL join
rather than a broker UI. Exactly-once claiming without a distributed lock.

**Costs.** Polling adds latency (bounded by the poll interval) and load. It will
not carry tens of thousands of jobs per second — far beyond anything this system
needs. Retry, backoff and dead-lettering are ours to write and to test.

**Migration path.** `IJobQueue` is the seam. Moving to SQS, Service Bus or
RabbitMQ is a new adapter plus a DI registration change. Nothing in Application
or Domain changes.

## Alternatives considered

- **Hangfire.** A good dashboard, but its storage schema is opaque to our
  queries and its licensing is now dual. We would still write the pipeline.
- **RabbitMQ / Redis.** Correct at a scale we do not have, and each adds a
  service to compose, a failure mode, and a second source of truth for state.
