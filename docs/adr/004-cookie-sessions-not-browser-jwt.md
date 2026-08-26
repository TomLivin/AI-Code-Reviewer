# ADR-004: Cookie sessions instead of browser-held JWTs

**Status:** Accepted · 2026-08-26

## Context

The Angular client needs an authenticated session against the API. The common
pattern in .NET portfolio projects is a JWT stored in `localStorage` and sent as
a bearer token.

That pattern has a property this application cannot accept: **any XSS becomes
total account compromise, and the stolen token cannot be revoked** until it
expires. This application renders untrusted source code from arbitrary GitHub
repositories in the browser, so XSS risk is elevated rather than typical.

## Decision

Authentication between the browser and the API uses an `HttpOnly`, `Secure`,
`SameSite=Strict` session cookie.

- JavaScript cannot read the cookie, so XSS cannot exfiltrate the session.
- Sessions are server-side and therefore revocable — "sign out everywhere" works.
- CSRF is handled by `SameSite=Strict` **plus** ASP.NET Core antiforgery tokens
  on state-changing requests. `SameSite` alone is not a complete CSRF control.
- The SPA and API are served same-site behind one reverse proxy, so CORS is
  locked to a single known origin.
- GitHub access tokens are held server-side only and never appear in any
  response DTO.

## Consequences

**Good.** The highest-impact browser attack against this system is closed.
Revocation is real. Fewer moving parts than a token-refresh flow.

**Costs.** Harder to consume from a native mobile client or a third-party
integration. If we ever need that, the answer is a Backend-for-Frontend: the API
keeps issuing a cookie to the browser and holds tokens server-side, rather than
moving tokens into the browser.

## Alternatives considered

- **JWT in `localStorage`.** Rejected on the XSS and revocation grounds above.
- **JWT in a cookie.** Possible, but it keeps JWT's revocation problem while
  discarding its only real advantage (statelessness across services), which a
  monolith does not need.
