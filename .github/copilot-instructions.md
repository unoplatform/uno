# Uno Platform Development Instructions

The Uno Platform is a cross-platform .NET UI framework that allows you to build native mobile, web, desktop, and embedded applications from a single C# codebase using WinUI/XAML. It targets Windows, macOS, Linux, iOS, Android, and WebAssembly.

**Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Prerequisites and Environment Setup

Install the latest stable .NET SDK (currently .NET 10.0 for this repository, with .NET 9.0 also supported):
```bash
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh && chmod +x /tmp/dotnet-install.sh && /tmp/dotnet-install.sh --channel 10.0
export PATH="$HOME/.dotnet:$PATH"
```
- Always use the latest stable supported .NET version for development
- Update the channel number as new stable versions are released
- Takes 1.5 minutes. NEVER CANCEL. Set timeout to 5+ minutes.

Install required workloads for WebAssembly development:
```bash
dotnet workload install wasm-tools wasm-tools-net9 wasm-tools-net10
```
- Update workload versions (e.g., `wasm-tools-net11`) as new .NET versions are released
- Takes 2.5 minutes. NEVER CANCEL. Set timeout to 10+ minutes.

For other platforms, install additional workloads as needed:
- Mobile: `dotnet workload install maui`
- iOS: Requires macOS and Xcode
- Android: Requires Android SDK

### Single-Platform Development (Recommended)

**CRITICAL**: Always use single-platform builds with solution filters for efficient development, favor Skia desktop first because it builds and runs faster, unless a specific platform is requested. Multi-platform builds are resource-intensive and often unnecessary.

1. **Setup cross-targeting override** (required before opening any solution):
```bash
cd src
cp crosstargeting_override.props.sample crosstargeting_override.props
```

2. **Edit `crosstargeting_override.props`** to set your target platform:
```xml
<Project>
  <PropertyGroup>
    <!-- Choose one target framework (update version number for latest stable .NET): -->
    <UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride>              <!-- WebAssembly/Skia (current) -->
    <!-- <UnoTargetFrameworkOverride>net9.0</UnoTargetFrameworkOverride>           WebAssembly/Skia (previous) -->
    <!-- <UnoTargetFrameworkOverride>net10.0-windows10.0.19041.0</UnoTargetFrameworkOverride>  Windows (current) -->
    <!-- <UnoTargetFrameworkOverride>net9.0-windows10.0.19041.0</UnoTargetFrameworkOverride>   Windows (previous) -->
    <!-- <UnoTargetFrameworkOverride>net10.0-android</UnoTargetFrameworkOverride>  Android (current) -->
    <!-- <UnoTargetFrameworkOverride>net9.0-android</UnoTargetFrameworkOverride>   Android (previous) -->
    <!-- <UnoTargetFrameworkOverride>net10.0-ios</UnoTargetFrameworkOverride>      iOS (current) -->
    <!-- <UnoTargetFrameworkOverride>net9.0-ios</UnoTargetFrameworkOverride>       iOS (previous) -->
    
    <!-- Performance optimizations for development: -->
    <AccelerateBuildsInVisualStudio>true</AccelerateBuildsInVisualStudio>
    <OptimizeImplicitlyTriggeredBuild>true</OptimizeImplicitlyTriggeredBuild>
  </PropertyGroup>
</Project>
```

3. **Open the corresponding solution filter**:
- **WebAssembly (native)**: `Uno.UI-Wasm-only.slnf`
- **Skia variants (WASM, Android, iOS Skia implementations)**: `Uno.UI-Skia-only.slnf`
- **Mobile platforms (native Android, iOS)**: `Uno.UI-netcore-mobile-only.slnf`
- **Windows**: `Uno.UI-Windows-only.slnf`
- **Unit Tests only**: `Uno.UI-UnitTests-only.slnf`

**Platform Clarifications:**
- **Native variants**: WebAssembly native (`Uno.UI-Wasm-only.slnf`), native mobile platforms (`Uno.UI-netcore-mobile-only.slnf`)
- **Skia variants**: All Skia implementations including WASM, Android, and iOS Skia variants are included in `Uno.UI-Skia-only.slnf`

