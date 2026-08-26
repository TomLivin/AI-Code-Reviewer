# Architecture Decision Records

Each record captures one decision, the context that forced it, and what it costs
us. They are immutable: a decision that changes gets a new record that supersedes
the old one, so the reasoning history stays intact.

| ADR | Decision | Status |
|-----|----------|--------|
| [001](001-modular-monolith-with-separate-worker.md) | Modular monolith with a separate worker process | Accepted |
| [002](002-postgres-backed-job-queue.md) | PostgreSQL-backed job queue instead of a broker | Accepted |
| [003](003-result-pattern-for-expected-failures.md) | Result pattern for expected failures | Accepted |
| [004](004-cookie-sessions-not-browser-jwt.md) | Cookie sessions instead of browser-held JWTs | Accepted |
| [005](005-never-compile-untrusted-repository-code.md) | Never compile or execute untrusted repository code | Accepted |
| [006](006-github-as-sole-identity-provider.md) | GitHub as the sole identity provider | Accepted |
| [007](007-cqrs-without-a-mediator-library.md) | CQRS without a mediator library | Accepted |
| [008](008-deterministic-risk-scoring.md) | Deterministic risk scoring; the model never sets the score | Accepted |
| [009](009-review-run-is-the-review.md) | A review run is the review; no separate review aggregate | Accepted |
| [010](010-persistence-port-exposes-dbsets.md) | The persistence port exposes DbSets, not repositories | Accepted |
