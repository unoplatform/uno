# AGENTS.md

This file provides guidance to AI Agents when working with code in this repository.

## Project Overview

Uno Platform is an open-source .NET UI cross-platform framework for building .NET applications from a single codebase using the WinUI 3 API. It targets Web (WebAssembly), Desktop (Windows, macOS, Linux via Skia), and Mobile (iOS, tvOS, Android).

**Reference these instructions first**, then lean on skills (workflows) and the path-scoped rules below.

#### Claude Code Skills (invoke via `/skill-name`)

| Skill | Command | Use For |
|-------|---------|---------|
| Add Sample | `/add-sample` | Creating SamplesApp sample pages with correct registration |
| Runtime Tests | `/runtime-tests` | Building and running Uno runtime tests (Skia Desktop/WASM) |
| WinUI Runtime Tests | `/winui-runtime-tests` | Running runtime tests against native WinUI on Windows |
| WinUI Porting | `/winui-port` | Porting WinUI C++ code to Uno Platform C# (full deep reference) |
| DevServer | `/devserver` | DevServer CLI/Host build, test, MCP proxy, add-in discovery |

#### Path-scoped rules (`.claude/rules/`)

These load **automatically** when you touch matching files — you don't invoke them. They hold the non-obvious, subsystem-specific conventions so this always-loaded file stays lean:

| Rule | Applies to | Covers |
|------|-----------|--------|
| `code-style.md` | `src/**/*.cs` | nullable, file headers (MUX/MIT), logging, `[Uno.NotImplemented]` |
| `platform-targeting.md` | `src/**/*.cs` | file-suffix vs `#if` vs `OperatingSystem.IsX()` vs `ApiExtensibility` |
| `debugging-discipline.md` | `src/**/*.cs` | full root-cause/validation/diagnosis-bias protocols |
| `dependency-properties.md` | `src/Uno.UI/**` | `[GeneratedDependencyProperty]`, metadata, callbacks |
| `runtime-tests.md` | `src/Uno.UI.RuntimeTests/**` | `[RunsOnUIThread]`, `[PlatformCondition]`, `UITestHelper` |
| `unit-tests.md` | `src/Uno.UI.Tests/**` | MSTest, no-visual-tree logic tests |
| `source-generators.md` | `src/SourceGenerators/**` | incremental gens, LOH/perf, cancellation |
| `samples.md` | `src/SamplesApp/**` | `[Sample]`, theming, XamlStyler |
| `build-system.md` | `src/**/*.{csproj,props,targets}` | TFMs, output paths, package versions |

**Which to reach for:** the relevant `.claude/rules/*.md` is already in context (path-scoped) — use it as the checklist. Use a `/skill` for the actual build/run/scaffold/port workflow (and its deep reference).

---

## Quick Reference

### Technology Stack

| Technology | Purpose |
|------------|---------|
| .NET 10.0/9.0 | Multi-target framework |
| C# & XAML | Primary languages |
| TypeScript | WebAssembly/Web APIs only |
| Skia | Cross-platform rendering |
| MSBuild | Build orchestration |
| Roslyn | Source generators |

### Platform File Suffixes

| Suffix | Platform |
|--------|----------|
| `.Android.cs` | Android |
| `.iOS.cs` | iOS |
| `.UIKit.cs` | iOS & tvOS |
| `.wasm.cs` | WebAssembly |
| `.skia.cs` | Skia |
| `.reference.cs` | Reference implementation |
| `.crossruntime.cs` | Skia + WebAssembly + Reference (shared) |

### Key Source Directories

- `src/Uno.UI/` - Core UI framework (WinUI controls, layout, XAML runtime)
- `src/Uno.UWP/` - Non-UI WinRT APIs (platform-specific assemblies)
- `src/Uno.Foundation/` - Foundation APIs (platform-specific assemblies)
- `src/Uno.UI.Runtime.Skia.*/` - Skia platform runtimes
- `src/SourceGenerators/` - XAML parser, DependencyProperty generator
- `src/SamplesApp/` - Sample app for validation and tests
- `src/Uno.UI.RuntimeTests/` - Platform runtime tests
- `src/Uno.UI.DevServer.Cli/` - DevServer CLI tool
- `src/Uno.UI.RemoteControl.Host/` - DevServer Host process

### Build Setup (Required)

**1. Setup cross-targeting override:**
```bash
cd src
cp crosstargeting_override.props.sample crosstargeting_override.props
```

