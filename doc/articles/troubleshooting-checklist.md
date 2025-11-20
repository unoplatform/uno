---
uid: Uno.TroubleshootingChecklist
---

# Troubleshooting Checklist

A systematic approach to diagnosing and resolving issues with Uno Platform development. Follow these steps in order when encountering problems.

## Before You Start

Before seeking help or filing an issue, work through this checklist. Most problems can be resolved quickly with these steps.

### ✅ Step 1: Verify Your Environment

**Run uno-check to verify your setup:**

```bash
dotnet tool update -g uno.check
uno-check
```

**What it checks:**

- .NET SDK installation and version
- Required workloads (wasm-tools, mobile workloads)
- IDE extensions
- Platform SDKs (Android, iOS)

**If uno-check fails:**

- Follow its recommendations exactly
- Restart your terminal/IDE after fixes
- Run uno-check again to confirm

### ✅ Step 2: Check .NET and Package Versions

**Verify .NET SDK version:**

```bash
dotnet --version
```

**Expected:** 9.0.x or 10.0.x (10.0 recommended for LTS support)

**Check Uno Platform package versions:**

```bash
dotnet outdated
```

**Common issues:**

- Mixed Uno package versions (keep all at same version)
- Outdated packages (update all Uno.* packages together)
- Using .NET 8 or earlier (upgrade to .NET 9 or 10)

### ✅ Step 3: Clean Your Solution

**Deep clean (solves 70% of build issues):**

```bash
cd src  # or your project directory
dotnet clean

# Remove all bin/obj folders
find . -name "bin" -o -name "obj" | xargs rm -rf  # macOS/Linux
# OR
Get-ChildItem -Include bin,obj -Recurse | Remove-Item -Recurse -Force  # Windows

# Restore and rebuild
dotnet restore
dotnet build
```

**When to clean:**

- After updating packages
- When switching branches
- Before reporting a bug
- When build errors don't make sense

### ✅ Step 4: Restart Everything

Sometimes the simplest solution works:

1. Close your IDE completely
2. Kill any running build processes
3. Clear NuGet cache: `dotnet nuget locals all --clear`
4. Restart your computer (especially on Windows)
5. Reopen and rebuild

## Platform-Specific Checks

### Windows Development

**Visual Studio Issues:**

- [ ] Using Visual Studio 2022 version 17.8+ or Visual Studio 2026
- [ ] Uno Platform extension installed and up to date
- [ ] "Enable Hot Reload" is checked in Tools > Options > Debugging > .NET/C++
- [ ] Running as Administrator if installing tools/SDKs

**WinAppSDK Issues:**

- [ ] Windows SDK 10.0.19041.0 or later installed
- [ ] Project targets `net10.0-windows10.0.19041.0` or `net9.0-windows10.0.19041.0`

### macOS Development

**General:**

- [ ] Using latest macOS version supported by your hardware
- [ ] Xcode installed and up to date
- [ ] Xcode command line tools: `xcode-select --install`
- [ ] Accepted Xcode license: `sudo xcodebuild -license accept`

**iOS Development:**

- [ ] iOS SDK installed through Xcode
- [ ] Provisioning profiles configured
- [ ] Simulator available: `xcrun simctl list`

### Linux Development

**Prerequisites:**

- [ ] .NET 9 or 10 SDK installed
- [ ] GTK 3 or later installed
- [ ] Required build tools: `apt install build-essential` (Ubuntu/Debian)

**Skia Desktop:**

- [ ] libSkiaSharp.so can be found: `ldconfig -p | grep Skia`
- [ ] Required graphics libraries installed

### WebAssembly Development

**Common Issues:**

- [ ] wasm-tools workload installed: `dotnet workload list`
- [ ] Using supported browser (Chrome, Edge, Firefox, Safari)
- [ ] Browser developer tools show no console errors
- [ ] Service worker not caching old version (hard refresh: Ctrl+F5)

## Build Errors

### Error: "Project targets X but only Y is available"

**Cause:** Multi-targeting issue or missing workloads

**Solution:**

1. Use solution filters instead of full solution
2. Set up `crosstargeting_override.props` (copy from `.sample`)
3. Install required workload: `dotnet workload install <workload-name>`

### Error: "Package restore failed"

**Cause:** Network issues, authentication, or corrupted cache

**Solution:**

```bash
# Clear NuGet caches
dotnet nuget locals all --clear

# Check NuGet sources
dotnet nuget list source

# Restore with diagnostic logging
dotnet restore --verbosity detailed
```

### Error: "XAML compilation failed"

**Cause:** Invalid XAML syntax or missing namespace declarations

**Solution:**

1. Check error message for specific line/column
2. Verify all xmlns declarations are correct
3. Ensure x:DataType is specified in DataTemplates using x:Bind
4. Check for typos in property names
5. Use Hot Design<sup>®</sup> to visually inspect and fix XAML  
   <br><sub>Note: Hot Design<sup>®</sup> is available to Pro users, or Community users during the trial period. If you are on the Community tier, sign in to start a free trial.</sub>

