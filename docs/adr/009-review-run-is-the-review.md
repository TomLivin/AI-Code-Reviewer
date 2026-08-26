# ADR-009: A review run is the review

**Status:** Accepted · 2026-08-26

## Context

The original entity sketch had both a `CodeReview` and a `ReviewRun`, on the
assumption that a review is a long-lived thing which can be executed repeatedly.

Working through the behaviour, `CodeReview` had no state of its own. Every field
that mattered — status, risk score, summary, findings, duration, error — belongs
to one execution against one commit. A parent that always owns exactly one
active child and holds nothing itself is a join table with extra steps.

## Decision

`ReviewRun` is the only review entity. It is scoped to one pull request at one
`head_sha`.

- Reviewing again after a push creates a **new run**, so history is simply the
  ordered set of runs for a pull request.
- Findings hang off the run that produced them.
- Finding *status* (accepted, dismissed, false positive) carries forward across
  runs by fingerprint rather than by parent identity, which is what users
  actually want: a dismissal should survive a rerun.
- A partial unique index on `(pull_request_id, head_sha)` filtered to in-flight
  statuses makes a duplicate concurrent run impossible.

## Consequences

**Good.** One fewer table, one fewer join on every read. "Show me the history"
and "show me the latest" are both trivial queries. The lifecycle is a single
explicit state machine on one entity.

**Costs.** Anything genuinely per-pull-request rather than per-run — a
per-pull-request configuration override, say — has nowhere natural to live and
would need its own table. Nothing currently needs that.

Carrying finding status across runs depends on fingerprint stability. A weak
fingerprint means dismissals get lost on rerun, which makes the fingerprint
design (M6) load-bearing rather than incidental.

## Alternatives considered

- **Keep both entities.** Rejected: the parent has no state and no behaviour.
- **One run, mutated in place on rerun.** Rejected: it destroys history, and
  comparing a review before and after a fix is a feature we want.