**2. Edit `crosstargeting_override.props`** (recommended fast-iteration config):
```xml
<Project>
  <PropertyGroup>
    <!-- Choose ONE target: -->
    <UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride>              <!-- WebAssembly/Skia -->
    <!-- <UnoTargetFrameworkOverride>net10.0-android</UnoTargetFrameworkOverride>  Android -->
    <!-- <UnoTargetFrameworkOverride>net10.0-ios</UnoTargetFrameworkOverride>      iOS -->
    <!-- <UnoTargetFrameworkOverride>net10.0-windows10.0.19041.0</UnoTargetFrameworkOverride> Windows -->

    <!-- Disables analyzers + code-style enforcement for local builds. No effect on CI. -->
    <UnoFastDevBuild>true</UnoFastDevBuild>
  </PropertyGroup>
</Project>
```

Or pass the same flags per-build instead of committing them:

```bash
dotnet build … -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true
```

**Why these flags:**
- `UnoTargetFrameworkOverride` — restricts cross-targeted projects to a single TFM, skipping the redundant net9.0 outputs while you iterate on net10.0 (or vice versa).
- `UnoFastDevBuild` — disables `RunAnalyzersDuringBuild`, `EnforceCodeStyleInBuild`, and the `Microsoft.CodeAnalysis.NetAnalyzers` package for local builds. **Guarded by `ContinuousIntegrationBuild`, so CI is never affected** — analyzer-strict checks still run on every PR. Set persistently via the `UNO_FAST_DEV_BUILD=true` environment variable if you'd rather not edit the file.

Combined impact on `SamplesApp.Skia.Generic` (Windows, 32-core, warm NuGet cache): clean build ~3:23 → ~1:59, incremental rebuild after a Uno.UI edit ~2:23 → ~0:58. The `/runtime-tests` skill passes both flags by default (use `strict` to opt out for CI-equivalent coverage).

**Do not commit `crosstargeting_override.props`** — it is per-developer config and is intentionally `.gitignore`d.

**3. Use matching solution filter:**

| Platform | Solution Filter |
|----------|-----------------|
| WebAssembly | `Uno.UI-Wasm-only.slnf` |
| Skia (Desktop) | `Uno.UI-Skia-only.slnf` |
| Mobile (Android/iOS) | `Uno.UI-netcore-mobile-only.slnf` |
| Windows | `Uno.UI-Windows-only.slnf` |
| Unit Tests | `Uno.UI-UnitTests-only.slnf` |

**4. Build commands:**
```bash
cd src
dotnet restore Uno.UI-Skia-only.slnf                    # Restore (50-60s)
dotnet build Uno.UI-Skia-only.slnf --no-restore         # Build (3-5min)
dotnet test Uno.UI/Uno.UI.Tests.csproj                  # Unit tests (40-60s)
```

**CRITICAL**: **NEVER CANCEL** builds. Set timeouts to 15+ minutes. Favor Skia desktop for faster builds.

---

## Architecture Overview

### Platform Abstraction

Single C#/XAML codebase → WinUI 3 API → Platform-specific runtimes (Skia, WebAssembly, Native)

### Rendering Engines

- **Skia**: Cross-platform (Desktop Win32, macOS, Linux, Skia Android/iOS)
- **Native**: Platform controls (UIKit, Android Views, DOM elements)

### Platform Base Classes

| Platform | Inheritance |
|----------|-------------|
| Android native | `ViewGroup` → `UnoViewGroup` (Java) → `BindableView` → `UIElement` |
| iOS native | `UIView` → `BindableUIView` → `UIElement` |
| WebAssembly native | UIElements map to DOM elements (default: "div") |
| Skia | `IRenderer` interface for rendering pipeline |

### XAML Compilation

XAML files are parsed to C# via source generators (`XamlFileGenerator` in `Uno.UI.SourceGenerators`), not .xbf like WinUI. Generates `InitializeComponent()`, named fields, and x:Bind expressions.

### DependencyObject on Mobile

On Android/iOS, `DependencyObject` is an **interface** (not base class) since `UIElement` must inherit from native view classes. Source generators provide the implementation via `DependencyObjectGenerator`.

### Project Organization

Most libraries have 5 variants: Reference, Skia, WebAssembly, NetCoreMobile, Tests.

### Runtime Target Selection

For Skia, `RuntimeAssetsSelectorTask` ensures `Uno.UI` uses `netX` (generic) target for all Skia platforms. `Uno.UWP` and `Uno.Foundation` use platform-specific assemblies. Use runtime checks like `OperatingSystem.IsAndroid()` for platform-specific behavior on Skia for libraries above and including `Uno.UI`, or use `ApiExtensibility` with platform-specific implementations in `Runtime.Skia` projects.

