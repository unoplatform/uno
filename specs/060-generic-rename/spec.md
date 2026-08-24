# Freeing the name `skia` — the generic rename

**Status**: Proposal
**Audience**: Internal engineering (Uno Platform maintainers)

> Follows [spec 059](../059-runtime-identifier-cleanup/spec.md), which freed the MSBuild *property* axis
> (`UnoRuntimeFlavor` = `Generic` / `Wasm` / `Reference`). This one covers everything else still spelled
> `skia` while meaning "Uno draws it".

## 1. Why, and what changed about why

Until the drawing-backend abstraction (unoplatform/uno#24153), `skia` was merely a redundant name: Uno drew
with Skia, so "Skia" and "drawn by Uno" picked out the same thing. That PR makes them different things. Skia
becomes **one backend among several** — WebGPU, and the SkiaSharp-free managed engines — chosen at **run
time**, per host.

So there are now two distinct meanings sharing one word, and only one of them is real:

| Meaning | Examples | Verdict |
|---|---|---|
| **The actual Skia backend** | `Uno.UI.Composition.Skia`, `UnoDrawingBackendSkia`, `UNO_DRAWING_SKIA`, `SkiaSharp` usage | **Keep.** This is what Skia now means |
| **"Uno draws this" / the generic build** | `__SKIA__`, `*.skia.cs`, `uno-runtime/<tfm>/skia`, `HAS_UNO_SKIA`, the `netX.0-skia` pseudo-TFM, `Uno.UI.Runtime.Skia.*` | **Rename.** These name a backend they do not select |

A compile-time symbol *cannot* name the backend, because the backend is not known until run time. Every
occurrence in the second row is therefore not just redundant but false.

## 2. The finding that sets the sequencing

Counting hand-written `__SKIA__` files by project (excluding `Generated/`):

| Project | Files | Flavours | Is `__SKIA__` a real discriminator? |
|---|---|---|---|
| `Uno.UI.RuntimeTests` | 123 | single | **no — always true** |
| `Uno.UI` | 120 | single | **no — always true** |
| `SamplesApp` | 52 | single | **no — always true** |
| `Uno.WinRT` | 48 | 3 | yes |
| `Uno.UI.Composition` | 6 | single | **no — always true** |
| `Uno.Foundation`, `Uno.UI.Dispatching` | few | 3 / 4 | yes |

**Roughly 300 of ~354 hand-written occurrences sit in single-flavour projects, where the project sets
`UnoRuntimeFlavor=Generic` unconditionally and `__SKIA__` is therefore always defined.** Those are not
conditionals to rename — they are dead conditionals to *delete*, which is exactly what
`dev/mazi/dead-conditional-branches` (unoplatform/uno#24089) already does.

Renaming them would preserve ~300 always-true `#if`s under a new name and collide head-on with that PR.

**Therefore the rename must run last.** It should be a small mechanical sweep over what genuinely remains,
not a 60,000-occurrence diff that fights three other open PRs.

## 3. Sequencing

| | Change | State |
|---|---|---|
| 1 | #24088 — conditional compilation vocabulary | **merged** |
| 2 | spec 059 — runtime identifier cleanup (`UnoRuntimeFlavor`) | in review |
| 3 | #24089 — dead conditional branches (deletes the always-true `__SKIA__`) | open |
| 4 | #24153 — drawing backend abstraction (establishes what `Skia` legitimately means) | open |
| 5 | **this** — rename what is left | blocked on 3 and 4 |

Steps 3 and 4 are what make this a small change rather than a 60,000-occurrence one: 3 deletes most of the
occurrences outright, and 4 decides which of the rest are legitimately named after the backend.

## 4. Scope, once the queue drains

### 4.1 Mechanical, internal — no external contract

| Item | Scale | To |
|---|---|---|
| `__SKIA__` in multi-flavour projects | ~48 hand-written files | `__GENERIC__` |
| `__SKIA__` in `Generated/` stubs | 5,196 files | regenerate — change **WinAppSDKSyncGenerator**, do not hand-edit |
| `*.skia.cs` suffix | 132 files + `Uno.CrossTargetting.targets` | `*.generic.cs` |
| `HAS_UNO_SKIA` / `__UNO_SKIA__` | 106 | remove — spec 056 §4.2 already deprecates them in favour of `HAS_UNO` |

### 4.2 Shipped surface — each needs an explicit decision

| Item | Why it is not mechanical |
|---|---|
| `uno-runtime/<tfm>/skia` folder | Published package layout. `PackageDiffIgnore.xml` shows `generatepkgdiff.exe` reads assemblies under `uno-runtime/`; whether it resolves those folders **by hardcoded name** cannot be determined from this repo, and the tool ships as a binary. A CI run has to prove the API-diff gate survives |
| `netX.0-skia` hot-reload pseudo-TFM | A DevServer wire value (`ConfigureServer.RuntimeTargetFramework`) matched server-side. Client and server version independently, so it needs a transition that accepts both spellings |
| `Uno.WinUI.Runtime.Skia.*` package names | Public NuGet identities with an ecosystem of references, templates and docs. A rename is a migration, not a sweep — see §4.3 |

### 4.3 The runtime host packages — measured, not assumed

The question is whether `Uno.WinUI.Runtime.Skia.*` still names anything true once the backend is pluggable.
Measured against `feature/drawing-backend-abstraction`:

| Evidence | Result |
|---|---|
| `SkiaSharp` / `Composition.Skia` / `SkiaBackend` references in each host's `.csproj` | **0 of 8**, except `Headless` (1) |
| Host `.cs` files mentioning `SkiaSharp` / `SKSurface` / `SKCanvas` / `GRContext` | **7 of 358** |
| `MacOS`, `AppleUIKit`, `WebAssembly.Browser`, `Headless` | **zero** Skia references of any kind |
| What the 7 residual files are | all `GRContext`, all under `Rendering/` — the swapchain/graphics-context seam the drawing SPI negotiates over |

`Uno.UI.Runtime.Skia.Win32` even references `Uno.UI.Composition.WebGpu.Init` and no Skia project at all. These
are **platform hosts** — windowing, input, lifecycle, swapchain — not renderers. The name is no longer true.

**Recommendation: drop the `.Skia.` segment rather than replace it.** `Uno.WinUI.Runtime.Skia.Win32` becomes
`Uno.WinUI.Runtime.Win32`. That segment only ever disambiguated these from the *native* runtime hosts, which
7.0 removed, so there is nothing left to disambiguate against. `Generic` would be actively wrong here: these
are the most platform-specific projects in the repository. `Generic` belongs on the flavour axis (spec 059),
where the thing it names really is platform-neutral.

Two things to settle before doing it:

1. **`Uno.WinUI.Runtime.Skia.WebAssembly.Browser` → `Uno.WinUI.Runtime.WebAssembly.Browser`** sits one segment
   away from `Uno.WinUI.Runtime.WebAssembly`, the native browser host removed in 7.0 but still resolvable on
   nuget.org. Reusing that prefix for a package with opposite semantics invites the wrong restore.
2. **Scale.** 421 files reference `Uno.*.Runtime.Skia.*` (376 `.cs` — namespaces and usings — 23 `.csproj`,
   12 `.md`, 6 `.targets`, 3 `.yml`, 1 `.json`), plus directory renames, nuspecs, solution filters, the
   `Uno.Sdk` implicit package references, and the templates. This is a migration with a deprecation story,
   not a sweep.

### 4.4 Explicitly out of scope

`_LibraryUnoRuntimeIdentifier`'s `skia` default in `uno.winui.runtime-replace.targets`. That is the folder a
**third-party** cross-runtime library already packed into, on nuget.org today, under a name its author chose
through `UnoRuntimeIdentifier`. It is not ours to rename.

## 5. Why this branch is not merged on top of #24153 yet

Merging `feature/drawing-backend-abstraction` into this stack produces 23 conflicted files / 53 hunks —
concentrated in `ParsedText.cs` (13), `Visual.skia.cs` (12) and `UnicodeText.cs` (7). **Only one of those 23
files is touched by spec 059's commits**; the other 22 are collisions between #24088 and #24153, whose
resolution belongs to those two changes rather than to a rename.
