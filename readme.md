# EKG.App.GamesManagement — Games Management Service

An ASP.NET Core service for managing casino game definitions stored in Bitbucket repositories. Supports saving original games, per-operator overrides, and operator filters. Publishes a RabbitMQ notification after every change so that `EKG.Common.GamesClient` instances refresh their in-memory cache.

---

## Architecture

```
EKG.App.GamesManagement
├── EKG.App.GamesManagement.Host          # ASP.NET Core entry point — controllers, DI wiring
├── EKG.App.GamesManagement.BLL           # Business logic handlers + publisher
├── EKG.App.GamesManagement.DAL           # Bitbucket HTTP repository
├── EKG.App.GamesManagement.Model         # Shared entities, request/response types
└── EKG.App.GamesManagement.Tests.Unit    # Unit tests — handler business logic (mocked repo)
```

### Infrastructure Dependencies

| Service    | Purpose                            | Default Port |
|------------|------------------------------------|-------------|
| RabbitMQ 3 | Notify GamesClient of changes      | 5672        |
| Redis 7    | Session/token validation (via ACS) | 6379        |

Bitbucket is accessed over HTTPS — no local infrastructure needed.

---

## Authentication

All `/games/*` endpoints require either:
- `X-EKG-Session: <sessionToken>` — admin login session
- `X-EKG-Token: <apiToken>` — API token with `gamesmanagement` component access

Unauthenticated requests receive **HTTP 401**. The `EkgAuthAttribute(AcsComponents.GamesManagement)` from `EKG.Common.ACS` handles validation on all actions in `GamesController`.

See [`EKG.Docs/AuthenticationFlow.md`](../EKG.Docs/AuthenticationFlow.md) for the full authentication flow.

---

## Bitbucket Repositories

| Repo URL | Purpose |
|---|---|
| `https://bitbucket.org/evkgroup/ekg.caas.games` | Original game definitions |
| `https://bitbucket.org/evkgroup/ekg.caas.operatorgames` | Per-operator overrides and filters |

### File naming conventions

| File | Repo | Description |
|---|---|---|
| `{vendor}_{gameslug}.json` | `ekg.caas.games` | Full game definition |
| `{domainId}/{vendor}_{gameslug}.json` | `ekg.caas.operatorgames` | Changed fields only (diff vs original) |
| `{domainId}/filter.json` | `ekg.caas.operatorgames` | Operator filter (include/exclude vendors and game IDs) |

---

## API

### `POST /games/save`

Saves a game to the Bitbucket games repository and notifies subscribers.

**Request:**
```json
{
  "domainId": 1,
  "game": {
    "id": 41836,
    "slug": "muertitosvideobingo_rgs_matrix",
    "vendor": "RGS_Matrix",
    "enabled": true,
    "...": "..."
  }
}
```

**Response (success):**
```json
{ "success": true, "executionTime": 420 }
```

---

### `POST /games/save-override`

Compares the changed game with the original in Bitbucket, computes a diff of changed fields, and commits the diff to the operator-games repository.

**Request:**
```json
{
  "domainId": 1001,
  "changedGame": {
    "slug": "muertitosvideobingo_rgs_matrix",
    "vendor": "RGS_Matrix",
    "enabled": false
  }
}
```

**Response (success):**
```json
{ "success": true, "executionTime": 310 }
```

**Response (original not found):**
```json
{ "success": false, "errorMessage": "Original game not found in repository.", "executionTime": 120 }
```

---

### `POST /games/import`

Accepts a `.json` or `.zip` file containing game data in any format. Uses Groq AI (`llama-3.3-70b-versatile`) to extract and normalise games into the Game model, commits each game as `{Vendor}_{Slug}.json` on a new feature branch, then opens one Pull Request covering the entire batch.

**Request:** `multipart/form-data`

| Field | Type | Description |
|---|---|---|
| `file` | File | `.json` file with one or many games, or `.zip` archive containing multiple `.json` files |