### NotImplemented Stubs

Auto-generated stubs marked with `[Uno.NotImplemented]` allow compilation but warn if used. Located in `Generated` folders - never edit these files.

---

## Development Workflow

### Public Documentation and Spec References (MANDATORY)

When editing specifications, documentation, or other repo-tracked design artifacts intended to be shareable:

1. **Do not reference private artifacts** from the document.
   - Do not link to private issues, private pull requests, private boards, private docs, or private repositories.
   - If related work is tracked privately, mention it only in generic terms.

2. **Public specs are source-of-truth documents**.
   - Public or repo-local specs may be referenced by private trackers.
   - Private trackers must not be required to understand the public spec.

3. **Keep the dependency direction one-way**.
   - Allowed: private issues/PRs referencing a public spec in this repo.
   - Not allowed: a public spec in this repo referencing a private issue/PR/doc as normative context.

4. **If implementation follow-up exists in private repos**, describe it as alignment or downstream tracking work without identifiers or URLs.

### Debugging & Validation (MANDATORY — summary)

When fixing crashes, rendering, or selection/indexing bugs: **reproduce first → name the broken invariant → fix the root cause (and the mutation point) before adding guards → prove it with a test that fails-before/passes-after → validate at runtime, not compile-only.** Label every proposed change `root-cause fix` or `defensive hardening`; a guard-only change is never a complete resolution. Report validation evidence with explicit labels — **Code review** (by inspection) vs **Compile** (which project built) vs **Runtime** (which test/app ran) — and never present compile-only as runtime validation.

The full protocol (root-cause steps, diagnosis-bias checks, evidence rules) auto-loads from `.claude/rules/debugging-discipline.md` when editing `src/**/*.cs`.

### Validation Checklist

Run these after making changes:

1. **Build**: `dotnet build Uno.UI-UnitTests-only.slnf --no-restore`
2. **Unit tests**: `dotnet test Uno.UI/Uno.UI.Tests.csproj --no-build`
3. **Runtime tests** (UI changes): Use `/runtime-tests` skill (Skia Desktop default, pass test class/method name as argument)
4. **WinUI parity** (validate against native WinUI): Use `/winui-runtime-tests` skill
5. **Sample app** (visual changes): `cd src/SamplesApp/SamplesApp.Wasm && dotnet run`
6. **XAML formatting** (SamplesApp changes): `dotnet xstyler -d src/SamplesApp -r`

### SamplesApp: Add XAML files

When adding XAML samples to the `SamplesApp`, drop the files anywhere under `src/SamplesApp/SamplesApp.Samples/` — they are auto-discovered by glob (no manual registration required).

Sample creation checklist:
1. Create your sample XAML and code-behind under an appropriate folder in `src/SamplesApp/SamplesApp.Samples/`.
2. Add the `[Uno.UI.Samples.Controls.Sample]` attribute to the code-behind class.
3. Format XAML: `dotnet xstyler -f src/SamplesApp/SamplesApp.Samples/YourFolder/YourSample.xaml`
4. Build and run `SamplesApp` to verify the sample appears.

Theming guideline (brief): prefer `{ThemeResource}` for backgrounds/foregrounds so samples work in light and dark themes.

### Runtime Tests (Preferred for UI)

Add tests to `Uno.UI.RuntimeTests`. Key helpers:
- `WindowHelper.WindowContent` - Add elements to visual tree
- `await WindowHelper.WaitForLoaded(element)` - Wait for load
- `await WindowHelper.WaitForIdle()` - Wait for UI to settle

**To build and run tests, use the `/runtime-tests` skill.** It handles build, filter encoding, execution, and result parsing for both Skia Desktop and WASM. Test-authoring conventions auto-load from `.claude/rules/runtime-tests.md`.

### Common Build Issues

| Issue | Solution |
|-------|----------|
| "Assets file doesn't have a target" | Delete `obj/`, `bin/`, restore |
| "Windows XAML targets not found" | Use Skia/Wasm on Linux/macOS |
| Solution filter fails | Ensure `crosstargeting_override.props` matches filter |
| Persistent issues | Close VS, delete `src/.vs`, rebuild |
| Last resort | `git clean -fdx` (close VS first) |

### Key Build Properties

| Property | Purpose |
|----------|---------|
| `UnoTargetFrameworkOverride` | Single platform target |
| `UnoNugetOverrideVersion` | Debug with NuGet override |
| `UnoUISourceGeneratorDebuggerBreak` | Attach debugger to generators |
| `XamlSourceGeneratorTracingFolder` | Dump generator diagnostics |

