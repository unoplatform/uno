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
| Docs Build | `/docs-build` | Building, previewing & validating the docs website (DocFX), incl. external-doc commit bumps in `import_external_docs.ps1` |

#### Pre-commit review (invoke via `/review-panel`)

`/review-panel [scope]` runs an eight-lens reviewer panel (architect, contract, skeptic, performance, operability, quality, security, jerome) in parallel and synthesizes one report with a `ship` / `fix-first` / `block-merge` verdict. Run it before you commit or open a PR — pass a scope (`master..HEAD`, a `#PR`, `HEAD~1`) or omit it to auto-detect uncommitted changes / branch-vs-`master`. The panel learns from corrections recorded in `specs/lessons.md`. Lenses, scopes, and loop recipes: `.claude/review-panel-cheatsheet.md`.

#### Path-scoped rules (`.claude/rules/`)

These load **automatically** when you touch matching files - you never invoke them. They hold the
subsystem-specific conventions so this always-loaded file stays lean: `code-style`, `platform-targeting`
and `debugging-discipline` (`src/**/*.cs`); `dependency-properties` (`src/Uno.UI/**`); `runtime-tests`,
`unit-tests`, `source-generators`, `samples`; and `build-system` (`*.csproj|props|targets`).

Treat the loaded rule as your checklist, and use a `/skill` for the actual build/run/scaffold/port workflow.

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

These now live almost entirely in `Uno.WinRT`, `Uno.Foundation` and the `Uno.UI.Runtime.Skia.*` heads.
`Uno.UI` is Skia-only, so don't create a `MyControl.Android.cs` there.

### Key Source Directories

- `src/Uno.UI/` - Core UI framework (WinUI controls, layout, XAML runtime)
- `src/Uno.WinRT/` - Non-UI WinRT APIs (platform-specific assemblies)
- `src/Uno.Foundation/` - Foundation APIs (platform-specific assemblies)
- `src/Uno.UI.Runtime.Skia.*/` - Skia platform runtimes
- `src/SourceGenerators/` - XAML parser, DependencyProperty generator
- `src/SamplesApp/` - Sample app for validation and tests
- `src/Uno.UI.RuntimeTests/` - Platform runtime tests
- `src/Uno.UI.DevServer.Cli/` - DevServer CLI tool
- `src/Uno.UI.RemoteControl.Host/` - DevServer Host process

### Build Setup (Required)

**1.** `cd src && cp crosstargeting_override.props.sample crosstargeting_override.props` - per-developer,
gitignored, **never commit it**.

**2.** Set these in it (or pass them as `-p:` flags per build):

```xml
<UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride>
<UnoFastDevBuild>true</UnoFastDevBuild>
```

The first builds one TFM instead of every cross-target; the second skips analyzers and code-style
enforcement locally (CI is unaffected). Together they roughly halve build times. Full rationale, the
other TFM values and the VS caveat: `.claude/rules/build-system.md`.

**3. Matching solution filter:**

| Platform | Filter |
|----------|--------|
| Skia (Desktop, WebAssembly, Android, iOS) | `Uno.UI-Skia-only.slnf` |
| Windows | `Uno.UI-Windows-only.slnf` |
| Reference API | `Uno.UI-Reference-Only.slnf` |
| Unit Tests | `Uno.UI-UnitTests-only.slnf` |

**4. Build:**

```bash
cd src
dotnet restore Uno.UI-Skia-only.slnf                 # 50-60s
dotnet build Uno.UI-Skia-only.slnf --no-restore      # 3-5min
```

**CRITICAL: NEVER CANCEL a build.** Set timeouts to 15+ minutes. Favor Skia desktop for speed.

---

## Architecture Overview

### Platform Abstraction

Single C#/XAML codebase → WinUI 3 API → Platform-specific runtimes (Skia, WebAssembly, Native)

### Rendering Engines

- **Skia** renders every target: Desktop (Win32, macOS, Linux), Android, iOS/tvOS, WebAssembly.
- The UI layer is Skia-only - `Uno.UI` targets `$(NetSkiaPreviousAndCurrent)` and has no native-view TFMs.

### Development scope

`Uno.UI` and everything above it is **Skia-only** - one build serving every target. The native-view
UI layer (Android Views, UIKit, WASM DOM) has been removed; don't add or restore code for it.

**Platform-specific non-UI WinRT APIs are the exception** - `Uno.WinRT` and `Uno.Foundation` still ship
per-platform implementations and are actively developed, because Skia-on-Android consumes the Android
file picker, sensors, contacts and so on.

### XAML Compilation

XAML files are parsed to C# via source generators (`XamlFileGenerator` in `Uno.UI.SourceGenerators`), not .xbf like WinUI. Generates `InitializeComponent()`, named fields, and x:Bind expressions.

### Project Organization

The WinRT layer (`Uno.WinRT`, `Uno.Foundation`, `Uno.UI.Dispatching`) keeps per-platform variants: Reference, Skia, WebAssembly, NetCoreMobile. From `Uno.UI` upwards the UI layer ships a single Skia build, which also serves as the compile reference — there is no `.Reference` variant of those projects.