**Response (success):**
```json
{
  "success": true,
  "gamesFound": 6,
  "gamesCommitted": 6,
  "branchName": "import/20260523-111745",
  "pullRequestUrl": "https://bitbucket.org/evkgroup/ekg.caas.games/pull-requests/1"
}
```

**Response (no games found):**
```json
{ "success": true, "gamesFound": 0, "gamesCommitted": 0 }
```

**Notes:**
- Games with missing `slug` or `vendor` are skipped and logged.
- If PR creation fails (e.g. token scope), the response still returns `success: true` with `pullRequestUrl: null` and `branchName` so the branch can be used to open a PR manually.
- Large files should be split into batches (~25 KB each) to stay within the Groq free-tier token-per-minute limit.

---

### `POST /games/save-filter`

Saves an operator filter to the operator-games repository as `{domainId}/filter.json`.

**Request:**
```json
{
  "domainId": 1001,
  "filter": {
    "includeVendors": ["Netent", "Quickspin"],
    "excludeVendors": ["Wazdan", "Playtech"],
    "includeGameIds": [23, 2222, 32223],
    "excludeGameIds": [3122, 213123, 324343]
  }
}
```

**Response (success):**
```json
{ "success": true, "executionTime": 280 }
```

---

## Error Codes

| Condition | Description |
|---|---|
| Game required | Game object is missing |
| Game slug required | Slug field is empty |
| Game vendor required | Vendor field is empty |
| Original game not found | Cannot fetch original from Bitbucket before computing diff |
| DomainId required | DomainId is 0 or missing |
| Filter required | Filter object is missing |

---

## Configuration

**`appsettings.json`:**

```json
{
  "Application": { "Name": "GamesManagement" },
  "Bitbucket": {
    "GamesRepoToken": "",
    "OperatorGamesRepoToken": "",
    "Workspace": "evkgroup",
    "GamesRepo": "ekg.caas.games",
    "OperatorGamesRepo": "ekg.caas.operatorgames",
    "Branch": "main"
  },
  "MessageBroker": {
    "Host": "amqp://localhost",
    "Username": "guest",
    "Password": "guest",
    "Topics": {}
  }
}
```

| Key | Description |
|---|---|
| `Bitbucket:GamesRepoToken` | Bitbucket access token for the games repository (requires `repository:write` + `pullrequest:write`) |
| `Bitbucket:OperatorGamesRepoToken` | Bitbucket access token for the operator-games repository |
| `Bitbucket:Workspace` | Bitbucket workspace |
| `Bitbucket:GamesRepo` | Slug of the original games repo |
| `Bitbucket:OperatorGamesRepo` | Slug of the operator-games repo |
| `Bitbucket:Branch` | Branch to commit to (default: `main`) |
| `MessageBroker:*` | RabbitMQ connection settings |
| `Groq:ApiKey` | Groq API key — set via `GROQ_API_KEY` env var / `.env` file |
| `Groq:ModelName` | Groq model for game extraction (default: `llama-3.3-70b-versatile`) |

---

## Running Locally

### Option 1 — Tilt (recommended)

