# AGENTS.md

This file provides guidance to AI Agents when working with code in this repository.

## Project Overview

Uno Platform is an open-source .NET UI cross-platform framework for building .NET applications from a single codebase using the WinUI 3 API. It targets Web (WebAssembly), Desktop (Windows, macOS, Linux via Skia), and Mobile (iOS, tvOS, Android).

**Reference these instructions first**. Use specialized agents for deep dives:

| Agent | File | Use For |
|-------|------|---------|
| DependencyProperty | `.github/agents/dependency-property-agent.md` | Adding/modifying DependencyProperties |
| Source Generators | `.github/agents/source-generators-agent.md` | XAML/DependencyObject generator work |
| Runtime Tests | `.github/agents/runtime-tests-agent.md` | Creating and running runtime tests |
| WinUI Porting | `.github/agents/winui-porting-agent.md` | Porting WinUI C++ code to C# |
| DevServer CLI | `.github/agents/devserver-agent.md` | DevServer CLI/Host build, test, MCP proxy |

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

### Key Source Directories

- `src/Uno.UI/` - Core UI framework (141+ WinUI controls)
- `src/Uno.UWP/` - Non-UI APIs
- `src/Uno.Foundation/` - Foundation APIs
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

**2. Edit `crosstargeting_override.props`:**
```xml
<Project>
  <PropertyGroup>
    <!-- Choose ONE target: -->
    <UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride>              <!-- WebAssembly/Skia -->
    <!-- <UnoTargetFrameworkOverride>net10.0-android</UnoTargetFrameworkOverride>  Android -->
    <!-- <UnoTargetFrameworkOverride>net10.0-ios</UnoTargetFrameworkOverride>      iOS -->
    <!-- <UnoTargetFrameworkOverride>net10.0-windows10.0.19041.0</UnoTargetFrameworkOverride> Windows -->
  </PropertyGroup>
</Project>
```

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

### Root-Cause First Debugging Protocol (MANDATORY)

When fixing crashes, rendering issues, or selection/indexing bugs,
agents must follow this order:

1. **Reproduce first**
   - Capture exact repro steps and expected vs actual behavior.
   - Keep one known-good repro path and rerun it after each meaningful change.

2. **Identify the broken invariant**
   - Prefer state/lifecycle invariants over symptom-level checks.
   - For pipelines that derive secondary state, verify those derived
      structures (for example: maps, indices, caches, or metadata)
      are rebuilt from final post-mutation state.

3. **Fix root cause before adding guards**
    - Do not lead with null/index guards as the primary fix if
       ownership/lifecycle is incorrect.
    - Defensive guards are allowed only after root-cause correction,
       and only as secondary hardening.

4. **Prove correctness with targeted tests**
   - Add/extend tests that fail before and pass after the root fix.
   - Cover both the triggering scenario and one adjacent regression scenario.

5. **Validate with runtime behavior, not compile only**
   - Run the closest runtime or integration path available for the changed area.
   - If full runtime execution is not possible in the environment,
       state that explicitly and provide the exact command(s) for
       maintainers to run.

6. **Communicate confidence accurately**
   - Separate: (a) code review assessment,
       (b) compile validation, (c) runtime validation.
   - Never present guard-only mitigation as a complete root-cause fix.

**Anti-pattern to avoid:**

- Symptom-driven patching that accumulates bounds checks while
   stale or invalid intermediate state remains possible.

### Validation Evidence Protocol (MANDATORY)

For bug fixes and PR reviews, agents must report validation evidence
with explicit labels:

- **Code review assessment**: What logic appears correct by inspection.
- **Compile validation**: Which project/solution was built, and result.
- **Runtime validation**: Which app/test path was executed, and result.

Rules:

1. Do not present compile-only checks as runtime validation.
2. If runtime execution is skipped or blocked, state that explicitly
   and provide exact commands to run.
3. When reviewing another PR, distinguish between confidence from
   diff inspection vs. confidence from local execution.

### Diagnosis Bias Checks (MANDATORY)

Before proposing a crash fix, agents must run these checks to avoid
choosing symptom-level guards over the real fix:

1. **Invariant checkpoint before patching**
    - Name the invariant likely broken (ownership, lifecycle,
       index/map coherence, post-mutation consistency).
   - If no invariant is identified, do not claim a root-cause fix.

2. **Mutation-point review**
    - Inspect where state is created/trimmed/reordered and verify all
       dependent structures are refreshed there.
    - Prefer correcting the mutation point over adding protections in
       downstream consumers.

3. **Guard classification**
    - Explicitly label each proposed change as either
       `root-cause fix` or `defensive hardening`.
   - Guard-only changes must not be presented as complete resolution.

### Validation Checklist

Run these after making changes:

1. **Build**: `dotnet build Uno.UI-UnitTests-only.slnf --no-restore`
2. **Unit tests**: `dotnet test Uno.UI/Uno.UI.Tests.csproj --no-build`
3. **Runtime tests** (UI changes): See [runtime tests agent](.github/agents/runtime-tests-agent.md)
4. **Sample app** (visual changes): `cd src/SamplesApp/SamplesApp.Wasm && dotnet run`

### SamplesApp: Register XAML files (CRITICAL)

When adding XAML samples to the `SamplesApp`, you must register the XAML and its code-behind in `src/SamplesApp/UITests.Shared/UITests.Shared.projitems` or the sample will not appear in the app.

- ALWAYS add your XAML file to `src/SamplesApp/UITests.Shared/UITests.Shared.projitems`
- Without this registration, your sample will NOT be visible in SamplesApp
- Add both the XAML page and the code-behind file. Example:

```xml
<Page Include="$(MSBuildThisFileDirectory)YourFolder\YourSample.xaml">
  <SubType>Designer</SubType>
  <Generator>MSBuild:Compile</Generator>
</Page>
<Compile Include="$(MSBuildThisFileDirectory)YourFolder\YourSample.xaml.cs">
  <DependentUpon>YourSample.xaml</DependentUpon>
</Compile>
```

Sample creation checklist:
1. Create your sample XAML and code-behind under an appropriate folder in `UITests.Shared`.
2. Add the `[Uno.UI.Samples.Controls.Sample]` attribute to the code-behind class.
3. Register the XAML and code-behind in `UITests.Shared.projitems` (see XML above).
4. Build and run `SamplesApp` to verify the sample appears.

Theming guideline (brief): prefer `{ThemeResource}` for backgrounds/foregrounds so samples work in light and dark themes.

### Runtime Tests (Preferred for UI)

Add tests to `Uno.UI.RuntimeTests`. Key helpers:
- `WindowHelper.WindowContent` - Add elements to visual tree
- `await WindowHelper.WaitForLoaded(element)` - Wait for load
- `await WindowHelper.WaitForIdle()` - Wait for UI to settle

**Run tests headlessly:**
```bash
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0
cd src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

See `.github/agents/runtime-tests-agent.md` for detailed patterns.

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

See `.github/agents/dependency-property-agent.md` for full patterns. Quick template:

```csharp
public static DependencyProperty MyPropertyProperty { get; } =
    DependencyProperty.Register(nameof(MyProperty), typeof(MyType), typeof(MyControl),
        new FrameworkPropertyMetadata(default(MyType), OnMyPropertyChanged));

public MyType MyProperty
{
    get => (MyType)GetValue(MyPropertyProperty);
    set => SetValue(MyPropertyProperty, value);
}
```

### Code Style

- **Braces**: Always use, even for single-line conditionals
- **Indentation**: Tabs (configured in .editorconfig)
- **Extension methods**: In `[TypeName]Extensions.cs`, mark `internal`

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

### Specialized Agents
- `.github/agents/dependency-property-agent.md` - DependencyProperty patterns
- `.github/agents/source-generators-agent.md` - XAML/DependencyObject generators
- `.github/agents/runtime-tests-agent.md` - Runtime test execution
- `.github/agents/winui-porting-agent.md` - WinUI C++ to C# porting
- `.github/agents/devserver-agent.md` - DevServer CLI/Host maintenance

### Community
- [Discord](https://platform.uno/discord)
- [Samples App](https://aka.platform.uno/wasm-samples-app)
