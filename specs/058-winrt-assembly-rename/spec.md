# Uno Platform 7.0 — `Uno.dll` → `Uno.WinRT.dll`

> **Status: ready to merge, with the template test jobs temporarily disabled.** Written
> 2026-08-18, unparked 2026-08-24. The original gate below turned out to be circular; how it was
> broken, and what has to be restored afterwards, is in
> [Breaking the deadlock](#breaking-the-deadlock).

Completes the rename begun in [`specs/056`](../056-assembly-renames/spec.md), which moved the
folder and csprojs to `Uno.WinRT` but deliberately left `AssemblyName` alone. Background and the
measured blast radius live in 056 under
[When the assembly gets renamed](../056-assembly-renames/spec.md#when-the-assembly-gets-renamed);
this document covers only the mechanics and the sequencing.

## The dependency problem

`Uno.dll` and `Uno.WinRT.dll` are different assembly identities, so **every** binary compiled
against Uno 6.x stops resolving — not only those touching a changed API. The Uno.Sdk references
`Uno.UI.HotDesign` implicitly in **every Debug app build**, and it transitively pulls
`Uno.Toolkit.WinUI` and `Uno.Themes.WinUI`. All of them carry an assembly reference to `Uno`, so
until they have 7.0 builds, every template Debug build fails with `CS0012` on projected types such
as `Windows.UI.Core.CoreDispatcher`.

This document originally held the change unmerged until `Uno.UI.HotDesign` shipped a 7.0 build.

## Breaking the deadlock

That gate was circular. `Uno.UI.HotDesign` cannot be compiled against `Uno.WinRT.dll` until a
package containing `Uno.WinRT.dll` exists, and no such package is produced until this change is
merged. Waiting for the dependent to move first is waiting for something that cannot happen.

The deadlock is broken by merging the rename with the template test jobs **temporarily disabled**,
in this order:

1. Merge this change; 7.0 dev packages start shipping `Uno.WinRT.dll`.
2. `Uno.UI.HotDesign` — and the dependents beneath it — rebuild against those packages.
3. Re-enable the template test jobs and drop the workarounds listed below.

The cost is explicit and bounded: between steps 1 and 3 the repository does not verify that it can
build its own templates, and a Debug build of a freshly created 7.0-preview app fails until step 2
lands. That is a known gap in a preview line, not a regression that reaches a stable release — and
it is the only ordering in which the chain can move at all.

### What is disabled, and what must come back

Every item carries the greppable marker `TODO Uno (HotDesign 7.0)`:

| File | What was done | Restore condition |
|---|---|---|
| `build/ci/tests/.azure-devops-tests-templates.yml` | `condition: false` on `Dotnet_Template_Tests_NetCoreMobile_windows`, `…_macos` and `Dotnet_Template_Tests_net7_Linux` (12 matrix legs) | `Uno.UI.HotDesign` ships a 7.0 build |
| `build/test-scripts/run-net7-template-linux.ps1` | `-p:UnoDisableHotDesign=true` added to the default switches | idem |
| `build/test-scripts/run-netcore-mobile-template-tests.ps1` | `-p:UnoDisableHotDesign=true` added to the default switches | idem |
| `build/test-scripts/run-netcore-mobile-template-tests.ps1` | `UnoFeaturesOverride` narrowed from `Material;Extensions;Toolkit;CSharpMarkup;Svg;MVUX` to `Svg` | per feature, as each dependent ships a 7.0 build — not necessarily all at once |

The `Dotnet_Tests_Validate_DevServerCli` and `…_Compat` jobs in the same file are deliberately left
running: they restore and exercise the DevServer host rather than compiling app code against the
local build. If the add-in host turns out to fail loading a pre-7.0 `Uno.UI.HotDesign` against a
renamed core, that is the same root cause and belongs to the same follow-up — but it is not assumed
here. Likewise the `addin_version_alignment` stage only walks published add-in `AssemblyRef`
tables and is unaffected by this rename.

Re-enabling is tracked as follow-up work under the 7.0 epic.

## What changes

| Layer | Before | After |
|---|---|---|
| Folder / csprojs | `src/Uno.WinRT` + `Uno.WinRT.*.csproj` | unchanged (done in 056) |
| `AssemblyName` (×4) | `Uno` | **`Uno.WinRT`** |
| Output | `Uno.dll` in `bin/Uno.WinRT.<variant>/` | **`Uno.WinRT.dll`**, same folder |
| NuGet package id | `Uno.WinRT` | unchanged |
| `RootNamespace` | `Windows` | unchanged — the projected API surface must not move |
| `AndroidResgenNamespace` | `Uno.UWP` | **`Uno.WinRT`** — 056 kept the old value because renaming it alone bought nothing; here the whole assembly moves anyway |

`PackageDiffIgnore.xml` carries a single assembly-level entry (`<Assemblies>` → `Uno`), since every
type in the assembly reads as removed against the 6.6 baseline.

## The literals no compiler checks

An assembly identity is referenced **by string** from places the build never type-checks. Each of
these was a separate failure in the August 2026 attempt:

- `Uno.WinRT/ts/…/CoreApplication.ts` — `getAssemblyExports("Uno")`, resolved at **runtime**. Gets
  no compile-time signal; the symptom is a WASM app that never starts.
- `src/Uno.UI/LinkerDefinition.Wasm.xml` — `<assembly fullname="Uno">`; wrong value trims silently.
- `[InternalsVisibleTo("Uno")]` across `Uno.Foundation`, `Uno.Foundation.Logging`,
  `Uno.Foundation.Runtime.WebAssembly`, `Uno.Core.Extensions`, `Uno.UI.Dispatching`,
  `Uno.UI.FluentTheme{,.v1,.v2}`, `Uno.UI.XamlHost`.
- `UnoAssemblyHelper.cs` — takes folder, **assembly file name** and `bin/` subfolders as three
  separate arguments. Only the middle one changes here; 056 changed the other two.
- `Generator.cs` — `expectedRefs` compares `CompilationReference.Display`, which is the *assembly*
  name, not the project name. The neighbouring path literals stay `Uno.WinRT` from 056.
- `build/nuget/Uno.WinRT.nuspec` — the trailing `Uno.dll` / `Uno.pdb` file names (the directory
  segments already moved in 056). `Uno.UI.Dispatching.*` entries in the same file must not change.
- `SamplesApp.Skia.Generic.csproj` — 2 hard-coded output-file paths.
- `TestAssemblyLoadContext.cs`, `RuntimeAssetsSelectorTask.cs`, `Verifiers/CSGenerator.cs`.
- `build/test-scripts/run-net7-template-linux.ps1` and `run-netcore-mobile-template-tests.ps1` —
  keep pre-rename `Uno` packages out of the template builds.

## Validation

Proved locally (Windows, .NET SDK 10.0.301):

- **Compile** — `Uno.WinRT.Skia.csproj` builds and emits
  `bin/Uno.WinRT.Skia/Debug/net10.0/Uno.WinRT.dll`, matching what the nuspec now packs.
- **Compile** — `Uno.UI.csproj` builds clean against the renamed assembly (0 errors).

**Not proved locally, and CI must cover:**

- `Uno.WinAppSDKSyncGenerator` and `Uno.UI.SourceGenerators.Tests` target **net11.0** and cannot be
  built without a .NET 11 SDK. Both carry edits made here. The generator tests are exactly where
  the duplicate-identity trap surfaced last time — a stale pre-7.0 package alongside the local
  build makes every `Windows.*` type resolve twice, and `GetTypeByMetadataName` returns **null on
  ambiguity** rather than erroring, so the XAML generator *silently drops* properties.
- WASM app startup (the `getAssemblyExports` lookup has no compile-time signal).
- Template Debug builds — **not covered at all while the jobs above are disabled**. This is the
  gap that the follow-up closes; until then, the first real signal comes from the HotDesign rebuild.
- Android head (`AndroidResgenNamespace` change) and the mobile PackageDiff.

## Risks

| Risk | Mitigation |
|---|---|
| Template tests stay disabled and quietly rot | Six `TODO Uno (HotDesign 7.0)` markers plus a tracked follow-up; the restore table above is the checklist |
| A 7.0-preview app cannot Debug-build between merge and the HotDesign rebuild | Accepted and bounded — see [Breaking the deadlock](#breaking-the-deadlock); the alternative is a chain that never moves |
| A missed string literal fails at runtime or trim time, not at build | The checklist above enumerates every known one; treat it as the review checklist |
| Generator tests pass while silently emitting less | Compare generated output counts, do not trust a green run alone |
| Rebase onto a moved `feature/breakingchanges` drops a folded hunk | 056's walk-back showed file renames hide edits; re-run the literal sweep after any rebase |