### Building and Testing

**Restore packages** for your chosen platform:
```bash
cd src
dotnet restore Uno.UI-Wasm-only.slnf
```
- Takes 50-60 seconds. NEVER CANCEL. Set timeout to 10+ minutes.

**Build the solution**:
```bash
dotnet build Uno.UI-Wasm-only.slnf --no-restore -p:Configuration=Debug
```
- Takes 3-5 minutes for WebAssembly. NEVER CANCEL. Set timeout to 15+ minutes.
- **Note**: The `Uno.UI-Windows-only.slnf` solution filter and any projects targeting `net10.0-windows10.0.19041.0` or `net9.0-windows10.0.19041.0` (or other Windows-specific target frameworks) will fail to build on non-Windows environments due to Windows-specific dependencies. On macOS, Linux, or CI environments, use the `Uno.UI-Wasm-only.slnf`, `Uno.UI-Skia-only.slnf`, or `Uno.UI-netcore-mobile-only.slnf` solution filters for cross-platform development. If you need to build or test Windows-specific projects, use a Windows machine or a Windows VM. For unit tests, ensure you are using a solution filter and target framework compatible with your OS.

**Run unit tests**:
```bash
cd src
dotnet test Uno.UI/Uno.UI.Tests.csproj --logger "console;verbosity=normal"
```
- Takes 40-60 seconds to build and run. NEVER CANCEL. Set timeout to 10+ minutes.

**Clean build** (when having issues):
```bash
cd src
dotnet clean && rm -rf */bin */obj **/bin **/obj
```

### Sample Applications

**Running SamplesApp locally**:

**Native WebAssembly**:
1. Navigate to `src/SamplesApp/SamplesApp.Wasm/` for native WebAssembly
2. Ensure dependencies are restored: `dotnet restore`
3. Run: `dotnet run` for WebAssembly

**Skia WebAssembly**:
1. Navigate to `src/SamplesApp/SamplesApp.Skia.WebAssembly.Browser/` for Skia WebAssembly
2. Ensure dependencies are restored: `dotnet restore`
3. Run: `dotnet run` for Skia WebAssembly

**Online SamplesApp**: https://aka.platform.uno/wasm-samples-app

### XAML Sample Guidelines

When adding XAML samples to the SamplesApp, follow these theming guidelines to ensure samples work well in both light and dark themes:

**Use ThemeResource for UI Elements:**
- **Always use `{ThemeResource}` colors** for backgrounds and foregrounds in UI elements
- This ensures proper theme adaptation between light and dark modes
- Examples:
  ```xml
  <!-- Good: Uses theme-aware colors -->
  <Border Background="{ThemeResource SystemControlBackgroundChromeMediumBrush}">
      <TextBlock Foreground="{ThemeResource SystemControlForegroundBaseHighBrush}" 
                 Text="Sample content" />
  </Border>
  
  <!-- Avoid: Hard-coded colors that don't adapt to themes -->
  <Border Background="White">
      <TextBlock Foreground="Black" Text="Sample content" />
  </Border>
  ```

**Exception for Test/Demo UI:**
- **Test UI and demonstrations can use explicit colors** when showing specific functionality
- Use clear, high-contrast colors that are legible in both themes
- Examples where explicit colors are acceptable:
  ```xml
  <!-- Acceptable: Shape samples demonstrating colors -->
  <Rectangle Fill="Red" Width="100" Height="100" />
  <Rectangle Fill="Blue" Width="100" Height="100" />
  
  <!-- Acceptable: Color picker demonstrations -->
  <Button Background="Orange" Content="Orange Button Demo" />
  ```

**Common ThemeResource Colors:**
- `SystemControlBackgroundChromeMediumBrush` - Standard background
- `SystemControlForegroundBaseHighBrush` - Primary text
- `SystemControlBackgroundAltHighBrush` - Alternate background
- `SystemAccentColorBrush` - Accent color
- `SystemControlBackgroundBaseLowBrush` - Subtle background

