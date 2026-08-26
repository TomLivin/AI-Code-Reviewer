# Database Design

PostgreSQL 17. Nine tables, EF Core migrations, `snake_case` naming.

## Entity relationships

```mermaid
erDiagram
    users ||--o| github_accounts : "connects"
    users ||--o{ code_repositories : "owns"
    code_repositories ||--o{ pull_requests : "contains"
    pull_requests ||--o{ pull_request_files : "snapshots"
    pull_requests ||--o{ review_runs : "reviewed by"
    review_runs ||--o{ review_findings : "produces"
    review_runs ||--o{ ai_usages : "costs"

    users {
        uuid id PK
        bigint github_user_id UK
        text login
        timestamptz last_signed_in_at_utc
    }
    github_accounts {
        uuid id PK
        uuid user_id FK-UK
        bytea access_token_protected
        text_array scopes
    }
    code_repositories {
        uuid id PK
        uuid owner_user_id FK
        bigint github_repository_id
        text full_name
        timestamptz deleted_at_utc "soft delete"
    }
    pull_requests {
        uuid id PK
        uuid code_repository_id FK
        int number
        text head_sha
        text state
    }
    pull_request_files {
        uuid id PK
        uuid pull_request_id FK
        text head_sha
        text path
        text patch
    }
    review_runs {
        uuid id PK
        uuid pull_request_id FK
        uuid code_repository_id FK "denormalised"
        text head_sha
        text status
        int risk_score
        text score_breakdown_json
    }
    review_findings {
        uuid id PK
        uuid review_run_id FK
        uuid code_repository_id FK "denormalised"
        text severity
        text fingerprint
        text status
    }
    ai_usages {
        uuid id PK
        uuid review_run_id FK
        int input_tokens
        numeric estimated_cost_usd
    }
```

`background_jobs` sits outside this graph on purpose: it is generic
infrastructure shared by review, repository sync and comment publishing, and it
holds no foreign key into the domain.

## Decisions worth explaining

**UUIDv7 primary keys, generated in code.** Time-ordered, so inserts append to
the index instead of fragmenting it the way random UUIDv4 keys do. Generating
them in the domain means an entity is valid the moment it is constructed, and a
graph can be wired up before anything reaches the database.

**Enums stored as text.** Readable in `psql`, and adding a member never silently
reinterprets existing rows the way inserting into an int-backed enum would. The
cost is a few bytes per row and no database-level constraint on the value set.

**Audit timestamps written by an interceptor.** `CreatedAtUtc` and
`UpdatedAtUtc` have no public setter; `AuditableEntityInterceptor` stamps them
centrally so no call site can forget and no caller can rewrite history.

**`repository_id` denormalised onto `review_runs` and `review_findings`.**
Analytics filters findings by repository and severity over time. Carrying the key
removes a two-table join from the dominant read path, and makes the composite
index below possible at all.

## Index rationale

| Index | Why it exists |
|-------|---------------|
| `ix_review_runs_active_per_commit` — unique on `(pull_request_id, head_sha)` filtered to `status IN ('Queued','Running')` | **Concurrency control in the schema.** A double-clicked Review button makes the second insert raise `23505`, which the API turns into the existing run. Without it the pull request is analysed twice at twice the cost. The filter excludes completed runs so re-reviewing after a fix still works. |
| `ix_background_jobs_claimable` — on `available_at_utc` filtered to `state = 'Pending'` | The dispatcher polls this continuously. A partial index holds only claimable rows, so it stays small no matter how much completed history accumulates. |
| `ix_background_jobs_expiring_leases` — on `locked_until_utc` filtered to `state = 'Running'` | Lets the reaper find jobs whose worker died without scanning the table. |
| `ix_review_findings_review_run_id_fingerprint` — unique | De-duplication enforced by the database, not only by the merge step. If that logic regresses, this catches it rather than showing the user the same problem twice. |
| `ix_review_findings_code_repository_id_severity_created_at_utc` | Serves the entire analytics query — findings by severity over time for a repository — from one index. Only possible because `code_repository_id` is denormalised. |
| `ix_code_repositories_owner_user_id` filtered to `deleted_at_utc IS NULL` | Every listing hides disconnected repositories, so those rows are kept out of the index altogether. |
| `ix_pull_requests_code_repository_id_number` — unique | The natural key, which makes synchronisation an idempotent upsert. |
| `ix_pull_request_files_pull_request_id_head_sha_path` — unique | Snapshots are per commit, so a push adds rows rather than overwriting what an earlier review was based on. |

## Delete behaviour

Ownership cascades: deleting a user removes their GitHub connection and
repositories; deleting a repository removes its pull requests, and so on down to
findings and usage records.

Two exceptions:

- `review_runs.requested_by_user_id` is **restricted**. A user with review
  history cannot be hard-deleted; the correct answer to an erasure request is
  anonymisation, not destroying other people's review records.
- `review_findings.status_changed_by_user_id` is **set null**, since it is an
  audit reference rather than an ownership link.

Only `code_repositories` is soft-deleted. Disconnecting must hide the repository
while leaving review history intact, and reconnecting should restore the row
rather than create a duplicate — which is why the unique index on
`(owner_user_id, github_repository_id)` deliberately ignores the delete flag.
Findings, runs and jobs are never soft-deleted: they are the audit trail, and a
global filter over them would silently corrupt every analytics query.

## Working with migrations

```bash
docker compose up -d postgres
dotnet tool restore
dotnet dotnet-ef database update --project src/AiCodeReview.Infrastructure --startup-project src/AiCodeReview.Infrastructure
```

Adding one:

```bash
dotnet dotnet-ef migrations add <Name> --project src/AiCodeReview.Infrastructure --startup-project src/AiCodeReview.Infrastructure --output-dir Persistence/Migrations
```

Migrations are generated from `AppDbContextFactory` in the Infrastructure
project, so neither the API nor the Worker needs to be involved — a migration
job in a container needs no host configuration. They are never applied
automatically at startup: with more than one replica that is a race, so applying
them is a separate, explicit step.
