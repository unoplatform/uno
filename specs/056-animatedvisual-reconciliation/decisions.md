# AnimatedVisualPlayer reconciliation — provenance & decision log

Two branches independently implemented overlapping work: syncing `AnimatedVisualPlayer` to current
WinUI sources, adding missing Composition features, and reworking Uno Platform Lottie support. This
log records, for every reconciled unit, **which source was chosen and why**, so the merged branch can
be reviewed against its rationale rather than re-litigated.

## Branches

| Alias | Ref | Commits | Notes |
|-------|-----|---------|-------|
| **A** | `dev/mazi/animatedvisual` | 19 | Composition-engine-heavy; ProgressRing via generated visuals; LottieGen playground |
| **B** | `origin/agents/animated-visual-player-port` | 14 | Claimed 1:1 `AnimatedVisualPlayer` WinUI port; Lottie add-in rework |

Both are based on `96019901b87` (tip of `origin/feature/breakingchanges`) — branch A was rebased onto
it on 2026-08-12 specifically so the two change-sets are directly comparable.

## Adjudication rule

Per the porting rules in `/winui-port`, the merged result must be **as close as possible to the WinUI
C++ sources** — 1:1 file mapping, member order following the C++, natural WinUI names (never a `_Mux`
suffix), accurate `// MUX Reference` header lines, `// TODO Uno:` divergence markers preserved, and no
"simplification" of WinUI behavior. **WinUI fidelity beats elegance.** Where neither branch matches the
C++, the correct shape is written fresh rather than picking the lesser deviation.

Ground truth: `D:\Work\microsoft-ui-xaml2\controls\dev\AnimatedVisualPlayer\` (`AnimatedVisualPlayer.cpp`,
`.h`, `.idl`, `AnimatedVisualPlayerAutomationPeer.*`), `controls\dev\Generated\AnimatedVisualPlayer.properties.*`,
and `specs\AnimatedVisualPlayer Spec.md`.

## Hard requirement

The merged PR must support **both** kinds of Lottie:

1. **`.json` runtime Lottie** — a Lottie JSON parsed and rendered at runtime (Skottie on Skia).
2. **Composition/LottieGen Lottie** — generated C# building Composition visuals, consumed through
   `IAnimatedVisualSource` / `IAnimatedVisualSource3` / `IAnimatedVisual`.

---

## Decisions

### Rebase decisions (branch A onto `feature/breakingchanges`, 2026-08-12)

These predate the A-vs-B reconciliation; they resolve branch A against *upstream*, not against B.

| # | Unit | Source chosen | Why |
|---|------|---------------|-----|
| R1 | `Generated/.../PathKeyFrameAnimation.cs`, `ColorKeyFrameAnimation.cs` | **Rewritten** to `#if false` + `// Skipping already declared …` | Upstream removed the Reference flavor, so `__NETSTD_REFERENCE__` is dead; branch A's stubs kept it alive. Both members are implemented on Skia, so the generator's canonical output for a fully-implemented type is `#if false`. Copied the idiom verbatim from the sibling `ScalarKeyFrameAnimation.cs` / `BooleanKeyFrameAnimation.cs` in the new base. |
| R2 | `Generated/.../AnimationController.cs` (`PlaybackRate`, `Min/MaxPlaybackRate`) | **A** | A implements all three; upstream still stubs them. Implemented ⇒ `// Skipping already declared property`. |
| R3 | `Generated/.../Compositor.cs` (`CreateColorKeyFrameAnimation`, `CreatePathKeyFrameAnimation`, `CreateAnimationController`) | **A** | Same rule. `Dispose()` stays stubbed, narrowed from `__SKIA__ \|\| __NETSTD_REFERENCE__` to `__SKIA__`. |
| R4 | `CompositionSpriteShape.StrokeDashArray` | **Merged (A getter + upstream setter)** | A made the getter lazily non-null (`??= new CompositionStrokeDashArray(Compositor)`) because LottieGen-generated code calls `StrokeDashArray.Add(...)` with no null check. Upstream independently narrowed the setter to `internal` for WinUI parity — WinUI exposes `StrokeDashArray` as read-only. Both are right; kept A's getter and upstream's `internal set`. |
| R5 | `Shape.skia.cs` → `Shape.cs` | **A's hunk carried into the fold survivor** | Upstream commit `f782be39b1d` folded `.skia.cs` partials into their base files. This is a *fold*, not a drop, so A's `UpdateStrokeDashArray` hunk had to move to `Shape.cs`. It is also load-bearing: with R4's non-nullable property, upstream's `_shape.StrokeDashArray = null;` no longer compiles — A's `.Clear()` rewrite is required. |
| R6 | `refactor(win2d): Extract Microsoft.Graphics.Canvas into Uno.WinUI.Graphics.Win2D` | **Dropped from this branch** | Out of scope per the user; it also collides broadly with the new csproj topology (`Uno.UI.Reference.csproj` deleted, `Uno.UI.Skia.csproj` → `Uno.UI.csproj`) and needs redoing against it. Preserved at `refs/backup/animatedvisual-prerebase` (`45be3c2bfd9`) for a standalone follow-up PR. |