### Runtime Target Selection

For Skia, `RuntimeAssetsSelectorTask` ensures `Uno.UI` uses `netX` (generic) target for all Skia platforms. `Uno.WinRT` and `Uno.Foundation` use platform-specific assemblies. Use runtime checks like `OperatingSystem.IsAndroid()` for platform-specific behavior on Skia for libraries above and including `Uno.UI`, or use `ApiExtensibility` with platform-specific implementations in `Runtime.Skia` projects.

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
2. **Unit tests**: `dotnet test Uno.UI.UnitTests/Uno.UI.UnitTests.csproj --no-build` for a quick
   loop. Before pushing, run the full `dotnet test Uno.UI-UnitTests-only.slnf --no-build` - that
   is CI's actual gate, and it also covers the analyzer, source-generator, Hot Reload and
   DevServer test projects that the narrower command silently skips.
3. **Runtime tests** (UI changes): Use `/runtime-tests` skill (Skia Desktop default, pass test class/method name as argument)
4. **WinUI parity** (validate against native WinUI): Use `/winui-runtime-tests` skill
5. **Sample app** (visual changes): `dotnet run --project src/SamplesApp/SamplesApp -f net11.0-desktop`
6. **XAML formatting** (SamplesApp changes): `dotnet xstyler -d src/SamplesApp -r`

### Common Build Issues

| Issue | Solution |
|-------|----------|
| "Assets file doesn't have a target" | Delete `obj/`, `bin/`, restore |
| "Windows XAML targets not found" | Use Skia/Wasm on Linux/macOS |
| Solution filter fails | Ensure `crosstargeting_override.props` matches filter |
| Persistent issues | Close VS, delete `src/.vs`, rebuild |
| Last resort | `git clean -fdx` (close VS first) |

### CI Investigation (Azure DevOps)

Prefer the `mcp__azure-devops-uno__*` tools (`pipelines_build`, `pipelines_build_log`,
`testplan_show_test_results_from_build_id`) over hand-rolled `curl` against `dev.azure.com`.
Anonymous REST needs the project **GUID**, not the name `uno`.

If `curl` is unavoidable, always pipe through `jq` to extract only what you need
(`jq '.records[] | select(.result=="failed")'`) - never print a full timeline or log JSON into
the transcript; these responses routinely exceed 500 KB. Note that post-merge builds don't run
the WASM test stage.

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

Tabs, Allman braces (always), expression-bodied members for one-liners, `internal` extension methods in `[Type]Extensions.cs`, `#nullable enable` per-file, MUX/MIT headers on ported code. Comments only when they add value — short (a line or two, never a wall of text), explaining the non-obvious *why*, never narrating code removal/history; longer is OK only when explicitly requested, actually needed for code understanding, or carried verbatim from a WinUI port. Details auto-load from `.claude/rules/code-style.md`. Style is analyzer-enforced on CI even when `UnoFastDevBuild=true` skips it locally.

### Implementing New WinUI Features

1. Find generated stub: `src/Uno.WinRT/Generated/3.0.0.0/Windows.*/ClassName.cs`
2. Copy to non-generated location
3. Remove implemented platforms from `[NotImplemented]` attribute
4. Use platform suffix for platform-specific files

---

## Common Pitfalls

1. **Generated files are regenerated** - never edit `Generated/` folders
2. **Partial methods** used for extensibility: `OnLoaded()`, `OnUnloaded()`
3. **NuGet cache corruption** - delete `%USERPROFILE%\.nuget\packages\uno.ui` if debugging fails
4. **Long paths on Windows** - enable via registry if needed

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
- **Commit cadence**: when the user asks, or when working autonomously on a larger feature, commit in **logical groups** — one focused, Conventional-Commit-formatted commit per coherent chunk that builds clean, rather than one batch at the end. On complex work, these incremental commits also let reviewers follow the progression of the change rather than facing one giant diff. For small one-off edits in an interactive session, leave changes uncommitted unless asked.

### Pull Requests & Issues

When asked to open a PR or file an issue, **base it on the repo's existing templates** (filled out accordingly) — don't free-form:
- **PRs** → fill out every section of `.github/PULL_REQUEST_TEMPLATE.md` and submit it as the body (e.g. `gh pr create --body-file <filled>.md`).
- **Issues** → pick the matching GitHub issue **form** under `.github/ISSUE_TEMPLATE/` (`bug-report`, `enhancement`, `documentation-issue`/`-request`, `samples-issue`/`-request`, `feedback`, `support-request`, `success-story`) and fill its required fields (`gh issue create --template <name>.yml`).
- **Every PR must reference an associated issue** (unless it's a pure-documentation change). Before opening the PR, settle the issue: use the one identified in the conversation; else search for an existing match (`gh issue list --search "<keywords>"`); else create one from the forms above. Put its number on the template's first line — `**GitHub Issue:** closes #XYZ` — so merging the PR auto-closes the issue.

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
