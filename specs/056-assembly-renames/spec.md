# Uno Platform 7.0 — Assembly renames: `Uno.WinRT` and `Uno.UI.Extras`

> Two independent, binary-breaking assembly renames for the 7.0 major. Each ships as its own
> branch and PR against `feature/breakingchanges`. Part of the breaking-changes rollout
> ([#8339](https://github.com/unoplatform/uno/issues/8339)); the Toolkit rename is
> **BC53** / [#12322](https://github.com/unoplatform/uno/issues/12322).

_Written 2026-08-07._

## Why now

A major release is the only window in which an assembly can be renamed — there is no
non-breaking path, and per the rollout's hard-remove ground rule neither rename ships a
type-forwarder, an `[Obsolete]` alias, or an `xmlns` alias.

Both names are historical artifacts rather than deliberate API choices:

- **`Uno.UWP` / `Uno.dll`** holds the non-UI WinRT surface (`Windows.*` / `Microsoft.*`
  projections for storage, sensors, networking, …). "UWP" predates Uno Platform's WinAppSDK
  alignment, and `Uno.dll` is an uninformatively generic assembly name. The NuGet package is
  **already** `Uno.WinRT`, so this rename closes a gap between package, project, and assembly
  rather than opening a new one.
- **`Uno.UI.Toolkit`** is near-indistinguishable from `Uno.Toolkit.UI`, the separate Uno Toolkit
  product — the two names are a word-order swap apart. Reordering or shortening cannot fix that;
  the word "Toolkit" has to go.

## Decisions (locked)

| Decision | Outcome |
|---|---|
| `Uno.UWP` new name | **`Uno.WinRT`** — folder, csprojs, and `AssemblyName` |
| `Uno.UI.Toolkit` new name | **`Uno.UI.Extras`** — folder, csprojs, `AssemblyName`, root namespace |
| Namespace scope (Extras) | Only `Uno.UI.Toolkit`, `.DevTools.*`, `.Extensions` move |
| `RootNamespace` (WinRT) | Unchanged (`Windows`) — the projected API surface must not move |
| Compatibility shims | **None.** No type-forwarders, no `[Obsolete]` aliases, no `xmlns` alias |
| Downstream repos | **Out of scope.** Tracked separately (see [Out of scope](#out-of-scope)) |
| PackageDiff | Baseline reset, not per-member enumeration |
| PR base branch | `feature/breakingchanges` (not `master`) |

### Why `Uno.UI.Extras`

Keeps the `Uno.UI.*` family prefix alongside `Uno.UI.Composition` and `Uno.UI.Dispatching`, has
zero overlap with `Uno.Toolkit.UI` or `Uno.Extensions.*`, and "Extras" honestly describes the
contents — helpers and attached properties that do not belong in the core.

Rejected alternatives: `Uno.WinUI.Extras` (ties the assembly to one package's name);
`Uno.UI.Helpers` (collides conceptually with the `Uno.Helpers` namespace already inside the same
assembly); `Uno.Xaml.Extras` (`Uno.Xaml` is already the XAML parser's namespace).

### Why the namespace rename stops where it does

The assembly contains seven namespaces, only three of which are confusable:

| Namespace | Files | Action |
|---|---|---|
| `Uno.UI.Toolkit` | 7 | → `Uno.UI.Extras` |
| `Uno.UI.Toolkit.DevTools.*` | 8 | → `Uno.UI.Extras.DevTools.*` |
| `Uno.UI.Toolkit.Extensions` | 1 | → `Uno.UI.Extras.Extensions` |
| `Uno.Diagnostics.UI` | 8 | unchanged |
| `Uno.UI.Markup` | 2 | unchanged |
| `Uno.Helpers` | 2 | unchanged |
| `Uno.UI`, `Uno.UI.Maps` | 2 | unchanged |

Moving the other four would break `DiagnosticsOverlay`, `FromJsonExtension`, `ColorExtensions`,
and `ViewHelper` consumers for no clarity gain — none of those names are confusable with Uno
Toolkit. The assembly name and root namespace matching exactly is not worth the extra breakage.

---

## Part A — `Uno.UWP` → `Uno.WinRT`

### Rename

| What | From | To |
|---|---|---|
| Folder | `src/Uno.UWP` | `src/Uno.WinRT` |
| Projects | `Uno.{Skia,Reference,Wasm,netcoremobile}.csproj` | `Uno.WinRT.{…}.csproj` |
| `AssemblyName` (×4) | `Uno` | `Uno.WinRT` |
| Output | `Uno.dll` | `Uno.WinRT.dll` |
| `RootNamespace` | `Windows` | `Windows` (unchanged) |
| NuGet package id | `Uno.WinRT` | `Uno.WinRT` (already correct) |

### Consequences that are not the obvious find-and-replace

Output paths are `bin/<ProjectName>/`, so renaming the csprojs **moves every build output
directory**. Everything that hard-codes one of those paths breaks:

- **26 `<file src>` paths** in `build/nuget/Uno.WinRT.nuspec`
  (`src\Uno.UWP\Bin\Uno.Skia\…\Uno.dll` → `src\Uno.WinRT\bin\Uno.WinRT.Skia\…\Uno.WinRT.dll`)
- `SamplesApp.Skia.Generic.csproj` (2 hard-coded `…\Uno.Skia\…\Uno.dll` item paths)
- **5 `_AdjustedOutputProjects` entries** in `src/Directory.Build.props`. Note two of these
  (`Uno.Tests.csproj`) are already stale — no such project exists. Drop them rather than rename
  them.

Assembly *identity* is referenced by literal string in several places, none of which the
compiler will catch:

- **9 × `[assembly: InternalsVisibleTo("Uno")]`** — two under `Uno.Foundation`
  (`AssemblyInfo.cs` and `Uno.Core.Extensions/AssemblyInfo.cs`), plus
  `Uno.Foundation.Logging`, `Uno.Foundation.Runtime.WebAssembly`, `Uno.UI.Dispatching`,
  `Uno.UI.FluentTheme`, `.v1`, `.v2`, and `Uno.UI.XamlHost`
- `src/Uno.UI/LinkerDefinition.Wasm.xml` — `<assembly fullname="Uno">`
- `Uno.UI.RuntimeTests/.../TestAssemblyLoadContext.cs` — `name.Equals("Uno", …)`
- `build/PackageDiffIgnore.xml` — `<Member fullName="Uno" …>`

The **sync generator** routes generated WinRT stubs by hard-coded path and assembly name
(`src/Uno.WinAppSDKSyncGenerator/`):

- `Generator.cs` — 3 × `..\..\..\Uno.UWP\Generated\3.0.0.0` output paths
- `Generator.cs:186` — `var platformProject = @"..\..\..\Uno.UWP\Uno";` (project path prefix,
  suffixed per variant)
- `Generator.cs:536` — `basePath.Contains(@"\Uno.UWP\", …)` platform-vs-Skia discriminator
- `Generator.cs:2346` — assembly list `["Uno.Foundation", "Uno", "Uno.UI.Composition", …]`
- `Program.cs:19` — `DeleteDirectoryIfExists(@"..\..\..\Uno.UWP\Generated\")`

Plus ~30 `ProjectReference` paths across the tree and 6 `src/Uno.UI.slnx` entries.

### Sequencing constraint

This rename is a **prerequisite** for the sync-generator API-relocation work, which relocates
namespaces *into* this assembly. Applying the name afterwards would mean redoing that
relocation. Part A therefore lands first.

---

## Part B — `Uno.UI.Toolkit` → `Uno.UI.Extras`

### Rename

| What | From | To |
|---|---|---|
| Folder | `src/Uno.UI.Toolkit` | `src/Uno.UI.Extras` |
| Projects | `Uno.UI.Toolkit.{Skia,Reference,Windows}.csproj` | `Uno.UI.Extras.{…}.csproj` |
| `AssemblyName`, `RootNamespace` | `Uno.UI.Toolkit` | `Uno.UI.Extras` |
| Namespaces | `Uno.UI.Toolkit`, `.DevTools.*`, `.Extensions` | `Uno.UI.Extras`, `.DevTools.*`, `.Extensions` (16 files) |
| NuGet package | ships inside `Uno.WinUI` | unchanged (paths only) |

~250 references outside the project folder, concentrated in `Uno.UI.RuntimeTests` (48 files),
`SamplesApp` (34), `SourceGenerators` (29), and `doc/articles` (14).

### Consequences that are not the obvious find-and-replace

**Generated XAML output is checked in.** 19 golden files under
`SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeGeneratorTests/Out/**` contain
`global::Uno.UI.Toolkit.GlobalStaticResources` and `xmlns:toolkit="using:Uno.UI.Toolkit"`. These
are expected-output baselines and must be **regenerated**, not hand-edited — hand-editing would
make the tests pass while proving nothing about the generator.

**The Windows/WinAppSDK head derives identity from the assembly name.** Only that head produces:

- `Uno_UI_Toolkit_XamlTypeInfo.XamlMetaDataProvider` → becomes `Uno_UI_Extras_XamlTypeInfo`
- `Uno.UI.Toolkit.pri` → `Uno.UI.Extras.pri`, checked for existence by a hard-coded path in
  `build/Uno.UI.Build.csproj:194` which errors out by name
- `Uno.UI.Toolkit.Resource` (Android resource designer type)

All three appear across **10 lines** of `build/PackageDiffIgnore.xml`.

**Other spots:** `LinkerDefinition.net6.0.xml` is embedded as `$(AssemblyName).xml` so it follows
automatically; `Themes/Generic.xaml` declares `xmlns:toolkit`; ~20 paths and 3
`<reference file="Uno.UI.Toolkit.dll" />` entries in `build/nuget/Uno.WinUI.nuspec`; 4
`_AdjustedOutputProjects` entries; 3 `Uno.UI.slnx` entries plus the `/Uno.UI/Toolkit/` solution
folder name.

---

## Out of scope

- **Downstream repositories.** Roughly 172 files across the samples, Toolkit, Themes, Gallery,
  Chefs, Extensions, templates, and Studio repositories reference `Uno.UI.Toolkit`. They adopt
  the new name when they bump to 7.0 — the point at which their builds would break regardless.
  Existing tracking items already cover the Uno Toolkit 7.0 update and the external-doc commit
  pin bumps; no new tracking work is created here.
- **Merging `Uno.UI.Extras` into `Uno.UI`.** Not possible: the assembly has a Windows/WinAppSDK
  head, and `Uno.UI` does not exist on that target. The library remains separate by design.
- **The other five namespaces** inside `Uno.UI.Extras` (see table above).
- **Renaming `Uno.UI.Dispatching`**, which also ships inside the `Uno.WinRT` package. Judged on
  its own merit, separately.

---

## Delivery

| Order | Branch | PR base | Tracked as |
|---|---|---|---|
| 1 | `dev/mazi/uwp-ditch` | `feature/breakingchanges` | internal 7.0 tracking item |
| 2 | `dev/mazi/toolkit-extras` | `feature/breakingchanges` | BC53 / [#12322](https://github.com/unoplatform/uno/issues/12322) |

Part A first, because the sync-generator relocation work depends on the final name.

The two changes are file-independent except for four shared files — `src/Directory.Build.props`,
`src/Uno.UI.slnx`, `build/PackageDiffIgnore.xml`, and the Extras csprojs' `ProjectReference` to
`..\Uno.UWP\…`. Both branches can therefore be developed in parallel; the Extras branch rebases
once Part A merges.

Commit in logical groups that each build clean, rather than one squashed rename:

1. `git mv` the folder (pure move, no content edits — keeps rename detection intact)
2. Rename csprojs + `AssemblyName` / `RootNamespace`
3. Fix `ProjectReference`s, `Uno.UI.slnx`, `_AdjustedOutputProjects`
4. Assembly-identity string literals (`InternalsVisibleTo`, linker XML, ALC helper)
5. Namespace + `xmlns` sweep (Part B only), then regenerate golden files
6. `.nuspec` paths, `PackageDiffIgnore.xml` baseline reset
7. Sync-generator routing (Part A only)
8. Docs + migration guide

`specs/050-breaking-changes-rollup/spec.md` gets BC53 checked off, and a new line added for the
`Uno.WinRT` rename.

---

## Validation

`feature/breakingchanges` targets .NET 11 and there is no local .NET 11 SDK, so all local builds
pass `-p:UnoTargetFrameworkOverride=net10.0` (plus `-p:UnoFastDevBuild=true` for iteration).

| # | Check | Why it is not optional |
|---|---|---|
| 1 | Build `Uno.UI-Skia-only.slnf` | Baseline compile across the renamed graph |
| 2 | `Uno.UI.UnitTests` | Regression floor |
| 3 | **`SourceGenerators.Tests` run separately** | The only thing that exercises the 19 golden files; `Uno.UI.UnitTests.csproj` alone does not cover them |
| 4 | **Windows/WinAppSDK head build** (Part B) | `Uno_UI_Extras_XamlTypeInfo` and `Uno.UI.Extras.pri` exist only there, and `Uno.UI.Build.csproj:194` fails by filename. Requires `MSBuild.exe`, not `dotnet build` |
| 5 | Runtime tests, Skia Desktop | 48 RuntimeTests files consume the renamed namespaces (input injection via `DevTools`) |
| 6 | **Sync-generator round-trip** (Part A) | Re-run and confirm `Generated/` returns byte-identical under the new path — otherwise four hard-coded strings are unverified |
| 7 | PackageDiff | Expected to flag every type in both assemblies; handled as a baseline reset |
| 8 | ~~WASM head build for `LinkerDefinition.Wasm.xml`~~ | **Dropped.** The file is dead: `Uno.UI` has no WASM csproj after the native drop, and nothing embeds it. Its `<assembly fullname>` entry is updated for consistency only, and the file is a deletion candidate in its own right. |

Checks 3, 4, and 6 are the ones that fail *silently* — a green Skia build proves none of them.

### Duplicate assembly identity (found during Part A)

Renaming an assembly means the old and new names are **different identities**, so both can be
referenced by one compilation. Every `Windows.*` type is then defined twice, and Roslyn's
`Compilation.GetTypeByMetadataName` returns null on ambiguity rather than erroring — which made
the XAML generator *silently drop* literal and extended properties from its output.

This is not hypothetical: it broke 8 source-generator tests, because they reference a pre-7.0
`Uno.WinUI` package (which still ships `Uno.dll`) on top of the local build. Before the rename the
identical identity meant the local assembly simply shadowed the package's. The fix drops the
superseded package reference in the test harness.

The same failure mode reaches real consumers: a 7.0 app that transitively pulls a library compiled
against Uno 6.x gets duplicate `Windows.*` types. Recompiling every dependent library against 7.0
is the only remedy, and the migration guide says so explicitly.

## Risks

| Risk | Mitigation |
|---|---|
| A missed string literal (linker XML, ALC, PRI check) compiles fine and fails at runtime or trim time | Checks 4, 6, 8 above target exactly these; enumerate the literals from this spec as a checklist |
| Stale `bin/obj` under the old folder names masks path errors | Clean the old `src/Uno.WinRT` / `src/Uno.UI.Toolkit` output trees before validating |
| Conflict with in-flight sync-generator relocation work | Part A lands first, by design |
| Golden files hand-edited instead of regenerated | Regeneration is a named step; review the diff for generator-shaped output |
| PackageDiff baseline reset hides an unintended API change | Reset scoped per renamed assembly, not repo-wide |
