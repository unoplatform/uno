# Hot-Reload: Batched IDE File Updates with Workspace Readiness (VS)

**Repo**: `uno` (Uno.HotReload / Uno.UI.RemoteControl)
**Created**: 2026-07-21
**Status**: Draft
**Issue**: [unoplatform/uno#23840](https://github.com/unoplatform/uno/issues/23840)
**Related**: [spec 050](../050-hotreload-updatefile-workspace-gate/spec.md) (server-side workspace gate),
[#23838](https://github.com/unoplatform/uno/issues/23838) (client-side init gate),
[#23839](https://github.com/unoplatform/uno/issues/23839) (ENC0033 / GenerateAssemblyInfo)

## Overview & Objectives

In Visual Studio mode, a multi-file `UpdateFile` request that **creates** files (e.g. a new
XAML view: `.xaml.cs` + `.xaml` in one request) reaches VS as N independent, unordered
single-file messages, followed by a fixed-delay `ForceHotReloadIdeMessage` that executes
`Debug.ApplyCodeChanges`. VS's EnC then evaluates the change-set on whatever Roslyn snapshot
exists at that instant — typically **before** the project system has integrated the added
files and re-run the source generators. The result is a **modal error popup**
(`CS1061 'X' does not contain a definition for 'InitializeComponent'` — "Unable to build
changes" / "Changes not supported") for a state that is purely transient: a manual re-apply a
second later succeeds.

The popup **is** the defect. Any strategy that lets VS compile an intermediate state — and
recovers afterwards (retry, auto-rebuild) — still disrupts the user. The only acceptable fix
is to **not trigger EnC until the workspace can compile the change-set**.

### Key objective

One `UpdateFile` request = **one batched IDE interaction**: a single message carrying every
edit plus the hot-reload intent. The extension applies the edits in order, waits (bounded)
until the VS Roslyn workspace has integrated the added files **and** their source-generator
outputs, and only then triggers `Debug.ApplyCodeChanges`. No intermediate compile, no popup.

## Verified facts (investigation grounding)

Established from source review and from the minimal repro used for #23840 (validated
2026-07-21 on `net10.0-desktop`, `Uno.WinUI(.DevServer) 6.7.0-dev.828`, VS 2022):

| # | Fact | Consequence |
|---|------|-------------|
| F1 | `FileUpdater.UpdateAsync` applies edits with `Task.WhenAll(request.Edits.Select(EditFileAsync))`; in VS mode each edit becomes one `UpdateFileIdeMessage` via `IDEFileEditor` (`Uno.HotReload/IO/FileUpdater.cs`, `Server.Processors/HotReload/IDEFileEditor.cs`). | Multi-file requests reach VS as N unordered, concurrent messages; the client's edit ordering is not preserved. |
| F2 | `IDEFileEditor` forces `saveToDisk = forceSaveOnDisk ?? true` (see the TODO it carries). | The per-request save flag is not honored today; a batched message can carry and honor it. |
| F3 | VS-side `OnUpdateFileRequestedAsync` writes new files with `File.WriteAllText` (open documents get in-memory `ReplaceText`); project-system integration (globs → `Document`/`AdditionalDocument`) and source-generator re-runs are **asynchronous** (`Uno.UI.RemoteControl.VS/EntryPoint.cs`). | When the write acks, the Roslyn workspace does not yet reflect the added files, let alone their generated outputs. |
| F4 | The force is time-based: `FileUpdater` waits `ForceHotReloadDelay ?? 500 ms`, then sends one `ForceHotReloadIdeMessage`; VS immediately runs `Debug.ApplyCodeChanges`. | EnC compiles whatever snapshot exists at T+delay. For added XAML views the generator output is missing → CS1061 → modal popup. Transient by nature (manual re-apply succeeds). |
| F5 | The auto-retry in `HotReloadOperation.Complete` fires only on `NoChanges`; `CompilationError` is terminal. | No server-side recovery — and retry is not a fix anyway: the popup has already fired. Prevention is the requirement (decided). |
| F6 | The same multi-file creation batch through the dev-server workspace path succeeds: the emitted delta contains the new type **and** the regenerated generator outputs (`*_Bindings`, `EmbeddedXamlSourcesProvider`, `GlobalStaticResources`). | Generators handle dynamically-added AdditionalFiles fine; the defect is *when* VS EnC is triggered, not the generator pipeline. |
| F7 | `Uno.UI.RemoteControl.VS.dll` (EntryPoint + IDE-message handlers) ships in the **same** `Uno.WinUI.DevServer` package as the server processors (`tools/rc/17.0/`), loaded by the `uno.studio` VSIX through the reflection-probed EntryPoint contract. | A new IDE message and its handler ship atomically with the sender — no cross-version matrix — as long as the probed EntryPoint constructor contract is untouched. |
| F8 | `IFileUpdater` was extracted precisely for composition (spec 050); the processor swaps `WorkspaceGatedFileUpdater.Inner` on each `ConfigureServer` (`ServerHotReloadProcessor.CreateFileUpdater`). | A VS-specific `IFileUpdater` implementation plugs into an existing seam; the workspace gate keeps decorating it unchanged. |
| F9 | Roslyn workspace access from the extension is standard: `SComponentModel` → `IComponentModel.GetService<VisualStudioWorkspace>()`; workspace reads are free-threaded; `Project.GetCompilationAsync` **forces source-generator execution** and the resulting compilation is cached on the snapshot the subsequent EnC evaluation reuses. | The extension can deterministically wait until the change-set is compilable before triggering EnC. (`Microsoft.VisualStudio.LanguageServices` comes with the referenced VS SDK meta-package — to confirm at first Windows build.) |

## Design

### New IDE message — implements the existing contracts

The batched message **implements the existing `IUpdateFileRequest`** (which already carries
everything needed, *including* the hot-reload intent: `Edits`, `ForceSaveOnDisk`,
`IsForceHotReloadDisabled`, `ForceHotReloadDelay/Attempts`, `RequestId`), and the reply
**implements `IUpdateFileResponse`** (per-file `FileEditResult`s + `GlobalError` +
`HotReloadCorrelationId`), so `IdeFileUpdater` forwards the request and re-emits the
response with no bespoke mapping layer:

```
UpdateFileRequestIdeMessage(long CorrelationId, …)  : IdeMessageWithCorrelationId, IUpdateFileRequest
UpdateFileResponseIdeMessage(long CorrelationId, …)   : IdeMessageWithCorrelationId, IUpdateFileResponse
```

Why not reuse the existing concrete `UpdateFileRequest` directly: it is an `IMessage`
(client↔server RC frame, `Uno.UI.RemoteControl` assembly) while the IDE channel requires an
`IdeMessage` envelope, and the VS extension only references
`Uno.UI.RemoteControl.Messaging`. The interfaces and `FileEdit`/`FileEditResult` are made
visible to `Messaging` through the **linked-source mechanism already used for exactly this**
(`IUpdateFileRequest.cs` / `FileEdit.cs` compile into both `Uno.HotReload` and
`Uno.UI.RemoteControl` behind `#if UNO_HOTRELOAD` today — the same link is added to
`Messaging`; namespace unification to be settled at implementation, serializer support for
the nested records to be verified with `IdeMessageSerializer`).

The legacy `UpdateFileIdeMessage` and `ForceHotReloadIdeMessage` records are kept (F7 makes
them same-package, but the standalone force message remains needed for the `NoChanges`
auto-retry, and the legacy update message is kept one release as a rollback path).

### Server side — an IDE-batched `IFileUpdater`

`FileUpdater.UpdateAsync` is refactored into a template method with two extension points
(base behavior unchanged, existing tests must keep passing):

- `ApplyEditsAsync(request)` — base: current `Task.WhenAll` over `IFileEditor.EditAsync`;
- `TriggerHotReloadAsync(request)` — base: current pre-delay + `requestHotReload()`.

A new `IdeFileUpdater : FileUpdater` (wired by `CreateFileUpdater` when
`isRunningInsideVisualStudio`, replacing `IDEFileEditor`) overrides:

- `ApplyEditsAsync` — splits the request: **deletes** (`NewText == null`) go to the on-disk
  editor (unchanged semantics), **writes** are sent as one `UpdateFileRequestIdeMessage` carrying
  the `IUpdateFileRequest` data as-is (the force intent is `!IsForceHotReloadDisabled`),
  preserving edit order and the per-request save flag (fixes F2);
- `TriggerHotReloadAsync` — no-op: the trigger travels with the batch.

Invariants preserved: `tracker.StartHotReload` before anything (the operation must exist when
EnC results flow back); the hot-reload info file is persisted **before** the IDE is asked to
trigger (the batch carries the trigger, so the write happens before the send — the template
exposes the ordering); `EnableAutoRetryIfNoChanges` stays armed and its retries use the
standalone `ForceHotReloadIdeMessage` (retries never rewrite files).

### Extension side — apply, wait for readiness, then trigger

New handler for `UpdateFileRequestIdeMessage` in `EntryPoint`:

1. **Apply** every edit sequentially, in batch order, reusing the current per-file logic
   (in-memory `ReplaceText` for open documents, `File.WriteAllText` otherwise) — but **without
   any `document.Save()` yet** (see the race analysis below).
2. **Ack early**: reply with the `IUpdateFileResponse` result once the writes are applied. The
   readiness wait and the trigger continue asynchronously — their outcome flows through the
   existing hot-reload operation/status channel (which the client already observes via
   `WaitForHotReload`). This keeps the batch round-trip within the client's
   `ServerUpdateTimeout`.
3. **Readiness** (only when the request carries the force intent and the batch contains
   creations, `OldText == null`):
   - poll `workspace.CurrentSolution.GetDocumentIdsWithFilePath(path)` (covers `Document` and
     `AdditionalDocument`) for each created file, ~100 ms period — absorbs the project-system
     glob re-evaluation;
   - for each touched Roslyn project (multi-TFM ⇒ several projects per csproj — all of them):
     `await project.GetCompilationAsync(ct)` — forces generator execution; the materialized
     outputs are cached on the snapshot EnC will evaluate (F9);
   - bounded by a total readiness timeout (default **10 s**); on expiry, proceed anyway (never
     worse than today) and log a warning.
4. **Save & trigger**: perform the deferred `document.Save()`s (when the save flag requires
   them), then `Debug.ApplyCodeChanges` (unchanged mechanics) — all VS-visible triggers fire
   on a snapshot that is already compilable.

Pure-edit batches (no creation) skip step 3 entirely — fast path identical to today minus the
per-file fan-out.

### Race analysis — "can VS still apply before us?"

Computing a compilation never shows the popup: Roslyn compilations are immutable snapshots
behind a shared per-project cache, so a concurrent `GetCompilationAsync` from VS and from the
extension converge on the same (complete) result — there is no exclusive resource to win. The
popup only appears on an **EnC apply**, and the apply triggers are enumerable:

1. **Our `Debug.ApplyCodeChanges`** — controlled: fired after readiness (step 4).
2. **VS "hot reload on file save"** — fires on *VS saves only*: `File.WriteAllText` (the
   created-files path) is an external write, not a VS save; the exposure is `document.Save()`
   on open documents, which step 1/4 defers until after readiness. With the deferral, every
   save-driven apply evaluates a snapshot that already contains the generator outputs.
3. **User/debugger-initiated applies** (apply on continue from a breakpoint, manual apply)
   landing inside the readiness window — cannot be prevented. Residual risk, explicitly
   accepted: the window is actively shortened (we force the compilation immediately instead
   of leaving the workspace to catch up lazily), so the exposure is strictly smaller than
   today's.

The step-2 assumption ("hot reload on save fires on VS saves only") is behavior knowledge,
not source-verified — it is part of the manual validation matrix below.

### Requirements

- **R1 — One IDE interaction per request.** All writes of an `UpdateFile` request reach VS in
  a single message, applied sequentially in the client's edit order.
- **R2 — No premature EnC.** When the batch contains created files, **no VS-visible apply
  trigger fires before readiness**: `Debug.ApplyCodeChanges` and every deferred
  `document.Save()` come after the workspace exposes the created files and their generator
  outputs (readiness = documents present + `GetCompilationAsync` completed per touched
  project).
- **R3 — Bounded readiness.** The wait is capped (default 10 s); on timeout the trigger fires
  anyway with a logged warning — behavior degrades to today's, never below.
- **R4 — Fast path for pure edits.** Batches without creations skip readiness entirely.
- **R5 — Deletes unchanged.** `NewText == null` edits keep going through the on-disk editor,
  inside the same tracked operation.
- **R6 — Info file before trigger.** The hot-reload info file is persisted before the message
  carrying the trigger is sent.
- **R7 — Auto-retry unchanged.** The `NoChanges` retry keeps using the standalone
  `ForceHotReloadIdeMessage`; it never re-sends file contents.
- **R8 — Legacy path retained.** `UpdateFileIdeMessage` + `IDEFileEditor` handler code paths
  are kept for one release as a rollback switch; the VSIX and the reflection-probed EntryPoint
  contract are untouched (F7).
- **R9 — Diagnosability.** Log + telemetry: batch size, creations count, readiness wait
  duration, readiness timeout fallback, per-file results (mirroring the existing `update-*`
  event pattern).

### Compatibility & rollout

Per F7, the message sender (server processors) and handler (EntryPoint) ship in the same
`Uno.WinUI.DevServer` package: they can never disagree on the contract. The `uno.studio` VSIX
is not modified. Rider / VS Code / CLI are untouched (they use the on-disk editor path and the
dev-server workspace; spec 050 already covers their gating).

### Timeouts

| Stage | Bound | Notes |
|---|---|---|
| Batch write ack (IDE) | existing `SendAndWaitForResult` timeout | ack after writes only (R-ack in step 2) |
| Readiness wait (extension, async) | 10 s default | falls through to trigger + warning |
| Client `ServerUpdateTimeout` | unchanged (10 s default) | unaffected by readiness thanks to the early ack |
| Client `ServerHotReloadTimeout` | unchanged | EnC outcome flows through the operation channel as today |

## Testing & validation

- **Unit (buildable on any OS)**: `FileUpdater` template refactor — existing tests unchanged;
  `IdeFileUpdater` — ordering, delete-split, info-file-before-send, retry arming, message
  shape (mock IDE callback).
- **Manual (Windows/VS)**: the minimal repro from #23840 — create a `.xaml`/`.xaml.cs` pair
  during an active debug session. Success criterion: **no popup**, the view type (incl.
  `InitializeComponent`) applies on the first attempt. Regression: single-file text edit
  (fast path), plain `.cs` add, delete, `NoChanges` retry, readiness-timeout fallback
  (artificially large solution or reduced timeout), and a **mixed batch** (edit of an open
  document + a created pair) to validate the save-deferral discipline — no VS
  hot-reload-on-save apply during the readiness window (race-analysis assumption).
- **Smoke**: Rider / VS Code unchanged behavior (on-disk path).

## Next steps (out of scope here)

- **Real hot-reload status from the VS workspace**: with `VisualStudioWorkspace` now
  accessible from the extension, the EnC outcome reporting could move from diagnostics-log
  hooking to first-class Roslyn/EnC status APIs — a separate work item to spec.
- Removal of the legacy `UpdateFileIdeMessage` path once the batched path has soaked.
- Revisit `ForceHotReloadDelay` for the non-VS paths (the fixed pre-delay remains a blind
  timer there).

## Implementation notes

Deltas vs. the design above (first implementation pass):

- **Batch ack is the standard `IdeResultMessage`** (Success/Fail): the server↔IDE
  request/response plumbing (`SendAndWaitForResult`) is `Result`-typed and shared by every
  correlated message; introducing the typed `UpdateFileResponseIdeMessage` (per-file results) requires
  generalizing that plumbing and is deferred to a follow-up. `IdeFileUpdater` maps the
  single ack onto uniform per-edit results.
- **The shared contracts are single-sourced in `Uno.UI.RemoteControl.Messaging`, in the
  engine namespace `Uno.HotReload.IO`** (review decision: the RC namespace is legacy):
  `IUpdateFileRequest`/`FileEdit`/`FileEditResult`/`FileUpdateResult` compile ONLY into
  Messaging (`UNO_RC_MESSAGING` branch of the existing `#if UNO_HOTRELOAD` dance) and
  `Uno.HotReload` now references Messaging instead of compiling its own copies — one real
  type shared by the engine, the dev-server and the IDE (no duplicate-type aliasing, and the
  engine↔IDE edit mapping disappears entirely). Note: the `Uno.HotReload` package now
  depends on `Uno.WinUI.DevServer.Messaging`.
- **Escape hatch (R8)**: server configuration `hot-reload-ide-updater=false` restores the
  legacy per-file `UpdateFileIdeMessage` + `IDEFileEditor` composition (kept compiled).
- **F9 refinement**: the `Microsoft.VisualStudio.SDK` meta-package does **not** provide
  `Microsoft.VisualStudio.LanguageServices` — an explicit compile-time reference (Roslyn
  4.4 line, matching VS 17.4, `ExcludeAssets="runtime"`) was added to the extension project.
- **Empty-content edits** (`NewText.Length == 0`) are skipped by the IDE batch handler, in
  parity with the legacy single-file handler's `FileContent is { Length: > 0 }` guard.
- `IFileEditor` is documented as a legacy seam (request-level variants must implement/derive
  `IFileUpdater`); `IDEFileEditor` is `[Obsolete]`, suppressed only at the legacy
  escape-hatch composition site.
- The `FileUpdater` template refactor is covered by the existing engine test suite (green);
  dedicated `IdeFileUpdater` unit tests (ordering, delete split, info-file-before-send,
  retry arming) are still to be added.