Requires [Rancher Desktop](https://rancherdesktop.io/) with `dockerd` and [Tilt](https://tilt.dev/) installed.

Populate `.env` in the repo root (gitignored):

```
BITBUCKET_GAMES_TOKEN=your-games-repo-token
BITBUCKET_OPERATOR_GAMES_TOKEN=your-operatorgames-repo-token
```

```bash
tilt up
```

| Service | URL |
|---|---|
| GamesManagement API | http://localhost:5050/scalar/v1 |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |

The `games-management` resource uses manual trigger mode — click **▶ Run** in the Tilt UI to (re)start it after a code change.

### Option 2 — Docker Compose

```bash
docker-compose up --build
```

Same URLs as above.

### Option 3 — dotnet run

Requires RabbitMQ on `localhost:5672`.

```bash
$env:Bitbucket__GamesRepoToken="your-games-repo-token"
$env:Bitbucket__OperatorGamesRepoToken="your-operatorgames-repo-token"
$env:Groq__ApiKey="your-groq-api-key"
cd EKG.App.GamesManagement.Host
dotnet run
```

---

## Local NuGet Setup

This project depends on `EKG.Common.GamesClient` from GitHub Packages. Create `nuget.config` locally before building:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/evgenkovalenko/index.json" />
    <add key="local-packages" value="./local-packages" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="evgenkovalenko" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
    </github>
  </packageSourceCredentials>
</configuration>
```

Replace `YOUR_GITHUB_PAT` with a GitHub PAT with `read:packages` scope.

**For local dev before `EKG.Common.GamesClient` is published:** build the package locally and copy it to `local-packages/`:

```bash
cd ..\EKG.Common.GamesClient
dotnet pack EKG.Common.GamesClient\EKG.Common.GamesClient.csproj -c Release
copy EKG.Common.GamesClient\bin\Release\*.nupkg ..\EKG.App.GamesManagement\local-packages\
```

---

## Game Model

The `Game` class in `EKG.App.GamesManagement.Model` maps directly to the JSON files stored in the Bitbucket games repository. All nested objects use concrete types rather than `JsonElement`:

| Property | Type | Description |
|---|---|---|
| `Additional` | `Dictionary<string, AdditionalFeature>` | Feature flags (e.g. `highStake`, `fullScreen`); each has `DisplayName` and a `Value` (bool/string/number) |
| `Bonus` | `GameBonus` | `Contribution` (double), `Overridable` (bool) |
| `Creation` | `GameCreation` | `LastModified`, `Time`, `NewGameExpiryTime` (DateTime), `UniversalId` |
| `PlayMode` | `GamePlayMode` | `Anonymity`, `Fun`, `RealMoney` (bool) |
| `Popularity` | `GamePopularity` | `Coefficient` (double) |
| `Presentation` | `GamePresentation` | Localized string dictionaries for `GameName`, `Thumbnail`, `Logo`, etc.; `Icons` keyed by pixel size |
| `Property` | `GameProperty` | `FreeSpin`, `HitFrequency`, `Terminal`, `Width`, `Height`, `License` |
| `Report` | `GameReport` | `Category`, `InvoicingGroup` |
| `RuleUrl` | `Dictionary<string, string>` | Locale → URL map |
| `Currencies` | `JsonElement` | Structure varies by vendor |
| `MaintenanceWindows` | `JsonElement` | Structure varies by vendor |
| `VendorLimits` | `JsonElement` | Structure varies by vendor |

All supporting types are defined in `GameTypes.cs` in the Model project.

---

## Tests

### Unit Tests

No infrastructure required.

```bash
cd EKG.App.GamesManagement.Tests.Unit
dotnet test
```

| Suite | Tests |
|---|---|
| `SaveGameHandlerTests` | Happy path, missing game, missing slug, missing vendor, correct filename format |
| `SaveGameOverrideHandlerTests` | Happy path, original not found, missing domainId, missing changed game, correct file path |
| `SaveOperatorFilterHandlerTests` | Happy path, missing domainId, missing filter, correct file path |

---

## CI/CD

On every push to `main`, GitHub Actions:

1. **Publishes `EKG.App.GamesManagement.Model`** as a NuGet package to GitHub Packages — versioned `1.0.{run_number}`
2. **Builds and pushes a Docker image** to GitHub Container Registry — tagged `ghcr.io/evgenkovalenko/ekg.app.gamesmanagement:1.0.{run_number}` and `:latest`

**Add the secret** (run once after creating the GitHub repo):

```bash
gh secret set NUGET_READ_TOKEN
```

The token must be a GitHub PAT with `read:packages` scope.