---

## Code Conventions

### Partial Classes

Extensive use for:
- Platform-specific code: `MyControl.Android.cs`, `MyControl.iOS.cs`
- Generated code: `MyPage.xaml.g.cs`
- Logical separation: `MyControl.Properties.cs` for DependencyProperties

### DependencyProperty Pattern

Prefer `[GeneratedDependencyProperty]` for new properties. Conventions auto-load from `.claude/rules/dependency-properties.md`; for full templates copy from existing controls (`Canvas`, `RangeBase`, `Button`).

### Code Style

Tabs, Allman braces (always), `internal` extension methods in `[Type]Extensions.cs`, `#nullable enable` per-file, MUX/MIT headers on ported code. Details auto-load from `.claude/rules/code-style.md`. Style is analyzer-enforced on CI even when `UnoFastDevBuild=true` skips it locally.

### XAML Formatting (SamplesApp)

XAML files under `src/SamplesApp/` are formatted using [XamlStyler](https://github.com/Xavalon/XamlStyler).
Configuration is in `src/SamplesApp/Settings.XamlStyler`.

```bash
# One-time setup (restore tools after cloning)
dotnet tool restore

# Format all SamplesApp XAML files
dotnet xstyler -d src/SamplesApp -r

# Format a single file
dotnet xstyler -f src/SamplesApp/SamplesApp.Samples/MyFile.xaml

# Check without modifying (CI mode)
dotnet xstyler -d src/SamplesApp -r -p
```

A GitHub Actions workflow enforces formatting on PRs that touch SamplesApp XAML files.

### Implementing New WinUI Features

1. Find generated stub: `src/Uno.UWP/Generated/3.0.0.0/Windows.*/ClassName.cs`
2. Copy to non-generated location
3. Remove implemented platforms from `[NotImplemented]` attribute
4. Use platform suffix for platform-specific files

---

## Common Pitfalls

1. **DependencyObject is an interface** on Android/iOS - don't inherit, implement
2. **Generated files are regenerated** - never edit `Generated/` folders
3. **Visual tree differs by platform** - Android/iOS use native hierarchy; WebAssembly uses DOM; Skia uses rendering tree
4. **Partial methods** used for extensibility: `OnLoaded()`, `OnUnloaded()`
5. **NuGet cache corruption** - delete `%USERPROFILE%\.nuget\packages\uno.ui` if debugging fails
6. **Long paths on Windows** - enable via registry if needed

---

## Commit Guidelines

**MANDATORY**: All commits MUST follow [Conventional Commits](https://www.conventionalcommits.org/).

### Format
```
<type>[optional scope]: <description>
```

### Common Types

| Type | Purpose | Version Impact |
|------|---------|----------------|
| `fix` | Bug fixes | PATCH |
| `feat` | New features | MINOR |
| `docs` | Documentation | - |
| `test` | Tests | - |
| `chore` | Maintenance | - |
| `feat!` | Breaking change | MAJOR |

### Examples
```bash
git commit -m "chore: Initial work"
git commit -m "fix: Resolve null reference in TextBox"
git commit -m "feat(ios): Implement native picker control"
git commit -m "feat!: Remove deprecated API methods"
```

Guidelines:
- Keep description under 50 characters
- Use imperative mood ("Add" not "Added")
- Reference issues: `fix: Resolve layout issue (fixes #12345)`

---

## References

### Documentation
- [Building Uno](https://platform.uno/docs/articles/uno-development/building-uno-ui.html)
- [Contributing Guide](https://platform.uno/docs/articles/uno-development/contributing-intro.html)
- [Creating Tests](https://platform.uno/docs/articles/contributing/guidelines/creating-tests.html)

### In-Repo Docs
- Build guide: `doc/articles/uno-development/building-uno-ui.md`
- Samples guide: `doc/articles/uno-development/working-with-the-samples-apps.md`

### Subsystem deep dives
- `/winui-port` skill - WinUI C++ → C# porting
- `/devserver` skill - DevServer CLI/Host maintenance
- `/runtime-tests` skill + `.claude/rules/runtime-tests.md` - runtime test execution & authoring
- `.claude/rules/dependency-properties.md` - DependencyProperty patterns
- `.claude/rules/source-generators.md` - XAML/DependencyObject generators

### Community
- [Discord](https://platform.uno/discord)
- [Samples App](https://aka.platform.uno/wasm-samples-app)
