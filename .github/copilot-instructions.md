# Uno Platform Development Instructions

The Uno Platform is a cross-platform .NET UI framework that allows you to build native mobile, web, desktop, and embedded applications from a single C# codebase using WinUI/XAML. It targets Windows, macOS, Linux, iOS, Android, and WebAssembly.

**Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Prerequisites and Environment Setup

Install .NET 9.0 SDK (required for this repository):
```bash
wget https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh --channel 9.0
export PATH="$HOME/.dotnet:$PATH"
```
- Takes 1.5 minutes. NEVER CANCEL. Set timeout to 5+ minutes.

Install required workloads for WebAssembly development:
```bash
dotnet workload install wasm-tools wasm-tools-net8
```
- Takes 2.5 minutes. NEVER CANCEL. Set timeout to 10+ minutes.

For other platforms, install additional workloads as needed:
- Mobile: `dotnet workload install maui`
- iOS: Requires macOS and Xcode
- Android: Requires Android SDK

### Single-Platform Development (Recommended)

**CRITICAL**: Always use single-platform builds with solution filters for efficient development. Multi-platform builds are resource-intensive and often unnecessary.

1. **Setup cross-targeting override** (required before opening any solution):
```bash
cd src
cp crosstargeting_override.props.sample crosstargeting_override.props
```

2. **Edit `crosstargeting_override.props`** to set your target platform:
```xml
<Project>
  <PropertyGroup>
    <!-- Choose one target framework: -->
    <UnoTargetFrameworkOverride>net8.0</UnoTargetFrameworkOverride>              <!-- WebAssembly/Skia -->
    <!-- <UnoTargetFrameworkOverride>net8.0-windows10.0.19041.0</UnoTargetFrameworkOverride>  Windows -->
    <!-- <UnoTargetFrameworkOverride>net8.0-android</UnoTargetFrameworkOverride>  Android -->
    <!-- <UnoTargetFrameworkOverride>net8.0-ios</UnoTargetFrameworkOverride>      iOS -->
    
    <!-- Performance optimizations for development: -->
    <AccelerateBuildsInVisualStudio>true</AccelerateBuildsInVisualStudio>
    <OptimizeImplicitlyTriggeredBuild>true</OptimizeImplicitlyTriggeredBuild>
  </PropertyGroup>
</Project>
```

3. **Open the corresponding solution filter**:
- **WebAssembly**: `Uno.UI-Wasm-only.slnf`
- **Skia (Linux/macOS/Windows)**: `Uno.UI-Skia-only.slnf`
- **Mobile platforms**: `Uno.UI-netcore-mobile-only.slnf`
- **Windows**: `Uno.UI-Windows-only.slnf`
- **Unit Tests only**: `Uno.UI-UnitTests-only.slnf`

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
- **Note**: Some builds may fail in non-Windows environments due to Windows-specific dependencies. This is expected.

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
1. Navigate to `src/SamplesApp/SamplesApp.Wasm/` for WebAssembly
2. Ensure dependencies are restored: `dotnet restore`
3. Run: `dotnet run` or `dotnet serve` for WebAssembly

**Online SamplesApp**: https://aka.platform.uno/wasm-samples-app

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

| Target Framework | Platform | Solution Filter |
|------------------|----------|-----------------|
| `net8.0` or `net9.0` | WebAssembly, Skia | `Uno.UI-Wasm-only.slnf`, `Uno.UI-Skia-only.slnf` |
| `net8.0-windows10.0.19041.0` | Windows | `Uno.UI-Windows-only.slnf` |
| `net8.0-android` | Android | `Uno.UI-netcore-mobile-only.slnf` |
| `net8.0-ios` | iOS | `Uno.UI-netcore-mobile-only.slnf` |
| `net8.0-maccatalyst` | macOS Catalyst | `Uno.UI-netcore-mobile-only.slnf` |

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
cd SamplesApp/SamplesApp.Wasm && dotnet run
```

**Before committing**:
```bash
dotnet build Uno.UI-UnitTests-only.slnf --no-restore
dotnet test Uno.UI/Uno.UI.Tests.csproj --no-build
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