### Reconciliation decisions (A vs B)

**The full per-unit provenance table lives in [`plan.md` §3](./plan.md#3-provenance-log)** — one row per
unit, each with the chosen source (A / B / hand-merged / rewritten), a WinUI C++ citation, and the
rationale. The integration steps are §2 and the validation gates are §7.

Headline finding: **neither branch works on its own.** A owns the Composition engine that makes real
LottieGen output run; B owns the faithful control port and the `.json` Lottie path. The merged branch is
**A's engine under B's control**, plus five hand-merged files and four fresh corrections.

| | A alone | B alone | Merged |
|---|---|---|---|
| `.json` runtime Lottie (Skottie) | broken | supported | **supported** |
| Composition / LottieGen Lottie | supported | broken | **supported** |

Decision summary by area:

| Area | Source | One-line reason |
|------|--------|-----------------|
| `AnimatedVisualPlayer` control body, header, DPs, automation peer | **B** | Genuine 1:1 port (1241 lines in C++ member order + companion `.h.mux.cs`); A's 816-line version is mostly the deletion of a legacy gate. |
| AVP type declaration | **B** | WinUI is `: FrameworkElement` (`AnimatedVisualPlayer.idl:74-75`); A's `: Panel` widens public API. `Panel` in WinUI is only `DeriveFromPanelHelper_base` for fallback-content children (`.h:15-16`). |
| Interfaces (`IAnimatedVisual2`, `IAnimatedVisualSource3`, `IDynamicAnimatedVisualSource`) | **B** | Shipped `.idl` signatures; A consumed `[NotImplemented]` stubs. |
| `Generated/IAnimatedVisualSource.cs` | **Rewritten** | Neither is right — B tags a working interface `[NotImplemented]`; correct fix is hand-declare + regenerate. |
| Composition expression engine, keyframe types, gradient + geometry fixes | **A** | 28 function specifications, `Color`/`PathKeyFrameAnimation`, TrimOffset, 12-o'clock ellipse. B has none of it; without it LottieGen output throws. |
| `KeyFrameEvaluator` / `IKeyFrameEvaluator` | **B** | Strict superset — adds `Seek(float)` and `IsPaused`, both required by the `SeekAnimation` fix. |
| `KeyFrameAnimation.SetPlaybackRate` | **A** | B is null-forgiving and drops a rate set before `Start()`. |
| `CompositionObject.SeekAnimation` | **B** | A pauses and re-evaluates but never moves the playhead, so scrub-then-resume jumps back. **Auto-merges silently** — the highest-value fix a naive merge would drop. |
| `AnimationController.Min/MaxPlaybackRate` | **Rewritten** to ±16 | A invented ∓1e6f; B used `float.Min/MaxValue`, making its own clamp a no-op. Documented Windows range is −16..16. |
| `Uno.UI.Lottie` add-in (`.json` path) | **B** | A's entire footprint is one line, which B's rewrite deletes. B removes the parallel `DispatcherQueueTimer` clock that made the 1:1 port impossible. |
| `LottieVisualSource.reference.cs` | **Dropped** | Dead code — the csproj is `UnoRuntimeIdentifier=Skia`, and `Uno.CrossTargetting.targets:23` removes `**\*.reference.cs`. |
| `GetAppDataPath` | **Rewritten** | Route through the shared `AppDataUriEvaluator`, keeping B's containment check; a second URI parser is a maintenance trap. |
| `FrameworkElement.IsViewHit()` | **Rewritten** (scoped to AVP) | B's repo-wide hit-test change has no regression evidence and doesn't belong in a control PR. |
| `m_playDuration` | **Hand-merged** | A's ctor computation matches WinUI (`cpp:22-27`); recompute in `Start()` only when it was zero, because Uno's sources load asynchronously. Marked `// TODO Uno:`. |
| `ProgressRing` via generated visuals | **A** | Also drops the `Uno.UI.Lottie` add-in requirement — call out in release notes. |
| `Uno.WinAppSDKSyncGenerator/Generator.cs` | **B** | **Prerequisite for A's stub edits** — without it the generator resolves composition types against `Uno.UI`, which doesn't contain them. |

Three claims were independently re-verified before acceptance (agent citations are not taken on trust):

1. A's `AnimationPlay.Complete()` pins `InsertScalar("Progress", _toProgress)`, so `Stop()` parks at the
   **end** instead of the start. WinUI `cpp:771-780` reads *"Stop the animation by setting the Progress
   value to the fromProgress of the most recent play."* B has no such write. **Confirmed.**
2. `CompositionObject.SeekAnimation` diverges (A `PauseAnimation`, B `animation.Seek`). **Confirmed.**
3. `git merge-tree` reports exactly 7 content conflicts, and `CompositionObject.cs` is **not** among them
   — it auto-merges, which is what makes decision 2 dangerous. **Confirmed.**

---

## Out of scope / follow-ups

| Item | Why split out |
|------|---------------|
| Win2D extraction into `Uno.WinUI.Graphics.Win2D` | Independent refactor; needs redoing against the post-Reference-collapse csproj topology. `refs/backup/animatedvisual-prerebase`. **Sequencing constraint:** this PR makes 10 `Microsoft.Graphics.Canvas` types public in `Uno.UI.Composition` (so unmodified `LottieGen -Public` output compiles); the extraction then moves them to another assembly. Shipping them in *different* releases is binary-breaking for anyone who bound to the intermediate state — ship both in the same release and say so in the PR description. |
| Generalise `FrameworkElement.IsViewHit() => HasCompositionChildVisual` | Defensible framework-wide, but needs its own pointer/hit-test regression run rather than riding along in a control PR. |
| `CompositionVisualSurface` clip height (`Size.X` → `Size.Y`) | Unambiguous two-line bug fix with no Lottie dependency — worth an independent cherry-pick to `master`/`stable` so it isn't gated on a breaking-change PR. |
| Encoded-appdata-traversal fix | The vulnerable path exists on `master` today; if this PR slips, the hunk should land alone. |
| `buildTransitive/Uno.WinUI.Lottie.targets` stale gate | `_ValidateLottieDependencySkia` hard-errors unless the head references both `SkiaSharp.Views.Uno.WinUI` and `SkiaSharp.Skottie`; the rework renders via `Uno.WinUI.Graphics2DSK`, so the `Views` half may now be an unnecessary consumer burden. Inferred from reading the targets file — **not build-validated.** |

---

## Open items

- Cleanup noted during the rebase: `#if __SKIA__` guards inside `Uno.UI.Composition` are now always-true
  dead code (the assembly builds Skia-only after the Reference collapse). Not churned during the rebase;
  worth a sweep.
