# ADR-005: Never compile or execute untrusted repository code

**Status:** Accepted · 2026-08-26

## Context

Static analysis of a .NET repository is most accurate with a full Roslyn
semantic model. Building one requires restoring packages and compiling.

Both steps execute attacker-controlled code on our server:

- `dotnet restore` runs MSBuild targets that arrive inside NuGet packages.
- Roslyn analyzers and **source generators run inside the compiler process**.
  A repository that references a malicious generator achieves arbitrary code
  execution simply by being compiled.

The threat is not hypothetical. Any user can point this system at any public
repository, so we must assume repository content is hostile.

## Decision

Repository code is **parsed, never compiled and never executed**.

Static analysis uses `CSharpSyntaxTree.ParseText`, which is a pure function over
text: no package restore, no MSBuild, no semantic model, no code execution.
Rules are written against the syntax tree.

Repository content is likewise never shell-interpolated, never used to build a
file path without validation, and never used as a URL to fetch.

If semantic analysis is ever genuinely required, it must run in an ephemeral
container with no network access, no mounted secrets, a read-only file system,
a hard timeout and a non-root user — and that will get its own ADR.

## Consequences

**Good.** The most severe risk in the system is eliminated by design rather than
mitigated by configuration. Analysis is fast and needs no build toolchain.

**Costs.** Syntax-only rules cannot resolve types across files, so some checks
(for example, "is this variable actually a `DbContext`?") are heuristic or
impossible. We accept lower analysis fidelity in exchange for not running
attacker code. This limitation is documented in the user-facing docs rather than
hidden.

## Alternatives considered

- **Compile in a sandbox now.** Rejected for the current scope: correct
  sandboxing is a project in itself, and the benefit does not yet justify it.
- **Trust repositories the user owns.** Rejected: a user can own a fork of
  anything, and "the user vouched for it" is not a security boundary.
