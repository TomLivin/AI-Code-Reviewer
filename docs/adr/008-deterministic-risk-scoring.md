# ADR-008: Deterministic risk scoring; the model never sets the score

**Status:** Accepted · 2026-08-26

## Context

A pull request gets a single headline number: "Risk 71/100". The obvious
implementation is to ask the LLM for it.

An LLM-produced score is not reproducible across runs, cannot be unit tested,
cannot be explained to the user beyond restating it, and would change silently
whenever the prompt or the model version changed.

## Decision

The model proposes **findings**. The system prices them.

`RiskScoreCalculator` lives in `Domain`, is a pure function, and takes only
validated findings plus a weight configuration. Its stages are:

1. Per finding: `severity weight × confidence factor × source factor`.
2. Diminishing returns within a category, so fifty style nits cannot outrank one
   authorisation bypass.
3. Asymptotic normalisation to 0–100.
4. Contextual multipliers (security-sensitive paths, no tests changed, findings
   confined to test files), each recorded with its reason.
5. Severity floors and banding.

Every term is persisted to `review_runs.score_breakdown` and rendered in the UI,
so the score is explainable rather than asserted.

## Consequences

**Good.** Reproducible: the same findings always produce the same score. Unit
testable against a calibration table. Explainable in the UI. Tunable without
touching a prompt. Weights are snapshotted per run, so changing them never
silently rewrites history.

**Costs.** The weights need calibrating against real pull requests, and the
calibration is a judgement call we own rather than one we can outsource.

## Alternatives considered

- **Ask the model for a score.** Rejected on reproducibility, testability and
  explainability.
- **Model score blended with a computed score.** Rejected: it inherits the
  non-determinism while making the result harder to explain.