This ensures a consistent, professional appearance across all theme modes while maintaining the flexibility to demonstrate specific color-related functionality when needed.

## Validation

**ALWAYS run these validation steps after making changes:**

1. **Build validation**:
```bash
cd src && dotnet build Uno.UI-UnitTests-only.slnf --no-restore
```

2. **Unit test validation**:
```bash
cd src && dotnet test Uno.UI/Uno.UI.Tests.csproj --no-build
```

3. **Sample app validation** (for UI changes):
```bash
cd src/SamplesApp/SamplesApp.Wasm && dotnet run
```

4. **Documentation validation** (for doc changes):
```bash
cd doc && npm install && npm run build
```

5. **Runtime test validation** (preferred for UI features):

Runtime tests are generally preferred over unit tests for testing UI features. Add new tests to the `Uno.UI.RuntimeTests` project and run them at runtime on the target platform.

**Running runtime tests via SamplesApp**:
1. Build and run the SamplesApp for your target platform (e.g., `SamplesApp.Skia.WebAssembly.Browser` for WebAssembly)
2. Click the test runner button in the top left corner of the application
3. In the test interface, enter the name of your test
4. Click "Run" to execute the test
5. View results in the log output showing passed/failed tests with detailed information

**Adding new runtime tests**:
```bash
# Navigate to the runtime tests project
cd src/Uno.UI.RuntimeTests

# Add your test class following existing patterns
# Tests should be marked with [TestMethod] attribute
# Use the Given_When_Then naming convention
```

## Common Tasks

### Repository Structure

**Key directories**:
- `src/` - Main source code and solution filters
- `src/SamplesApp/` - Sample applications for all platforms  
- `doc/` - Documentation source files
- `build/` - Build scripts and CI configuration
- `.github/workflows/` - GitHub Actions CI/CD pipelines

### Important Files

**ls -la [src]**:
```
crosstargeting_override.props     - Target framework override (create from .sample)
Uno.UI-Wasm-only.slnf            - WebAssembly solution filter
Uno.UI-Skia-only.slnf            - Skia (desktop) solution filter  
Uno.UI-netcore-mobile-only.slnf  - Mobile platforms solution filter
Uno.UI-Windows-only.slnf         - Windows solution filter
Uno.UI-UnitTests-only.slnf       - Unit tests solution filter
Uno.UI.sln                       - Full solution (heavy, avoid)
```

### Available Platform Targets

*Note: The repository supports both .NET 10.0 (current) and .NET 9.0 (previous). Use .NET 10.0 for new development.*

| Target Framework | Platform | Solution Filter |
|------------------|----------|-----------------|
| `net10.0` | WebAssembly (current) | `Uno.UI-Wasm-only.slnf` |
| `net9.0` | WebAssembly (previous) | `Uno.UI-Wasm-only.slnf` |
| `net10.0` | Skia (Linux/macOS, current) | `Uno.UI-Skia-only.slnf` |
| `net9.0` | Skia (Linux/macOS, previous) | `Uno.UI-Skia-only.slnf` |
| `net10.0-windows10.0.19041.0` | Windows (current) | `Uno.UI-Windows-only.slnf` |
| `net9.0-windows10.0.19041.0` | Windows (previous) | `Uno.UI-Windows-only.slnf` |
| `net10.0-android` | Android (current) | `Uno.UI-netcore-mobile-only.slnf` |
| `net9.0-android` | Android (previous) | `Uno.UI-netcore-mobile-only.slnf` |
| `net10.0-ios` | iOS (current) | `Uno.UI-netcore-mobile-only.slnf` |
| `net9.0-ios` | iOS (previous) | `Uno.UI-netcore-mobile-only.slnf` |
| `net10.0-maccatalyst` | macOS Catalyst (current) | `Uno.UI-netcore-mobile-only.slnf` |
| `net9.0-maccatalyst` | macOS Catalyst (previous) | `Uno.UI-netcore-mobile-only.slnf` |

