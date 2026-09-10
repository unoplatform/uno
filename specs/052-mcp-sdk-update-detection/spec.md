# MCP / DevServer — Uno.Sdk update detection

## Problem

The IDE extensions notify a developer when a newer **recommended** `Uno.Sdk` is available and offer to update `global.json`. Agent/CLI users driving an app through the Uno DevServer MCP get **no equivalent signal** — an agent has no way to know the project is on an outdated SDK, or to tell the user to update.

This closes that gap by surfacing the same "update available" signal through the DevServer's `uno_health` MCP tool (and the `uno.devserver health` command), using the **same source of truth** the extensions use so recommendations stay consistent.

Related work is tracked in private issues: the original request to surface SDK-update state, and a broader agent sign-in effort whose next tier covers surfacing actionable state in `uno_health`.

## Source of truth

The recommended version is the curated manifest the IDE extensions already read:

- URL: `https://aka.platform.uno/uno-sdk-manifest` → `https://uno-assets.platform.uno/manifests/uno-sdk.json`
- Repo: `unoplatform/uno-assets` → `manifests/uno-sdk.json`
- Shape: `{ "version": "<semver>" }`

This is deliberately the **recommended** stable version, which may trail the newest NuGet release. We must **not** substitute raw NuGet "latest", or the DevServer would recommend a version the team has not blessed (and disagree with the IDE extensions).

## Design

1. **`SdkManifest` helper** (`Uno.UI.DevServer.Cli/Helpers`):
   - `GetLatestUnoSdkVersionAsync()` — best-effort GET of the manifest (5s timeout); returns `null` (never throws) when offline/unreachable.
   - `IsNewer(candidate, current)` — numeric component-by-component comparison, ignoring any `-prerelease` suffix. Mirrors the extension's comparison.

2. **Health report fields** (`HealthReport`): `UnoSdkPackage`, `LatestUnoSdkVersion`, `UnoSdkUpdateAvailable`.

3. **`HealthReportFactory.Create`** takes the fetched `latestUnoSdkVersion`, computes `updateAvailable` against the discovered `UnoSdkVersion`, and — when an update exists — adds an actionable **Warning** `ValidationIssue` (`IssueCode.UnoSdkUpdateAvailable`) whose remediation tells the agent to update the `Uno.Sdk` version in `global.json`.

4. **Public `Uno.Sdk` only:** the manifest is the recommended version for the public `Uno.Sdk`. Detection is gated to `Uno.Sdk` workspaces — an `Uno.Sdk.Private` project is never compared or advised (its `LatestUnoSdkVersion` stays `null`). The plain-text/health label uses the discovered package id (`UnoSdkPackage`) rather than a hardcoded "Uno.Sdk".

5. **Advisory is non-degrading:** an available update must not, on its own, move a working DevServer from `Healthy` to `Degraded`. The status computation excludes `UnoSdkUpdateAvailable` from the Degraded trigger (but a genuine `Warning`/`Fatal` alongside it still degrades/fails as usual).

6. **Fetch placement:**
   - MCP `uno_health` (`HealthService`) — background, cached fetch (`Lazy<Task>`, `ExecutionAndPublication`) so health calls stay non-blocking; the injected `ILogger` is threaded through so fetch failures are traceable. The version provider is injectable (`Func<CancellationToken, Task<string?>>`) so tests supply a value without real network I/O.
   - `uno.devserver health` command — the fetch is started up front so it **overlaps** discovery/host checks, and is awaited only when building the report.
   - Timeouts/cancellations are logged as a plain message (no stack trace); other failures log at Debug with the exception.

## Surfaces

- `uno_health` MCP tool → JSON now carries `unoSdkVersion`, `latestUnoSdkVersion`, `unoSdkUpdateAvailable`, and the advisory issue.
- `uno.devserver health` (plain text) → a `Recommended Uno.Sdk: <v> (update available)` line.

## Out of scope (follow-ups)

- **Action tool** `uno_update_sdk` that rewrites `global.json` (the extension's "Update" button) — proposed follow-up.
- **Uno Studio app** banner (reuses the same manifest) — tracked separately for the Studio app.
- Server-side or per-version "ignore" preferences (the extension persists these per project).

## Testing

- `Given_SdkManifest` — `IsNewer` across newer/older/equal, differing segment counts, numeric-not-lexical ordering, pre-release suffixes, and missing inputs.
- `Given_HealthReportFactory_SdkUpdate` — update-available advisory + fields; advisory-only status stays `Healthy`; up-to-date/ahead → no advisory; `Uno.Sdk.Private` → no advisory; manifest unavailable (`null`) → no advisory; a `Fatal` alongside the advisory → `Unhealthy`; a real `Warning` alongside it → `Degraded`.
- `Given_HealthReportFormatter` — update-available suffix present, up-to-date suffix absent, `Uno.Sdk.Private` label uses the actual package id, and JSON carries the new fields.
- Manual (runtime): `uno.devserver health` from a project pinned below vs. at/above the recommended version.
