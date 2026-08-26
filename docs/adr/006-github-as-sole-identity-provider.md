# ADR-006: GitHub as the sole identity provider

**Status:** Accepted · 2026-08-26

## Context

The original plan had both local email/password accounts (ASP.NET Core Identity)
and GitHub OAuth for repository access. That means two authentication paths and
an account-linking problem.

The product cannot be used at all without a GitHub account: every feature reads
from a GitHub repository.

## Decision

"Sign in with GitHub" is the only way to authenticate. A `users` row is created
on the first successful OAuth callback. There is no password in this system.

## Consequences

**Good.** ASP.NET Core Identity is removed entirely, along with password
hashing, email confirmation, password reset, lockout policy and account linking.
No password can be leaked, because none is stored. Roughly twenty hours of
well-trodden work removed from the critical path, and the resulting sign-in flow
matches what the real product would do.

**Costs.** GitHub outage means no sign-in. No demo account that works without a
GitHub identity — for portfolio purposes this is addressed with screenshots and
a recorded walkthrough rather than by weakening authentication.

**Reversal cost.** Low. Adding a second scheme later means a new provider and a
`github_accounts`-style link table; the `users` table is already provider-neutral.

## Alternatives considered

- **Both schemes.** Rejected: two auth paths, an account-linking edge case
  matrix, and more attack surface, for a sign-in method nobody would use.
- **Magic-link email.** Rejected: requires an email provider and still leaves
  the user needing GitHub OAuth before they can do anything.