### Common Build Issues

**"Assets file doesn't have a target"**: Delete `obj/` and `bin/` folders, then restore
**"Windows XAML targets not found"**: Expected on Linux/macOS, use WebAssembly or Skia targets instead  
**"Package restore timeout"**: Network issue, retry with longer timeout
**"Solution filter fails"**: Ensure `crosstargeting_override.props` target matches the solution filter

### Development Workflow Commands

**Start new feature work**:
```bash
cd src
cp crosstargeting_override.props.sample crosstargeting_override.props
# Edit crosstargeting_override.props to set target framework
dotnet restore Uno.UI-[Platform]-only.slnf
dotnet build Uno.UI-[Platform]-only.slnf --no-restore
```

**Test changes**:
```bash
dotnet test Uno.UI/Uno.UI.Tests.csproj
# For runtime tests (preferred for UI features):
cd SamplesApp/SamplesApp.Wasm && dotnet run
# Use the test runner button in SamplesApp to run runtime tests
```

**Before committing**:
```bash
dotnet build Uno.UI-UnitTests-only.slnf --no-restore
dotnet test Uno.UI/Uno.UI.Tests.csproj --no-build
# Run relevant runtime tests via SamplesApp for UI changes
```

## Documentation

- **Building Uno**: `doc/articles/uno-development/building-uno-ui.md`
- **Contributing Guide**: `doc/articles/uno-development/contributing-intro.md`  
- **Sample Apps Guide**: `doc/articles/uno-development/working-with-the-samples-apps.md`
- **Creating Tests**: `doc/articles/contributing/guidelines/creating-tests.md`

## Timing Expectations

- **SDK Installation**: 1-2 minutes
- **Workload Installation**: 2-3 minutes  
- **Package Restore**: 50-60 seconds
- **Unit Test Build**: 40-60 seconds
- **Full Platform Build**: 3-15 minutes (varies by platform)
- **Sample App Startup**: 1-2 minutes

**NEVER CANCEL** long-running operations. Build times are normal and expected. Always set timeouts of 15+ minutes for builds and 10+ minutes for restores.

## Commit Guidelines

**MANDATORY**: All commits MUST follow the [Conventional Commits](https://www.conventionalcommits.org/) specification as enforced by CI. This ensures proper automatic release note generation and semantic versioning.

### Required Commit Format

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Common Commit Types

- **fix**: Bug fixes (PATCH version bump)
- **feat**: New features (MINOR version bump)
- **docs**: Documentation changes only
- **test**: Adding or updating tests
- **perf**: Performance improvements
- **chore**: Maintenance tasks, tooling, dependencies
- **ci**: CI/CD pipeline changes
- **style**: Code style changes (formatting, etc.)
- **refactor**: Code changes that neither fix bugs nor add features

### Examples

```bash
# Initial work (always start with this)
git commit -m "chore: Initial work"

# Bug fixes
git commit -m "fix: Resolve null reference in TextBox control"
git commit -m "fix(android): Handle back button navigation correctly"

# New features
git commit -m "feat: Add support for custom border radius"
git commit -m "feat(ios): Implement native picker control"

# Documentation
git commit -m "docs: Update build instructions for .NET 10.0"

# Breaking changes (note the !)
git commit -m "feat!: Remove deprecated WinUI 2.x compatibility layer"
```

### Breaking Changes

For breaking changes, add `!` after the type/scope:
```bash
git commit -m "feat!: Remove deprecated API methods"
```

### Additional Guidelines

- Keep the description under 50 characters when possible
- Use imperative mood ("Add feature" not "Added feature")
- Reference issues when relevant: `fix: Resolve layout issue (fixes #12345)`
- All commits will be validated by CI and must pass conventional commit format checks
- Iterate and refine: Embrace rapid iteration, don't worry about perfect designs initially; improve them step by step.

For complete guidelines, see: `doc/articles/contributing/guidelines/conventional-commits.md`