### Error: "DependencyObject not found" or Similar Type Errors

**Cause:** Missing or incorrect package references

**Solution:**

```bash
# Ensure core packages are installed
dotnet add package Uno.WinUI --version 5.5.x
dotnet add package Uno.WinUI.DevServer --version 5.5.x

# Check all packages are same version
dotnet outdated
```

## Runtime Errors

### App Crashes on Startup

**Check these in order:**

1. **Exception details:** Check debugger output for exception message
2. **App.xaml.cs:** Verify `UseStudio()` and configuration are correct
3. **Platform-specific code:** Check conditional compilation directives
4. **Missing resources:** Verify all images/assets are included in project
5. **Initialization order:** Ensure services are registered before use

### Hot Reload Not Working

**Checklist:**

- [ ] `UseStudio()` is called in App.xaml.cs
- [ ] Running in Debug mode with debugger attached
- [ ] Uno Platform extension installed and enabled
- [ ] Target framework is net9.0 or net10.0
- [ ] Not modifying code that requires restart (method signatures, new types)

**If still not working:**

1. Stop debugging
2. Clean solution
3. Rebuild
4. Start debugging again
5. Check IDE output window for Hot Reload messages

### UI Not Updating When Data Changes

**Causes and solutions:**

1. **Not implementing INotifyPropertyChanged:**
   - Use `ObservableObject` base class
   - Call `SetProperty(ref _field, value)` in property setters

2. **Not using ObservableCollection:**
   - Replace `List<T>` with `ObservableCollection<T>` for UI-bound lists

3. **Wrong binding mode:**
   - Add `Mode=TwoWay` for properties that should update both ways

4. **Missing x:Bind in XAML:**
   - Use `{x:Bind}` instead of `{Binding}` for better performance

### Navigation Failures

**Common issues:**

1. **Routes not registered:** Check `IRoutes` configuration
2. **ViewModel not registered in DI:** Add to `ConfigureServices`
3. **Navigation service not injected:** Ensure `INavigator` is available
4. **Incorrect navigation syntax:** Use proper method for your scenario

## Performance Issues

### Slow Startup

**Check:**

- [ ] Not loading large resources synchronously on startup
- [ ] Services initialized lazily when possible
- [ ] Images use appropriate DecodePixelWidth/Height
- [ ] Not performing network calls before showing UI

### Slow Scrolling

**Check:**

- [ ] Using `ListView`/`GridView` with virtualization, not `ItemsControl`
- [ ] DataTemplate complexity is reasonable
- [ ] Images in list items have DecodePixelWidth set
- [ ] Not binding to complex computed properties without caching

### High Memory Usage

**Check:**

- [ ] Images are properly decoded to display size
- [ ] Not keeping references to large objects unnecessarily
- [ ] ObservableCollections are cleared when no longer needed
- [ ] Event handlers are unsubscribed properly

## Getting Help

### Information to Gather Before Asking

**Minimum information needed:**

1. **Uno Platform version:** Check your .csproj file
2. **.NET version:** Run `dotnet --version`
3. **Target platform:** iOS, Android, WASM, Windows, etc.
4. **IDE and version:** Visual Studio 2022/2026, VS Code, Rider
5. **Operating system:** Windows 11, macOS Sequoia, Ubuntu 24.04, etc.
6. **Error message:** Full text with stack trace
7. **Steps to reproduce:** What you did before the error occurred

### Create a Minimal Reproduction

If filing an issue:

1. Create new project from template
2. Add minimum code to reproduce the issue
3. Remove unrelated code and packages
4. Verify issue still occurs
5. Share the minimal project

### Where to Get Help

**Official channels:**

- [GitHub Discussions](https://github.com/unoplatform/uno/discussions) - Questions and community help
- [GitHub Issues](https://github.com/unoplatform/uno/issues) - Bug reports and feature requests
- [Discord](https://www.platform.uno/discord) - Real-time chat with community
- [Stack Overflow](https://stackoverflow.com/questions/tagged/uno-platform) - Q&A with `uno-platform` tag

**Before posting:**

1. Search existing issues and discussions
2. Run through this entire checklist
3. Gather all required information
4. Create minimal reproduction if possible

## Quick Diagnostic Commands

**Copy and run these to gather diagnostic info:**

```bash
# System info
dotnet --version
dotnet --list-sdks
dotnet workload list

# Package info
cd YourProject
dotnet list package
dotnet outdated

# Build with detailed output
dotnet build --verbosity detailed

# Check for environment issues
uno-check --verbose
```

## Related Topics

- [Common Mistakes Guide](xref:Uno.CommonMistakes) - Avoid these pitfalls
- [Common Issues](xref:Uno.UI.CommonIssues) - Platform-specific problems
- [Quick Reference](xref:Uno.QuickReference) - Common commands
- [Performance Guide](xref:Uno.UI.Performance) - Optimize your app
- [Hot Design<sup>®</sup> Agent](xref:Uno.HotDesign.Agent) - AI assistant for troubleshooting
