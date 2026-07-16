# Hot-Reload Baseline Identity Handshake: RID & Module MVIDs in `ConfigureServer`

> **Status: DRAFT — hard requirement: needs reviews before implementation.**
> Written 2026-07-16, following the uno-private#2256 investigation. Nothing below is
> committed design; the protocol addition in particular must be reviewed (message
> compatibility, module-list scope) before any code is written.

## Overview & Objectives

The RID-specific baseline alignment (follow-up of spec 047/049, shipped for
uno-private#2256) re-points the kept head flavor's `CompilationOutputInfo` to the output the
running application was *probably* built from: the trigger is declarative (the
`RuntimeIdentifier` captured in the application's build properties), and a candidate path is
accepted when its module is **readable** (a valid PE with an MVID — the same primitive
Roslyn's EnC uses to decide a project is "built").

Readability is however not **identity**. Roslyn's EnC contract is stricter: the baseline
assembly on disk must be *the very module loaded in the application* (same MVID), otherwise:

- the emitted deltas carry a `ModuleId` the application never loaded, and the client drops
  them silently — server says `Found 1 metadata updates`, device shows nothing;
- or (missing baseline) EnC treats the project as **not built** and emits nothing at all —
  `Solution update status: None`, visible only in Roslyn's own EnC session log
  (`EditSession.cs`: `mvid == Guid.Empty` → `project not built`).

Debugger-based EnC does not have this gap: the debugger reports the debuggee's loaded-module
MVIDs and Roslyn matches baselines against them. Watch-mode Roslyn cannot — it has no channel
to the application. **The dev-server does have that channel.** This spec closes the gap the
same way the debugger does: the client reports its module identities, and the server treats
MVID equality as the source of truth for baseline selection and diagnostics.

Remaining failure modes this addresses (all silent today, even after the alignment):

1. **Stale RID-specific output** — the app was deployed, then rebuilt locally (other RID, or
   RID-less `dotnet build`): a readable-but-wrong baseline wins; deltas are emitted and
   silently dropped client-side.
2. **Ambiguous probe** — custom output layouts resolved by "newest readable candidate", a
   heuristic tie-break where identity would decide exactly.
3. **Library staleness** — the alignment deliberately leaves project references untouched
   (their RID-less evaluated paths are correct *by layout*), but nothing verifies their
   on-disk baselines match the modules deployed in the app.
4. **Configuration mismatch** — Release-deployed app against a Debug workspace baseline (and
   vice versa): readable, wrong, silent.

## Proposal

### 1. Protocol — `ConfigureServer` additions

```csharp
public record ConfigureServer(
	string ProjectPath,
	string[] MetadataUpdateCapabilities,
	string[] MSBuildPropertiesRaw,
	string? HotReloadInfoPath,
	bool EnableMetadataUpdates,
	bool EnableHotReloadThruDebugger,
	string? RuntimeTargetFramework = null,
	string? RuntimeIdentifier = null,          // NEW — the RID the running build was produced with
	AppModuleId[]? AppModules = null)          // NEW — loaded module identities
	: IMessage;

public record AppModuleId(string AssemblyName, Guid Mvid);
```

- `RuntimeIdentifier`: determined client-side the same declarative way as
  `RuntimeTargetFramework` (from the build capture; unlike the TFM it cannot be probed at
  runtime, but transporting it explicitly removes the server's dependency on the MSBuild
  property bag shape). The captured-properties entry remains the fallback for older clients.
- `AppModules`: the identities of the modules loaded in the application —
  `Assembly.Modules[0].ModuleVersionId` over `AppDomain.CurrentDomain.GetAssemblies()`,
  filtered to non-framework assemblies (heuristic to review: exclude `System.*`,
  `Microsoft.*` except `Microsoft.UI/Uno.*`? — see Open questions). Collected once at
  `ConfigureServer` time; cost is negligible (no metadata read, the MVID is already in
  memory).

Both fields default to `null`: an older client simply doesn't send them and the server keeps
today's behavior end-to-end (captured RID + readability acceptance). An older *server*
receiving the new fields must ignore them — to be covered by a serializer-tolerance test.

### 2. Server — identity-based selection and diagnostics

In the workspace initialization pipeline (after TFM filtering, replacing the acceptance rule
of the current alignment):

1. **Head flavor**: among the candidate output paths (evaluated path, RID-specific path,
   subtree probe), accept the one whose MVID **equals** the app-reported MVID for the head
   assembly. The "newest readable" tie-break disappears — identity decides.
   - No candidate matches → `Warn` stating the app's MVID, every probed path with its MVID,
     and that hot reload will not apply until the deployed build's outputs are present.
     (Open question: degrade vs refuse init.)
   - App didn't report modules (older client) → keep the current acceptance (readability,
     RID-specific preferred).
2. **Project references**: for each library of the filtered solution present in
   `AppModules`, compare the baseline MVID with the reported one; mismatches produce a
   single aggregated `Warn` (list of stale libraries). No re-pointing for libraries — their
   correct location is unambiguous; staleness means *rebuild*, which the user must do anyway.
3. **Emit-time anti-silence**: when EnC returns zero updates and zero rude-edit diagnostics
   for a change that touched a project flagged unmatched/unbuilt in (1)/(2), surface a
   `Warn` correlating the two (today the only trace is Roslyn's session log).

### 3. Explicitly out of scope

- **Evaluation-side RID injection** (global properties, or the
  `UnoHotReloadRuntimeIdentifier`/`UnoHotReloadTargetFramework` props pipe): rejected during
  the 2256 investigation — `MSBuildWorkspace` applies global properties to every project of
  the graph without MSBuild's P2P negotiation (`SetTargetFramework` /
  `GlobalPropertiesToRemove`), so a global RID relocates the expected outputs of every
  RID-less-built library; and the props pipe is maintenance the team wants to remove, not
  entrench.
- **Per-root-project global properties** (evaluating only the head with pinned TFM/RID):
  separate investigation, parked — requires a custom loader/BuildHost; would subsume part of
  this spec if it ever lands, and the MVID handshake remains valuable regardless (it
  validates *whatever* path selection produced).

## Test plan (sketch)

- **Unit (server)**: fixture emitting PE files with controlled distinct MVIDs (two `Emit`s of
  the same compilation source differ; capture each MVID at emit time) — selection by
  identity among decoys, no-match warning content, older-client fallback, library staleness
  aggregation.
- **Protocol**: round-trip serialization with/without the new fields; old-server tolerance
  (unknown members ignored) and old-client tolerance (nulls).
- **Runtime tests (client)**: `AppModules` contains the head assembly with its real MVID on
  every target (extends the existing ungated `Given_ClientHotReloadProcessor` coverage).
- **E2E (DevServer.Tests)**: workspace against a deployed-then-rebuilt app layout asserting
  the mismatch warning fires and deltas stop being emitted against the wrong baseline.

## References

- uno-private#2256 (both episodes: `netX.0-skia` TFM misreport; RID-less baseline silently
  "not built").
- PRs: #23790 (client runtime TFM probe, master), #23791 (6.6 backport, merged), #23798
  (RID-specific baseline alignment, 6.6), plus its master port.
- Roslyn: `EditSession.cs` ("project not built" on `mvid == Guid.Empty`),
  `DebuggingSession.GetProjectModuleIdAsync` (`ReadAssemblyModuleVersionId`,
  `FileNotFoundException → Guid.Empty` without error) — the primitives this spec builds on.
- Specs: 047 (property whitelist & runtime-reported TFM filtering), 049, 050.

## Open questions (to settle in review)

1. **Module list scope**: all loaded assemblies vs user-code filter — size vs completeness;
   should dynamically-loaded (post-`ConfigureServer`) assemblies trigger an update message?
2. **Refuse vs degrade** when the head MVID matches no candidate: refusing init makes the
   failure loud but kills XAML-only `UpdateFile` scenarios that don't need EnC; degrading
   keeps them alive behind a warning.
3. **wasm**: `ModuleVersionId` availability and cost under Mono interpreter — expected fine,
   to verify.
4. **Serializer tolerance** of the message pipeline for unknown/extra members (old server ×
   new client) — verify, don't assume.
5. Should `RuntimeIdentifier` eventually feed a *validated* evaluation (the parked
   root-project investigation) instead of path probing entirely?
