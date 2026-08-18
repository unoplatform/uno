# Uno Platform 7.0 — `Uno.dll` → `Uno.WinRT.dll`

> **Status: parked. Do not merge yet.** The change is complete and builds; it is held on
> `dev/mazi/winrt-assembly` until the precondition below is met. Written 2026-08-18.

Completes the rename begun in [`specs/056`](../056-assembly-renames/spec.md), which moved the
folder and csprojs to `Uno.WinRT` but deliberately left `AssemblyName` alone. Background and the
measured blast radius live in 056 under
[When the assembly gets renamed](../056-assembly-renames/spec.md#when-the-assembly-gets-renamed);
this document covers only the mechanics and the gate.

## The merge gate

`Uno.dll` and `Uno.WinRT.dll` are different assembly identities, so **every** binary compiled
against Uno 6.x stops resolving — not only those touching a changed API. The Uno.Sdk references
`Uno.UI.HotDesign` implicitly in **every Debug app build**, and it transitively pulls
`Uno.Toolkit.WinUI` and `Uno.Themes.WinUI`. All of them carry an assembly reference to `Uno`, so
until they have 7.0 builds, every template Debug build fails with `CS0012` on projected types such
as `Windows.UI.Core.CoreDispatcher`.

**Merge only once `Uno.UI.HotDesign` ships a 7.0 preview build**, which per the first-party upgrade
sequencing is the last of the three add-ins to land. At that point the dependents already have 7.0
builds in flight and simply re-roll against a newer preview — ordinary churn, not a flag day.

Merging earlier does not produce a slow failure; it produces a repository that cannot build its own
templates.

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
- Template Debug builds — the gate above.
- Android head (`AndroidResgenNamespace` change) and the mobile PackageDiff.

## Risks

| Risk | Mitigation |
|---|---|
| Merged before HotDesign is on 7.0 | The gate above; the branch stays unmerged, not just unreviewed |
| A missed string literal fails at runtime or trim time, not at build | The checklist above enumerates every known one; treat it as the review checklist |
| Generator tests pass while silently emitting less | Compare generated output counts, do not trust a green run alone |
| Rebase onto a moved `feature/breakingchanges` drops a folded hunk | 056's walk-back showed file renames hide edits; re-run the literal sweep after any rebase |
