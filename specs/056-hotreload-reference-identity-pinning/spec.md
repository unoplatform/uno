# Hot-Reload: Pin Re-Bound Metadata References to the Session Baseline for the EnC Emit

**Repo**: `uno` (Uno.HotReload)
**Created**: 2026-08-10
**Status**: Resolved — implemented on this branch
**Related**: [#24023](https://github.com/unoplatform/uno/issues/24023) (the report),
[#24012](https://github.com/unoplatform/uno/pull/24012) (sibling Roslyn-5.6 fallout: generator-error
emit gate), [spec 054](../054-hotreload-audit-changeset-scope/spec.md) (the audit scope the aligned
solution now feeds)

## Overview & Objectives

Adding a `PackageReference` and using one of its types in XAML, in a single coalesced hot-reload
change set, degenerates to a rude edit with **zero deltas** on the Roslyn 5.6 line (SDK
`6.8.0-dev.x`, also stable 6.7 for net10 hosts) while it produced a working delta on Roslyn 4.14
(`6.7.0-dev.914`). Package resolution and staging succeed (`packages=True` — the assemblies ship);
the failure is entirely in the EnC emit.

Root cause (verified by decompiling `Microsoft.CodeAnalysis.Features` 5.6.0 and by real-EnC
tests): `EditSession.EmitSolutionUpdateAsync` now runs `HasReferenceRudeEdits`, comparing the
emitted compilation's `ReferencedAssemblyNames` against the `InitiallyReferencedAssemblies` the
project's EnC baseline captured (the baseline compilation's full reference identity set, frozen at
the module's first emit). Any already-known simple name appearing at a **different identity**
reports `ENC1099` ("Changing project or package reference caused the identity of referenced
assembly to change from '{0}' to '{1}'…", Error) and the project is skipped — the shim then
returns zero updates with the diagnostics. Referencing a **brand-new** simple name is explicitly
supported (`HasAddedReference` → `projectsToRedeploy`, delta emitted). Roslyn 4.14 had no
reference checks at all.

A mid-session package add hits the unsupported half routinely: the updater (Studio Live's
`AdhocSolutionUpdater` → `MsBuildNugetWorkspaceBinder.Bind`) binds **every** assembly of the
resolved closure onto the project, and real closures overlap the app's built graph — e.g.
`Mapsui.Uno.WinUI 5.0.2` resolving SkiaSharp 3.x against an app built on SkiaSharp 4.148, or a
released `Uno.WinUI` against a dev-build app. One overlapped identity kills the whole cycle.

Objective: restore the 4.14-era capability — a package add during hot reload produces a delta and
the new control materializes — without fighting Roslyn: make the emitted solution *agree* with the
baseline on every already-known reference identity.

## Design

`HotReloadManager` snapshots, at construction, each project's file-backed
`PortableExecutableReference`s keyed by assembly simple name (the file name — identical for every
build/NuGet asset). Before every emit, `WithBaselineReferenceIdentities` rewrites the updater's
solution: a PE reference whose simple name exists in the snapshot but whose path differs is
replaced by the **baseline instance** (added-alongside duplicates collapse to one occurrence);
new names and non-PE references flow through untouched. The emitted delta therefore binds the
identities the running application actually loaded — which is also the only thing the runtime
can resolve without a restart.

Decisions and their reasons:

- **Emit-scoped, not committed to `CurrentSolution`** — the manager keeps the updater's faithful
  result (spec 045 §2: rebinds must persist); pinning happens at the same seam as the
  generator-error suppression (#24012). EnC commits the *pinned* snapshot; the next cycle re-pins
  deterministically, so committed-vs-emitted reference identities always agree (guarded by the
  two-cycle E2E test).
- **Audits run on the aligned solution** — `ResolveAuditProjects` / generator-error collection /
  the blocked-compilation audit all see the graph the emit compiles. Only the pinned fork can
  explain a pin-induced compile failure (an edit using v2-only API fails against pinned v1 —
  a real error the user must see).
- **Multi-version baseline names are excluded** from pinning (surfaced as Verbose at session
  start): pinning cannot pick a side; Roslyn owns that case (ENC1098).
- **Reporting**: pin events print an Output summary naming up to 3 assemblies ("…at the identity
  the application was built with; changing a referenced assembly requires a rebuild") only when
  the pin set *changes* — the re-bind persists, so an unchanged set would repeat verbatim every
  cycle for the rest of the session. Per-pin old→new paths print at Verbose on every cycle. A
  pin-free emit clears the dedup so a revert→re-add reports again.
- **`HotReloadUpdate.PinnedReferences`** exposes the pinned set to handlers: deltas bind the
  baseline files, NOT the conflicting ones, so a handler staging assembly files must not
  overwrite a baseline file with a same-named conflicting one (see Residual risks).

## Requirements

- **R1** — Referencing a brand-new assembly mid-session emits a delta (Roslyn-supported path,
  canary: `When_NewAssemblyReferenceAdded_Then_UpdateIsEmitted`).
- **R2** — Roslyn 5.x blocks a same-name identity change with ENC1099 at the shim level
  (canary documenting the raw behavior — if a Roslyn update relaxes it, revisit the pinning:
  `When_ReferencedAssemblyIdentityChanges_Then_EmitIsBlockedWithENC1099`).
- **R3** — A package add whose closure re-binds an already-referenced assembly still hot
  reloads end-to-end: Success, one delta, no ENC errors, pin surfaced on the update and named
  at Output (`When_PackageAddRebindsExistingAssembly_Then_UpdateIsStillEmitted`).
- **R4** — The re-bind persisting in `CurrentSolution` while EnC committed the pinned solution
  is stable across cycles: a later plain edit re-pins and emits; the Output summary does not
  repeat for an unchanged pin set (`When_RebindPersistsAcrossCycles_Then_NextEditStillEmits`).
- **R5** — Pinning primitives: replace → pinned back; added-alongside → collapses to one
  baseline occurrence; new name → untouched (same solution instance); multi-version baseline
  name → excluded and surfaced; unknown project → untouched; names match case-insensitively
  (`Given_ReferenceIdentityPinning`).

## Residual risks / follow-ups

- **Staging overwrite (studio.live)** — `AdhocHotReloadProcessor` stages resolved package files
  with an unconditional write into the running app's output directory; a conflicting same-named
  file can overwrite the baseline file the deltas bind against (loaded assemblies are unaffected,
  but a relaunch starts from the downgraded file). Pre-fix the cycle died in ENC1099 (staging
  still ran); post-fix the session is otherwise healthy, so the divergence is silent.
  `HotReloadUpdate.PinnedReferences` carries what staging must skip; the exclusion belongs in
  studio.live's handler chain.
- **Reference-only breakage audit** (pre-existing, spec 054 scope) — a csproj-only cycle that
  *removes* a still-used assembly completes as NoChanges (the audit scope matches documents
  only); the compile error surfaces on the next document edit. Folding `ChangeSet.EditedProjects`
  into the audit scope is a follow-up.
- **Same-path identity drift** — a reference rebuilt in place (same path, new `AssemblyVersion`)
  is invisible to path-keyed pinning and still ENC1099s (a genuine rebuild scenario); the raw
  rude edit is reported. Mapping ENC1099 to the friendlier rebuild guidance is a possible
  refinement.
