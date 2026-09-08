# Uno Platform 7.0 — Project move to `src/Uno.WinRT`, assembly rename to `Uno.UI.Extras`

> Two independent changes for the 7.0 major. Each ships as its own branch and PR against
> `feature/breakingchanges`. The Toolkit rename is part of the breaking-changes rollout
> ([#8339](https://github.com/unoplatform/uno/issues/8339)) as
> **BC53** / [#12322](https://github.com/unoplatform/uno/issues/12322); the WinRT project move
> is not a breaking change and carries no rollout item.

_Written 2026-08-07. Part A revised 2026-08-10, then again 2026-08-18 — see
[When the assembly gets renamed](#when-the-assembly-gets-renamed)._

## Why now

A major release is the only window in which an assembly can be renamed — there is no
non-breaking path, and per the rollout's hard-remove ground rule the Toolkit rename ships no
type-forwarder, `[Obsolete]` alias, or `xmlns` alias.

Both names are historical artifacts rather than deliberate API choices:

- **`Uno.UWP`** holds the non-UI WinRT surface (`Windows.*` / `Microsoft.*` projections for
  storage, sensors, networking, …). "UWP" predates Uno Platform's WinAppSDK alignment, and the
  folder was already the only library in the tree whose name disagreed with its csprojs
  (`Uno.UWP/Uno.Skia.csproj` against the `Uno.Foundation/Uno.Foundation.Skia.csproj` pattern).
  Moving it to `src/Uno.WinRT` drops the dead name and makes folder, csproj and package agree.
  It also lands the project on its eventual assembly name, so the identity rename that follows is
  a one-line change rather than a second folder move.
- **`Uno.UI.Toolkit`** is near-indistinguishable from `Uno.Toolkit.UI`, the separate Uno Toolkit
  product — the two names are a word-order swap apart. Reordering or shortening cannot fix that;
  the word "Toolkit" has to go.

## Decisions (locked)

| Decision | Outcome |
|---|---|
| `Uno.UWP` folder + csprojs | **`src/Uno.WinRT`** + `Uno.WinRT.{Skia,Reference,Wasm,netcoremobile}.csproj` |
| `Uno` `AssemblyName` | **Unchanged in this change.** Stays `Uno.dll`; renamed to `Uno.WinRT` later in the 7.0 preview line — see below |
| `Uno.UI.Toolkit` new name | **`Uno.UI.Extras`** — folder, csprojs, `AssemblyName`, root namespace |
| Namespace scope (Extras) | Only `Uno.UI.Toolkit`, `.DevTools.*`, `.Extensions` move |
| `RootNamespace` (WinRT) | Unchanged (`Windows`) — the projected API surface must not move |
| Compatibility shims | **None** for the Extras rename. No type-forwarders, `[Obsolete]` aliases, or `xmlns` alias |
| Downstream repos | **Out of scope.** Tracked separately (see [Out of scope](#out-of-scope)) |
| PackageDiff | Part A: no delta. Part B: baseline reset, not per-member enumeration |
| PR base branch | `feature/breakingchanges` (not `master`) |

### When the assembly gets renamed

The rename to `Uno.WinRT.dll` **is happening in 7.0** — but not in this change, and not first. It
was implemented in August 2026 and reverted; this section records why, and what has to be true
before it is re-applied.

`Uno.dll` and `Uno.WinRT.dll` are different assembly identities, so every binary compiled against
Uno 6.x stops binding. That alone is not what makes it expensive: 7.0 breaks compatibility by
design, and the first-party ecosystem is being rebuilt for it regardless. What makes it expensive
is **ordering** — this repository's CI must go green before the dependents that consume its output
can be rebuilt against it.

Three failure classes surfaced. Two are one-line fixes:

| Failure | Cost |
|---|---|
| WASM `getAssemblyExports("Uno")` — a runtime lookup no compiler checks | one line in `CoreApplication.ts` |
| XAML generator resolving duplicate `Windows.*` types — `GetTypeByMetadataName` returns null on ambiguity, so properties are *silently* dropped | drop the stale package reference from generator test fixtures |
| **Template Debug builds fail with `CS0012`** | **structural — see below** |

The Uno.Sdk references `Uno.UI.HotDesign` implicitly in **every Debug app build**, which
transitively pulls `Uno.Toolkit.WinUI` and `Uno.Themes.WinUI`. Metadata read of
`Uno.Toolkit.WinUI` 6.3.0-dev.6 (net8.0), counting type references per target assembly:

| Binary | → `Uno` | → `Uno.UI` | → `Uno.UI.Toolkit` |
|---|---|---|---|
| `Uno.Toolkit.WinUI.dll` | 18 | 254 | 2 |
| `Uno.Toolkit.WinUI.Material.dll` | 2 | 125 | 2 |
| `Uno.Toolkit.Skia.WinUI.dll` | 4 | 71 | 1 |
| `Uno.Toolkit.WinUI.Cupertino.dll` | 2 | 70 | 2 |

The 18 include `Windows.UI.Color`, `Windows.UI.Text.FontWeight`, `Windows.System.VirtualKey` and
`Windows.UI.Core.CoreDispatcher` — types that appear throughout public signatures. **This is the
difference between an API removal and an assembly rename: a removal breaks a pre-built consumer
only if that consumer touches the removed member, while a rename makes the assembly reference
itself unresolvable, so every consumer fails unconditionally.**

#### What must be true before re-applying it

7.0 has not shipped and previews publish continuously, so the rename is sequenced *inside* the 7.0
preview line rather than deferred to a later major:

1. The first-party upgrade waves put `Uno.Toolkit`, `Uno.Themes` and — critically —
   `Uno.UI.HotDesign` onto 7.0 preview builds. HotDesign lands last of the three, and it is the one
   the Uno.Sdk pulls into every Debug build.
2. Only then is the rename re-applied. Every dependent already has a 7.0 build in flight at that
   point and simply re-rolls against a newer preview — ordinary preview churn, not a flag day.

Deferring to 8.0 instead would buy a **second** ecosystem-wide rebuild for a change that is purely
cosmetic, which is strictly worse than paying for it inside a rebuild that is already funded.

The three reverted commits remain in this branch's history and are the starting point for the
follow-up work.

#### The same mechanism applies to Part B

`Uno.Toolkit.WinUI.dll` references `Uno.UI.Toolkit.ElevatedView` and
`Uno.UI.Toolkit.GlobalStaticResources`. Part B ships with no type-forwarders, so it faces the
identical unresolvable-reference problem on a narrower surface — and because
`GlobalStaticResources` runs during resource initialization, the failure can present as a startup
`TypeLoadException` rather than a compile error. **Part B must be validated against a template
Debug build before it merges.** It is the cheap rehearsal for Part A's eventual rename.

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

## Part A — `src/Uno.UWP` → `src/Uno.WinRT`

### Move

| What | From | To |
|---|---|---|
| Folder | `src/Uno.UWP` | `src/Uno.WinRT` |
| Projects | `Uno.{Skia,Reference,Wasm,netcoremobile}.csproj` | `Uno.WinRT.{Skia,Reference,Wasm,netcoremobile}.csproj` |
| `AssemblyName` (×4) | `Uno` | unchanged |
| Output | `Uno.dll` in `bin/Uno.<variant>/` | `Uno.dll` in **`bin/Uno.WinRT.<variant>/`** — the folder follows the csproj name, the file name follows `AssemblyName` |
| `RootNamespace` | `Windows` | unchanged |
| NuGet package id | `Uno.WinRT` | unchanged |
| `AndroidResgenNamespace` | `Uno.UWP` | unchanged — **deliberate, not an oversight.** The generated `Uno.UWP.Resource` type is public in the mobile head and matches the 6.6 PackageDiff baseline; renaming it would register as a removed public type and need a fresh ignore entry, to buy nothing |

Nothing ships differently: the package, the assembly, and every type keep their identity, so
PackageDiff sees no delta and consumers need no action. Only the *inputs* to packing move.

### Consequences that are not the obvious find-and-replace

Only *paths* move, but they are load-bearing in places the compiler never checks. Renaming the
csprojs also moves `bin/<ProjectName>/`, so build outputs relocate even though `Uno.dll` does not —
splitting that distinction wrong is the single easiest mistake in this change:

- **26 `<file src>` paths** in `build/nuget/Uno.WinRT.nuspec`: `src\Uno.UWP\Bin\Uno.<variant>\…`
  → `src\Uno.WinRT\Bin\Uno.WinRT.<variant>\…`. **The trailing `Uno.dll` / `Uno.pdb` file names do
  not change** — they follow `AssemblyName`, not the csproj name
- `SamplesApp.Skia.Generic.csproj` — 2 hard-coded `…\Uno.UWP\bin\Uno.Skia\…\Uno.dll` item paths
- `SamplesApp/SamplesApp.csproj` — 4 `ProjectReference`s. This legacy head is easy to miss: no
  solution filter builds it, so nothing fails when it is left stale
- `UnoAssemblyHelper.cs` — takes folder name, **assembly file name** and `bin/` subfolders as three
  separate arguments; the first and third move, the second does not
- `src/Uno.UI/tsconfig.json` — `../Uno.UWP/js/*`
- **5 `_AdjustedOutputProjects` entries** in `src/Directory.Build.props` key on the *csproj file
  name*, so — unlike a pure folder move — they **do** have to be rewritten. Two of them
  (`Uno.Tests.csproj`) are stale: no such project exists. Drop them.

The **sync generator** routes generated WinRT stubs by hard-coded path
(`src/Uno.WinAppSDKSyncGenerator/`):

- `Generator.cs` — 3 × `..\..\..\Uno.UWP\Generated\3.0.0.0` output paths
- `Generator.cs:186` — `var platformProject = @"..\..\..\Uno.UWP\Uno";` (project path prefix,
  suffixed per variant) — becomes `..\..\..\Uno.WinRT\Uno.WinRT`
- `Generator.cs` — `basePath.Contains(@"\Uno.UWP\", …)` platform-vs-Skia discriminator, becoming
  `\Uno.WinRT\`. This is a concrete reason `Uno.WinRT` beats a bare `Uno` for the folder name:
  matching on `\Uno\` would make a repository cloned into a folder named `Uno` look like this
  project on every path, silently emitting native defines for Skia-only libraries.
- `Program.cs:19` — `DeleteDirectoryIfExists(@"..\..\..\Uno.UWP\Generated\")`
- `AGENTS.md` and `.claude/skills/winui-port/SKILL.md` document the source layout by path

Plus ~30 `ProjectReference` paths across the tree, 6 `src/Uno.UI.slnx` entries, and 9 `.slnf`
filters.

### Sequencing constraint

This move is a **prerequisite** for the sync-generator API-relocation work, which relocates
namespaces *into* this project — the generator addresses it by path, so the relocation would have
to be redone if the folder moved afterwards. Part A therefore lands first.

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

Part A first, because the sync-generator relocation work addresses this project by path.

The two changes are file-independent except for four shared files — `src/Directory.Build.props`,
`src/Uno.UI.slnx`, `build/PackageDiffIgnore.xml`, and the Extras csprojs' `ProjectReference` to
`..\Uno.UWP\…`. Both branches can therefore be developed in parallel; the Extras branch rebases
once Part A merges.

Commit in logical groups that each build clean, rather than one squashed rename:

1. `git mv` the folder (pure move, no content edits — keeps rename detection intact)
2. Rename csprojs + `AssemblyName` / `RootNamespace` (Part B only)
3. Fix `ProjectReference`s, `Uno.UI.slnx`, `.slnf` filters, `_AdjustedOutputProjects`
4. Assembly-identity string literals (Part B only: `InternalsVisibleTo`, linker XML, ALC helper)
5. Namespace + `xmlns` sweep (Part B only), then regenerate golden files
6. `.nuspec` paths; `PackageDiffIgnore.xml` baseline reset (Part B only)
7. Sync-generator routing (Part A only)
8. Docs + migration guide

`specs/050-breaking-changes-rollup/spec.md` gets BC53 checked off. Part A adds no rollout item —
it is not a breaking change.

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
| 6 | **Sync-generator round-trip** (Part A) | Re-run and confirm `Generated/` returns byte-identical under the new path — otherwise the hard-coded paths are unverified |
| 7 | PackageDiff | Part A: must show **no** delta. Part B: flags every type in the renamed assembly; handled as a baseline reset |
| 8 | **Template tests** (Part A) | The only check that exercises the Uno.Sdk's implicit package graph — `Uno.UI.HotDesign` and the Uno-family feature packages are pre-7.0 binaries, and this is where an identity change surfaces |

Checks 3, 4, 6 and 8 are the ones that fail *silently* — a green Skia build proves none of them.

### Why the identity split is deferred (found during Part A)

Two identities for one assembly means both can be referenced by a single compilation. Every
`Windows.*` type is then defined twice, and Roslyn's `Compilation.GetTypeByMetadataName` returns
null on ambiguity rather than erroring — which made the XAML generator *silently drop* literal and
extended properties from its output, breaking 8 source-generator tests that reference a pre-7.0
`Uno.WinUI` package on top of the local build.

The mirror-image failure hit the template tests: with no `Uno.dll` in the graph at all, the
pre-7.0 binaries the Uno.Sdk pulls implicitly had nothing to bind to, and every Debug template
build failed with `CS0012`. And the WASM browser head resolves its exports by assembly name at
runtime (`getAssemblyExports`), so the rename broke app startup with no compile-time signal.

Together these are why Part A now moves the folder only, and why the rename waits for the
first-party upgrade waves ([When the assembly gets renamed](#when-the-assembly-gets-renamed)).
The lesson generalises: an assembly
identity is referenced by *string* from source generators, trimming descriptors, ALC helpers and
JavaScript interop, and none of those are type-checked.

## Risks

| Risk | Mitigation |
|---|---|
| A missed string literal (linker XML, ALC, PRI check) compiles fine and fails at runtime or trim time | Checks 4, 6, 8 above target exactly these; enumerate the literals from this spec as a checklist |
| Stale `bin/obj` under the old folder names masks path errors | Clean the old `src/Uno.UWP` / `src/Uno.UI.Toolkit` output trees before validating |
| Conflict with in-flight sync-generator relocation work | Part A lands first, by design |
| Golden files hand-edited instead of regenerated | Regeneration is a named step; review the diff for generator-shaped output |
| PackageDiff baseline reset hides an unintended API change | Reset scoped per renamed assembly, not repo-wide |
