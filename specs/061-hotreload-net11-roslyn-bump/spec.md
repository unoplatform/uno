# Hot Reload: Align the net11 Dev-Server Host with the SDK 11 Compiler (Roslyn 5.12)

Status: **implemented; unit and end-to-end runtime validation done on SDK 11.0.100**. Most manual app scenarios and
the package-graph diff remain (see Test plan).
Issue: [uno#26316](https://github.com/unoplatform/uno/issues/26316).
Builds on: [spec 053](../053-hotreload-workspace-analyzer-flavor-skew/spec.md) (R2 alignment rule, R5/R6 capability
fixes), [spec 060](../060-hotreload-architecture/spec.md) (test suites).

## Overview & Objectives

The net11.0 dev-server host embeds Roslyn **5.6.0** (`src/Uno.DevServer.Roslyn.props`). It was never aligned with
SDK 11: the net11 block was created in a merge by copying the net9 line, then set to the net10 line (5.6.0) to fix
the build (`3ec0ad390a`). Roslyn 5.6 tops out at C# 14, while `net11.0` projects default to `LangVersion` 15.0. The
workspace rejects `/langversion:15.0`, silently falls back to C# 14, and any C# 15 syntax in a project blocks every
Hot Reload update to it, XAML-only edits included.

Spec 053 R2 says each host flavor embeds the Roslyn line of the SDK it serves. This spec applies that rule to net11
and carries forward every check that the 4.14 → 5.6 bump needed.

### What SDK 11.0.100 ships (verified 2026-11-11)

| Component | SDK 11.0.100 | net11 host today | Source |
|---|---|---|---|
| Roslyn | **5.12.0** (`5.12.0-1.26529.118`) | 5.6.0 | `dotnet/dotnet` tag `v11.0.100`, `src/roslyn/eng/Versions.props` |
| MSBuild | **18.12.2** | 18.7.1 | same tag, `src/msbuild/eng/Versions.props` |

- `5.12.0-1.26529.118` on the `dotnet-tools` feed is built from the exact VMR commit of the `v11.0.100` tag
  (`297acf930f`, read from the package's `.nuspec` `repository commit`). All eight `Microsoft.CodeAnalysis.*`
  packages the host pins exist at that version.
- nuget.org has no Roslyn newer than **5.9.0** (C# 14). MSBuild 18.12.2 **is** on nuget.org.

## Requirements

### R1 — Package source for Roslyn 5.12 (temporary)

Add the `dotnet-tools` feed to the net11 block of `src/Uno.DevServer.Roslyn.props` as a
`RestoreAdditionalProjectSources` entry, **not** to `NuGet.Config`:

```xml
<RestoreAdditionalProjectSources>$(RestoreAdditionalProjectSources);https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json</RestoreAdditionalProjectSources>
```

In 2024 (`52f889ab97`, removed again in `c069e88a02`) the feed went into `NuGet.Config`. That makes every restore in
the repository depend on the feed: a machine or CI mirror that cannot reach it fails to restore all of Uno for a pin
only the net11 dev-server needs. Only the restore graphs that resolve the net11 Roslyn line get the source:

- `Uno.HotReload` and `Uno.UI.RemoteControl.Server.Processors` (the host flavor);
- `Uno.HotReload.Tests` and `Uno.UI.SourceGenerators.Tests` (they reference `Uno.HotReload` and import the props).

The Host, `Uno.UI.RemoteControl` and the apps reference the Processors with `ReferenceOutputAssembly=false`, so the
Roslyn packages are not in their restore graph and they need no extra source.

**No `packageSourceMapping`**, as in 2024. Once any mapping exists, NuGet ignores every source without one, and
CI adds the `uno-dev` and `uno-features` feeds at run time with `dotnet nuget add source`
(`build/ci/templates/uno-dev-feed.yml`): a mapping here would stop those legs from finding dev packages. The
version pins are exact (Central Package Management), so the extra source cannot float a dependency.

`Microsoft.CodeAnalysis.Analyzers 5.12.0-1.26465.4`, a dependency of the 5.12 packages, exists only on the feed.

When 5.12.0 reaches nuget.org, switch `DevServerRoslynVersion` to it and remove the feed
([uno#26328](https://github.com/unoplatform/uno/issues/26328)).

### R2 — Bump the net11 matrix

`src/Uno.DevServer.Roslyn.props`, net11.0 block only:

| Property | From | To |
|---|---|---|
| `DevServerRoslynVersion` | 5.6.0 | 5.12.0-1.26529.118 |
| `DevServerMSBuildVersion` | 18.7.1 | 18.12.2 |
| `DevServerMSBuildToolsVersion` | 18.7.1 | 18.12.2 |

The net9 and net10 blocks do not change.

### R3 — Transitive dependency fallout

`Microsoft.CodeAnalysis.Workspaces.MSBuild 5.12` requires newer packages than the central pins:

| Package | Required by 5.12 | Central pin | Effect |
|---|---|---|---|
| `Microsoft.Extensions.Logging` (+ `DependencyInjection`, `Options`, `Primitives`, `Logging.Abstractions`) | ≥ 10.0.10 | 10.0.1 | NU1605 downgrade, an error under `TreatWarningsAsErrors`, in `Uno.UI.RemoteControl.Server.Processors` (references `Microsoft.Extensions.Logging` directly) |
| `System.Composition` | ≥ 10.0.10 | — | not centrally pinned; resolves transitively |
| `Microsoft.Build.Framework` | ≥ 18.7.1 | 18.12.2 after R2 | satisfied by R2 |

As implemented: `DevServerExtensionsVersion` (10.0.10, net11 block of `src/Uno.DevServer.Roslyn.props`) raises
`Microsoft.Extensions.Logging` and `Microsoft.Extensions.DependencyInjection`, the two centrally pinned ones, for
the projects importing that file only. The repository-wide pins do not move. `System.Composition` is not centrally
pinned and resolves transitively.

### R4 — The host package stays self-contained

`Uno.UI.RemoteControl.Host` copies its dependencies locally (`CopyLocalLockFileAssemblies`). Check that:

- the shipped package (`Uno.WinUI.DevServer`) carries the 5.12 assemblies in its `tools` payload and declares
  **no** NuGet dependency on a prerelease `Microsoft.CodeAnalysis.*` (NU5104), so consumers never need the
  `dotnet-tools` feed;
- `Microsoft.Build*.dll` are **not** in the host output, so MSBuild still loads from the SDK through the locator.

### R5 — Clear message when the language version is too new

Independent of the bump, and needed for the next SDK. The raw `LangVersion` never reaches the workspace: Roslyn's
`MSBuildProjectLoader.Worker` parses the command line and ignores its errors, so an unparseable `/langversion`
leaves `SpecifiedLanguageVersion` at `Default` with no diagnostic at all.

Detection therefore uses what the workspace does know:

- the SDK always passes an explicit `/langversion`: `Microsoft.CSharp.Core.targets` sets an empty `LangVersion` to
  `_MaxSupportedLangVersion`, which is `9 + (major - 5)` for a .NET 5+ target framework (capped at the SDK
  compiler's newest version);
- `EmbeddedRoslyn.MaxCSharpVersion` is the embedded compiler's newest major version.

`EmbeddedRoslyn.WarnOnUnsupportedLanguageVersions` warns per C# project whose specified version is `Default`,
`Latest`, `LatestMajor` or `Preview` and whose target framework defaults to a C# version above `MaxCSharpVersion`.
These four resolve against the compiler, so when the SDK default is out of the embedded compiler's reach they all
mean an older C# here than in the build. An explicit version the embedded compiler can parse is honored as-is and
never warns, so the `LangVersion` workaround the warning suggests silences it. It runs right after
`WarnOnAnalyzerLoadFailures`, on the same solution snapshot, so it repeats when the solution is reloaded (a file add).

Example on the 5.6 embed with a `net11.0` project:

```text
Project 'Cs15Check' (net11.0) defaults to C# 15.0, but the hot-reload compiler (Roslyn 5.6) supports up to C# 14.0:
C# 15 syntax will block hot reload with errors the build does not have, XAML-only edits included. Update Uno.Sdk,
or set <LangVersion>14.0</LangVersion> in the project.
```

The load-time warning is one line among many, and every blocked edit afterwards lists only the compile errors. So
`HotReloadManager` follows "Hot reload blocked by …" with a hint for each blocking project the same check flags
(`EmbeddedRoslyn.GetUnsupportedLanguageVersionHint`). The check is a pure function of the project, so no state is
carried into the emit path:

```text
Hot reload blocked by 3 compilation error(s) in Cs15Check (edited: MainPage.xaml, HotReloadInfo.g.cs).
'Cs15Check' defaults to C# 15.0, but the hot-reload compiler (Roslyn 5.6) supports up to C# 14.0, so errors on C# 15
syntax above are not build errors. Update Uno.Sdk, or set <LangVersion>14.0</LangVersion> in the project.
```

The XAML generator's "Hot reload blocked by … source generator error(s)" line gets the same hint, for the change-set's
projects: a XAML error such as an unresolved `x:Bind` member can come from code-behind the embedded compiler cannot
parse. Computing the hint never fails the pass: an exception is logged at Verbose and the pass stays blocked.

The hint is also added to the operation's diagnostics as the Warning `UNOHR0002`, so the IDE, the in-app indicator
and `uno_app_hot_reload_status` show the reason, not only `Failed`. The compile errors themselves still reach the
dev-server console only (the FIXME in `HotReloadManager`). `UNOHR0002` is documented next to `UNOHR0001` in
`doc/articles/uno-build-error-codes.md`.

Not detected: an explicit numbered `LangVersion` newer than the embedded compiler on a target framework whose
default it supports (`<LangVersion>15.0</LangVersion>` on `net10.0` with a 5.6 host). The dropped value surfaces as
`Default`, which an explicit `<LangVersion>default</LangVersion>` also produces, so it cannot be told apart without
a false warning. Also not detected: `latest`, `latestMajor`, `preview` or `default` on a target framework whose
default the embedded compiler supports, built by a newer SDK (`net11.0` with `latest` on SDK 12). These resolve
against the build's compiler, which the workspace does not expose.

## Checks carried over from the 5.6 bump

Each of these broke, or was found broken, during spec 053 or right after it, and the build did not catch any of
them.

| # | What broke last time | Where | Check for 5.12 |
|---|---|---|---|
| C1 | Roslyn removed `WatchHotReloadService` between 5.0 and 5.3; the shim moved to `UnitTestingHotReloadService` via reflection | 053 R2 | The reflection lookups in `Uno.HotReload/Microsoft/WatchHotReloadService.cs` bind on 5.12. They fail fast at session creation (`454c69d5ab`), so one real session proves it. Diff the type's public surface between 5.6 and 5.12 |
| C2 | The move dropped Roslyn's implicit `AddExplicitInterfaceImplementation` grant; every ResourceDictionary/DataTemplate edit hit ENC0106/CS9346 | 053 R5 | `Given_WatchHotReloadService` green; check whether 5.12 grants it itself (then the shim appends it twice, which must stay harmless) |
| C3 | CoreCLR EnC defect: the 4th update of a session failed while `AddFieldRva` was granted | 053 R6 | Withdrawal test green; runtime run of **more than 8 generations** in one session. Do not drop R6 on the assumption that .NET 11 fixed the runtime bug: prove it separately |
| C4 | Analyzer flavor skew: 0 generators, hundreds of phantom errors | 053 R1/R3 | `CompilerApiVersion` now resolves to `roslyn5.12`; a library using the MVVM Toolkit compiles clean in the workspace; no analyzer load-failure warnings |
| C5 | Analyzers failed to load in collectible contexts (CI hang) | 053 R4 | `Given_CollectibleAnalyzerAssemblyLoader` green; `Given_HotReloadWorkspace` does not hang |
| C6 | A rude edit on 5.x was committed as the next baseline; later edits were silently lost | 060, #24085 | Rude-edit recovery tests in `Uno.HotReload.Tests` green |
| C7 | Package add during Hot Reload: ENC1099 on 5.6 | 056 | Reference-pinning tests green; repeat the manual package-add scenario |
| C8 | Empty update arms roslyn#79898 on the next edit | 055 | No-op dedup tests green; check whether 5.12 fixes the Roslyn bug (dedup stays either way) |
| C9 | `WorkspaceFailed` obsoleted in 5.x; new obsoletions fail the build under `TreatWarningsAsErrors` | `CompilationWorkspaceProvider` | Release build of Host and Processors on **both** TFMs; no new `NoWarn` |
| C10 | Test project could not restore against the new line (NU1107) | 053 test plan | `Uno.UI.SourceGenerators.Tests` now inherits the line from `Uno.HotReload`; restore it and run `MetadataUpdateTests` (snapshot baselines changed last time) |
| C11 | Copy-paste in the version matrix shipped the wrong line to net11 | `3ec0ad390a` | Covered by the guard test G1 below |

## New guardrails

- **G1 — language-version guard test.** In `Uno.HotReload.Tests`, assert that the embedded Roslyn can parse the
  `MaxSupportedLangVersion` of the SDK building the test project (C# 15 for net11). The test project targets
  `$(NetUnitTests)` only, so it guards that one host flavor; it fails the day that flavor sits on an old Roslyn
  line, which no check caught for net11.
- **G2 — C# 15 in the end-to-end app.** HRApp inherits `LangVersion` 13.0 from `src/Directory.Build.props`, so it
  cannot see this bug. Give HRApp's net11 build the SDK default `LangVersion` plus one file with C# 15 syntax. No
  new test is needed: every existing HRApp test recompiles that file, so the XAML-only edit tests (`Given_TextBlock`,
  `Given_Frame`, …) are the XAML-only edit test. They fail on a 5.6 host and pass on 5.12 (see Status).
  `Given_CSharpLatestSyntax` also edits a method body inside the C# 15 file and checks the new body runs. No CI job
  runs HRApp yet: G2 is a local check until one does, and G1 is the only guard CI enforces.
- **G3 — R5 warning tests.** A project whose target framework defaults to a C# version above the embedded maximum
  produces the warning once for `Default`, `Latest`, `LatestMajor` and `Preview`, a platform target framework
  included; a supported default or an explicit supported version does not. A blocked reload of such a project is
  followed by the hint, whether compile errors or a XAML generator error block it, and its diagnostics carry it as
  `UNOHR0002`.

## Non-goals

- Loading the SDK's own Roslyn at runtime instead of embedding one. It removes the skew for good but depends on
  the SDK's internal layout; it needs its own spec.
- Changing the net9 or net10 flavors.
- Revisiting R6 (`AddFieldRva`) without separate proof that the runtime defect is fixed.

## Test plan

Label evidence as **Compile**, **Unit** or **Runtime**. Compile-only is not runtime validation.

1. **Compile**: Release build of `Uno.UI.RemoteControl.Host`, `Server.Processors` and `Uno.HotReload` for
   net10.0 and net11.0, zero warnings (C9).
2. **Compile**: net9/net10 resolved package graphs are unchanged — diff `project.assets.json` before and after.
3. **Unit**: `Uno.HotReload.Tests` full suite, including G1 and G3 (C1, C2, C3, C5–C8).
4. **Unit**: `Uno.UI.SourceGenerators.Tests` `MetadataUpdateTests` (C10).
5. **Compile**: pack `Uno.WinUI.DevServer`; inspect the nuspec and `tools` payload (R4).
6. **Runtime**: `Given_HotReloadWorkspace` (Skia desktop) with G2 (C3–C5, issue repro).
7. **Runtime, manual**, on SDK 11.0.100 with a fresh `dotnet new unoapp -tfm net11.0`:
   - the issue's repro (labeled `continue outer;`) → XAML edit gives `Found 1 metadata updates`;
   - a C# edit inside the C# 15 file applies;
   - more than 8 edits in one session (C3);
   - a library using the MVVM Toolkit, edited during the session (C4);
   - add a package reference during the session (C7);
   - a clean machine without the `dotnet-tools` feed restores the app (R4).
8. **Runtime, regression**: the same app on `net10.0` / SDK 10 still hot reloads (net10 flavor untouched).

### Status as implemented

| Step | Status | Evidence |
|---|---|---|
| 1 | **Compile, done** | `Uno.UI.RemoteControl.Server.Processors` (with `Uno.HotReload`) Release, analyzers on, every target framework, on 5.12: 0 warnings, 0 errors (C9) |
| R1 | **Compile, done** | With no `dotnet-tools` entry in `NuGet.Config` and an empty package folder (`NUGET_PACKAGES`), `Uno.HotReload`, the Processors, `Uno.HotReload.Tests` and `Uno.UI.SourceGenerators.Tests` restore: 5.12 from the feed, 5.6 from nuget.org |
| 3 | **Unit, done** | `Uno.HotReload.Tests` on net11.0 against 5.12: 176 passed, 0 failed, 1 skipped (Unix-only), G1 and G3 included. The test assemblies load `5.12.0-1.26529.118`, the same build (commit `297acf930f`) as the SDK's own compiler |
| R4 (part) | **Compile, done** | The net11 Processors output has no `Microsoft.Build*.dll` (MSBuild `18.12.2` loads from the SDK), Microsoft.Extensions `10.0.10`, and 147 Roslyn binaries with valid .NET/Microsoft Authenticode signatures |
| 6 | **Runtime, done** | HRApp suite against a source-built net11 DevServer on SDK 11.0.100 (the `Given_HotReloadWorkspace` setup, driven directly): **5.12** — 36 passed, 0 failed, 11 skipped (existing `[Ignore]`), 97 metadata updates in one session (C1, C3), no compile errors, no R5 warning. **5.6** (same code, net11 line overridden) — `Given_TextBlock` 4/4 fail with the issue's symptom: XAML-only edits `Emit status: Blocked`, CS1002/CS0103/CS0201 on `continue outer;`; the R5 warning is logged once and the hint follows every blocked line (G2, R5). `Given_CSharpLatestSyntax` (an edit inside the C# 15 method): passes on 5.12 with 1 metadata update for the edit and 1 for the undo; fails on 5.6 with the same three errors on the edited file |
| 4 | **Unit, done** | `Uno.UI.SourceGenerators.Tests` `MetadataUpdateTests` on 5.12: 244 passed, 0 failed, 7 skipped (C10; needs a Debug `Uno.UI` net11.0 build first) |
| 5 | **Compile, done** | CI build 245527 package: `processors/net11.0` carries 5.12 and `processors/net10.0` 5.6.0; the nuspec declares no Roslyn or MSBuild dependency |
| 7 (part) | **Runtime, done** | The issue's repro on SDK 11.0.100: `dotnet new unoapp -tfm net11.0` (Uno.Sdk 7.0.5) plus `continue outer;`, same XAML-only edit twice. The stock 7.0.32 dev-server blocks it (CS1002); the build 245527 dev-server applies it ("Found 1 metadata updates", new text in the live visual tree). Details in [the PR comment](https://github.com/unoplatform/uno/pull/26318#issuecomment-6808591525) |
| 2, 7 (rest), 8 | Not run | Package-graph diff, the other manual app scenarios and the net10 regression |

## Rollout

One PR: R1–R5 and G1–G3, closes #26316. G1 is red until the bump lands, so the two cannot be split.

Follow-up: [uno#26328](https://github.com/unoplatform/uno/issues/26328) replaces the feed build with the nuget.org
5.12 release once published, and removes the `RestoreAdditionalProjectSources` entry.

Downstream alignment: `Uno.HotReload.dll` is also consumed standalone through raw `HintPath` references. Its net11
flavor now binds to the 5.12 assemblies, so such a consumer must ship the same Roslyn line (from the same feed until
nuget.org has it) before it takes this build.

## Decisions

- A prerelease-versioned Roslyn ships in 7.0 SR1 for the net11 host only; the net9 and net10 hosts stay on their
  nuget.org lines. Precedent: Uno 4.7 shipped the net7 host on `4.4.0-4.final`
  ([uno#10480](https://github.com/unoplatform/uno/pull/10480)), and Uno 5.0 shipped `4.8.0-2.final`
  ([uno#14000](https://github.com/unoplatform/uno/pull/14000)). Switching to the nuget.org 5.12.0 release is tracked
  by [uno#26328](https://github.com/unoplatform/uno/issues/26328).
- MSBuild is aligned at 18.12.2, the version SDK 11.0.100 ships (spec 053 rule).
- CI restores through the scoped feed: build 245527 is green.
