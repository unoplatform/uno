# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Uno Platform is an open-source cross-platform framework (Apache 2.0) for building .NET applications from a single codebase that run natively on Web (WebAssembly), Desktop (Windows, macOS, Linux via Skia), Mobile (iOS, Android via .NET), and Embedded systems. It uses the WinUI 3 API surface, allowing developers to leverage existing C# and XAML skills across all platforms.

## Building Uno.UI

### Prerequisites
- Visual Studio 2022 (18.0+) with:
  - ASP.NET and Web Development workload
  - .NET Multi-Platform App UI development workload
  - .NET desktop development workload
  - UWP Development (install all recent UWP SDKs starting from 10.0.19041)
- Install all Android SDKs starting from 7.1 (via Tools → Android → Android SDK manager)
- Run [Uno.Check](https://github.com/unoplatform/uno.check) to setup .NET Android/iOS workloads
- Latest [.NET SDK](https://aka.ms/dotnet/download)

### Single-Target Build (Recommended)

Building for a single target platform is considerably faster and less RAM-intensive. Follow these steps:

1. **Close all Visual Studio instances** (changing target while VS is open can cause crashes)
2. Copy `src/crosstargeting_override.props.sample` to `src/crosstargeting_override.props`
3. In `crosstargeting_override.props`, uncomment and set `<UnoTargetFrameworkOverride>xxx</UnoTargetFrameworkOverride>` to your desired platform:
   - `net10.0-windows10.0.19041.0` - Windows → use `Uno.UI-Windows-only.slnf`
   - `net10.0` - WebAssembly/Skia → use `Uno.UI-Wasm-only.slnf` or `Uno.UI-Skia-only.slnf`
   - `net10.0-ios` - iOS Native → use `Uno.UI-netcoremobile-only.slnf`
   - `net10.0-android` - Android Native → use `Uno.UI-netcoremobile-only.slnf`
   - `net10.0-maccatalyst` - macOS Catalyst → use `Uno.UI-netcoremobile-only.slnf`
4. Open the corresponding solution filter (`.slnf`) file from the `src` folder
5. Build the appropriate project to verify:
   - iOS/Android native: `Uno.UI` project
   - WebAssembly/native: `Uno.UI.Runtime.WebAssembly` project
   - Skia: corresponding `Uno.UI.Runtime.Skia.[Win32|X11|macOS|iOS|Android|Wpf]` project

**Important**: Close VS before changing the `UnoTargetFrameworkOverride` value.

### Troubleshooting Build Issues
- Ensure you're on the latest master commit
- Close VS 2022, delete `src/.vs` folder, rebuild
- If `.vs` deletion doesn't help, run `git clean -fdx` (after closing VS)
- Verify `UnoTargetFrameworkOverride` matches your solution filter
- Ensure Windows SDK `19041` is installed
- Enable Windows long paths: `reg ADD HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem /v LongPathsEnabled /t REG_DWORD /d 1`

## Testing

### Running Runtime Tests (Uno.UI.RuntimeTests)

Platform-runtime unit tests run in the platform environment using real Uno.UI binaries. They run from within SamplesApp:

1. Build and launch SamplesApp (use tablet layout on mobile if possible)
2. Navigate to 'Unit Tests' → 'Unit Tests Runner' (or click top-left button)
3. (Optional) Add a filter string to run specific tests
4. Press 'Run'

**Creating Runtime Tests:**
- Test files location: `Uno.UI.RuntimeTests/Tests/Namespace_In_Snake_Case/Given_ControlName.cs`
- Use `[TestClass]` on class and `[TestMethod]` on methods
- Use naming convention: `When_Your_Scenario` (Given-When-Then style)
- Add `[RunsOnUIThread]` attribute for UI-related tests (on method or class)
- Use `WindowHelper` class helpers:
  - `WindowHelper.WindowContent` - Add/remove elements from visual tree
  - `await WindowHelper.WaitForLoaded(element)` - Wait for element to load/measure/arrange
  - `await WindowHelper.WaitForIdle()` - Wait for UI thread to settle
  - `await WindowHelper.WaitFor(() => condition)` - Wait for specific condition
- Always close popups in `try/finally` to avoid interfering with other tests

### Running SamplesApp

The SamplesApp contains UI and non-UI samples for manual testing and automated UI tests.

1. Open Uno.UI with correct target override and solution filter for your platform
2. Select `SamplesApp.[Platform]` as startup app (e.g., `SamplesApp.iOS`)
3. Run the app

**Adding New Samples:**
- Location: `UITests.Shared` project → `Namespace_In_Snake_Case/ControlNameTests`
- Create a `UserControl` with meaningful name
- Add `[Uno.UI.Samples.Controls.Sample]` attribute to code-behind
- Sample attribute auto-determines category/name from namespace/classname (or specify manually)

## Common Commands

### Restore and Build
```bash
# Restore and build all packages (CI)
msbuild build/Uno.UI.Build.csproj /t:BuildCIPackages /p:Configuration=Release

# Restore and build reference assemblies
msbuild build/filters/Uno.UI-packages-reference.slnf /t:Restore;Build /p:Configuration=Release
```

### Running SyncGenerator Tool
Synchronizes WinRT/WinUI APIs with Uno implementations (Windows only):
```bash
# From uno\build folder (not uno\src\build)
run-api-sync-tool.cmd
```

### Running Tests
```bash
# Build and run runtime tests through SamplesApp (see above)

# WebAssembly snapshot tests
cd src/SamplesApp/SamplesApp.Wasm.UITests
npm i
# Run tests via F5 in VS or build SamplesApp.Wasm.UITests.njsproj
```

## Architecture and Code Organization

### Platform-Specific Code Patterns

Uno uses file suffixes to organize platform-specific implementations:
- `.Android.cs` - Android-specific
- `.iOS.cs` / `.UIKit.cs` - iOS-specific
- `.Apple.cs` - Apple platforms (iOS/macOS/tvOS)
- `.wasm.cs` / `.Interop.wasm.cs` - WebAssembly
- `.skia.cs` - Skia desktop
- `.reference.cs` - Reference implementation
- `.unittests.cs` - Test variant
- `.crossruntime.cs` - Shared runtime code

### Core Architecture Patterns

1. **Multi-Target Platform Abstraction**: Single C#/XAML codebase → WinUI 3 API → Platform-specific runtimes (Skia, WebAssembly, Native)

2. **Rendering Engines**:
   - **Skia**: Cross-platform rendering (Desktop Win32/WPF, macOS/AppKit, Linux/X11/FrameBuffer, embedded)
   - **WebAssembly**: Browser-based via .NET WASM Runtime (UIElements map 1:1 to DOM elements)
   - **Native**: Platform controls (UIKit on iOS, Android Framework on Android)

3. **Platform-Specific Base Classes**:
   - **Android**: `UIElement` inherits from `Android.Views.ViewGroup` → `Uno.UI.UnoViewGroup` (Java) → `Uno.UI.Controls.BindableView` → `UIElement`
   - **iOS**: `UIElement` inherits from `UIKit.UIView` → `Uno.UI.Controls.BindableUIView` → `UIElement`
   - **WebAssembly**: UIElements map directly to DOM elements (default tag: "div")
   - **Skia**: UIElements use `IRenderer` interface for rendering pipeline

4. **XAML Compilation**: XAML files are parsed to C# code via source generators (not .xbf like WinUI)
   - `XamlFileGenerator` in `Uno.UI.SourceGenerators`
   - Generates `InitializeComponent()` and named field backing
   - Supports x:Bind with compile-time type safety

5. **DependencyObject Implementation**:
   - On Android/iOS/macOS, `DependencyObject` is an **interface** (not base class) since `UIElement` inherits from native view classes
   - Source generators provide the implementation via `DependencyObjectGenerator`
   - Generated code provides `__Store` (DependencyObjectStore) for property storage

6. **Project Organization**: Most libraries have 5 variants:
   - Reference (API surface, netstandard2.0)
   - Skia (Desktop rendering)
   - WebAssembly (Browser)
   - NetCoreMobile (iOS/Android)
   - Tests (Unit tests)

### Key Source Directories

- `src/Uno.UI/` - Core UI framework (141+ WinUI controls)
- `src/Uno.Foundation/` - Foundation APIs
- `src/Uno.UI.Runtime.Skia.*/` - Platform-specific Skia runtimes
- `src/Uno.UI.Runtime.WebAssembly/` - WebAssembly runtime
- `src/SourceGenerators/` - C# source generators (XAML parser, DependencyProperty generator)
- `src/Uno.UI.BindingHelper.Android/` - Java interop classes for performance (UnoViewGroup)
- `src/Uno.UI/Mixins/` - Platform-specific extension methods and mixins
- `src/SamplesApp/` - Sample applications and UI tests
- `src/Uno.UI.RuntimeTests/` - Platform runtime tests
- `src/AddIns/` - Extensions (Lottie, MSAL, Maps, SVG, MediaPlayer)
- `build/` - Build infrastructure and NuGet configuration
- `doc/` - DocFx documentation

### NotImplemented Stubs

Uno auto-generates stubs for WinUI APIs not yet implemented, marked with `[Uno.NotImplemented]` attribute. These allow compilation but warn if referenced in app code. Located in `Generated` folders, generated by SyncGenerator tool.

## DependencyProperty System

### Standard DependencyProperty Pattern

DependencyProperties are the backbone of the Uno property system. Use this pattern when adding new properties to controls:

```csharp
#region MyProperty DependencyProperty

public static DependencyProperty MyPropertyProperty { get; } =
    DependencyProperty.Register(
        nameof(MyProperty),           // Property name
        typeof(MyType),                // Property type
        typeof(MyControl),             // Owner type
        new FrameworkPropertyMetadata(
            defaultValue: default(MyType),
            propertyChangedCallback: OnMyPropertyChanged,
            coerceValueCallback: null,
            flags: FrameworkPropertyMetadataOptions.AffectsMeasure  // Optional
        )
    );

public MyType MyProperty
{
    get => (MyType)GetValue(MyPropertyProperty);
    set => SetValue(MyPropertyProperty, value);
}

private static void OnMyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
{
    var owner = sender as MyControl;
    owner?.OnMyPropertyChangedInstance(args);
}

private void OnMyPropertyChangedInstance(DependencyPropertyChangedEventArgs args)
{
    // Implementation
}

#endregion
```

### FrameworkPropertyMetadataOptions (Common Flags)

```csharp
// Layout impact
FrameworkPropertyMetadataOptions.AffectsMeasure  // Triggers InvalidateMeasure()
FrameworkPropertyMetadataOptions.AffectsArrange  // Triggers InvalidateArrange()
FrameworkPropertyMetadataOptions.AffectsRender   // Triggers visual update

// Value conversion
FrameworkPropertyMetadataOptions.AutoConvert     // Auto-convert from string

// Property inheritance
FrameworkPropertyMetadataOptions.Inherits        // Value inherits from parent

// Child tracking
FrameworkPropertyMetadataOptions.LogicalChild    // Marks as logical child
```

### Attached Properties

Used for layout panels and utility properties:

```csharp
public static DependencyProperty LeftProperty { get; } =
    DependencyProperty.RegisterAttached(
        "Left",
        typeof(double),
        typeof(Canvas),
        new FrameworkPropertyMetadata(
            0.0,
            FrameworkPropertyMetadataOptions.AffectsArrange
        )
    );

[DynamicDependency(nameof(GetLeft))]
[DynamicDependency(nameof(SetLeft))]
public static double GetLeft(DependencyObject obj) =>
    (double)obj.GetValue(LeftProperty);

public static void SetLeft(DependencyObject obj, double value) =>
    obj.SetValue(LeftProperty, value);
```

**Note**: Use `[DynamicDependency]` attributes to aid trimming.

### Property Coercion

For properties that need validation or constraint enforcement:

```csharp
private static object CoerceValue(DependencyObject d, object baseValue, DependencyPropertyValuePrecedences precedence)
{
    var value = (double)baseValue;
    var owner = (RangeBase)d;

    // Clamp to min/max
    if (value < owner.Minimum) return owner.Minimum;
    if (value > owner.Maximum) return owner.Maximum;
    return value;
}

public static DependencyProperty ValueProperty { get; } =
    DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(RangeBase),
        new FrameworkPropertyMetadata(0.0, null, CoerceValue)  // 3rd param is coercion
    );
```

### GeneratedDependencyProperty Attribute

For source-generated dependency properties (simpler syntax):

```csharp
[GeneratedDependencyProperty(
    DefaultValue = 0.0d,
    AttachedBackingFieldOwner = typeof(UIElement),
    Attached = true,
    Options = FrameworkPropertyMetadataOptions.AffectsArrange)]
public static double Top { get; }
```

## Source Generators Architecture

### XAML Generation Pipeline

Located in: `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/`

**Pipeline Flow:**
```
XAML Files → Parse (parallel) → Build Maps → Generate Files (parallel) → Output C#
```

**Key Classes:**
- `XamlCodeGenerator` - Entry point (`ISourceGenerator`)
- `XamlFileGenerator` - Generates code for individual XAML files
- `XamlFileParser` - Parses XAML with caching (1-hour TTL)
- `XamlObjectDefinition` - Represents parsed XAML object tree
- `XamlMemberDefinition` - Represents XAML properties/events
- `NameScope` - Tracks named elements, backing fields, components

**Generated Code Includes:**
- `InitializeComponent()` method
- Named element backing fields
- Resource dictionary singletons
- x:Bind expression methods
- Component lazy-loading stubs

**Optimization Features:**
- File-level parse caching with checksum validation
- Parallel XAML parsing (except when debugger attached)
- Lazy component generation for `x:Load` marked elements
- `StringBuilderBasedSourceText` to avoid LOH allocations

### x:Bind Expression Generation

Located in: `XamlGenerator/Utils/XBindExpressionParser.cs`

Generates type-safe, compile-time bound expressions:

```xaml
<!-- XAML -->
<TextBlock Text="{x:Bind ViewModel.Name, Mode=OneWay}" />

<!-- Generated C# -->
private bool TryGetInstance_xBind_0(MyViewModel vm, out object result)
{
    result = null;
    if (vm == null) return false;
    result = vm.Name;
    return true;
}
```

**Features:**
- Null-safe navigation operators (`?.`)
- Property change tracking for OneWay/TwoWay bindings
- Method invocations with parameters
- Type conversions
- Fallback values

### DependencyObject Generation

Located in: `src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/`

Generates boilerplate for classes implementing `IDependencyObject`:

```csharp
// Generated code
partial class MyControl : IDependencyObject, IDependencyObjectStoreProvider
{
    private readonly DependencyObjectStore __Store = new DependencyObjectStore(this);

    DependencyObjectStore IDependencyObjectStoreProvider.Store => __Store;

    public object GetValue(DependencyProperty dp) => __Store.GetValue(dp);
    public void SetValue(DependencyProperty dp, object value) => __Store.SetValue(dp, value);
    public void ClearValue(DependencyProperty dp) => __Store.ClearValue(dp);

    // Platform-specific lifecycle hooks
    partial void OnAttachedToWindow(); // Android
    partial void OnLoaded();            // iOS/macOS
}
```

### Debugging Source Generators

Enable debugger attachment:
```xml
<PropertyGroup>
  <UnoUISourceGeneratorDebuggerBreak>True</UnoUISourceGeneratorDebuggerBreak>
</PropertyGroup>
```

Dump generator state for troubleshooting:
```xml
<PropertyGroup>
  <XamlSourceGeneratorTracingFolder>path_to_folder</XamlSourceGeneratorTracingFolder>
</PropertyGroup>
```

## Debugging Uno.UI

### Debugging in External Applications (NuGet Override)

Debug Uno.UI code in your own application:

1. Update `global.json` in your app to use a `-dev.xxx` version (e.g., `6.2.0-dev.59`)
2. Restore your app: `dotnet restore`
3. Find the `UnoVersion*` property from the Uno.Sdk NuGet README (e.g., `6.2.0-dev.171`)
4. In Uno.UI repo:
   - Copy `src/crosstargeting_override.props.sample` to `src/crosstargeting_override.props`
   - Set `<UnoNugetOverrideVersion>6.2.0-dev.171</UnoNugetOverrideVersion>` (use `UnoVersion*`, NOT `Uno.Sdk` version)
   - Build the appropriate Uno.UI project for your platform
5. In your app, open files from Uno.UI (`Ctrl+O` with full path) and set breakpoints
6. Run your app - breakpoints will hit

**Important**: This **overwrites your local NuGet cache** for that version. To revert, delete `%USERPROFILE%\.nuget\packages\Uno.UI` folder.

### Microsoft Source Link Support

Uno.UI supports SourceLink - enable in **Tools** / **Options** / **Debugging** / **General** → "Enable source link support"

## Code Style and Conventions

### Partial Classes

Extensive use of partial classes for:
- **Platform-specific code**: `MyControl.Android.cs`, `MyControl.iOS.cs`
- **Generated code**: `MyPage.xaml.g.cs`
- **Logical separation**: `MyControl.Properties.cs` for DependencyProperty definitions

### Disposables Pattern

Lightweight `IDisposable` pattern for resource management:
- `SerialDisposable` - Holds one disposable at a time, disposing previous
- `CompositeDisposable` - Disposes multiple disposables together
- `CancellationDisposable` - Links IDisposable to CancellationToken
- `DisposableAction` - Executes action on disposal

Located in: `src/Uno.Foundation/Uno.Core.Extensions/Uno.Core.Extensions.Disposables/`

### Extension Methods

Extension methods in dedicated classes: `[TypeName]Extensions.cs`
- Mark `internal` to avoid naming clashes
- Check `Uno.Foundation/Uno.Core.Extensions` for existing extensions
- Common extensions: `FindFirstChild<T>()`, `FindFirstParent<T>()`

### Braces

Always use braces, even for single-line conditionals:
```csharp
if (condition)
{
    DoSomething();
}
```

### Tabs vs Spaces

**Uno uses TABS** (configured in .editorconfig)

## Implementing New WinUI/WinRT Features

1. Find the generated API file: `src/Uno.UWP/Generated/3.0.0.0/Windows.*/ClassName.cs`
2. Copy to non-generated location: `src/Uno.UWP/Windows.*/ClassName.cs`
3. Keep only members you're implementing
4. Remove implemented platforms from `[NotImplemented]` attribute:
   ```csharp
   // Before
   #if __ANDROID__ || __IOS__ || __WASM__
   [NotImplemented("__ANDROID__", "__IOS__", "__WASM__")]

   // After (Android implemented)
   #if false || __IOS__ || __WASM__
   [NotImplemented("__IOS__", "__WASM__")]
   ```
5. Use platform suffix for platform-specific files: `ClassName.Android.cs`
6. Place shared code in non-suffixed file

## Platform-Specific Patterns

### Android
- **UIElement inherits from**: `ViewGroup` → `UnoViewGroup` (Java) → `BindableView` → `UIElement`
- **Performance-critical code**: Written in Java (`Uno.UI.BindingHelper.Android` project)
- **Layout cycle**: Triggered from native Android layout via `onMeasure`/`onLayout`
- **Mixins**: Located in `src/Uno.UI/Mixins/Android/`

### iOS/macOS
- **UIElement inherits from**: `UIView` (iOS) or `NSView` (macOS) → `BindableUIView` → `UIElement`
- **Layout cycle**: Triggered from native layout via `LayoutSubviews` (iOS) or `Layout` (macOS)
- **Mixins**: Located in `src/Uno.UI/Mixins/AppleUIKit/`
- **Shared code**: Use `.iOSmacOS.cs` or `.Apple.cs` suffix

### WebAssembly
- **UIElements**: Map 1:1 to DOM elements (default tag: "div")
- **TypeScript layer**: `WindowManager.ts` handles DOM manipulation
- **JavaScript interop**: Via `WebAssemblyRuntime.InvokeJS()`
- **Callbacks to C#**: Use `mono_bind_static_method` (must add to `LinkerDefinition.Wasm.xml`)

### Skia
- **Rendering**: Via `IRenderer` interface
- **Multiple hosts**: Win32, WPF, X11, FrameBuffer, GTK
- **Hardware acceleration**: Metal (macOS), DirectX (Windows), OpenGL (Linux)

## Hot Reload Architecture

Located in: `src/Uno.UI.RemoteControl*/`

**Hot Reload Phases:**
1. **Change Detection**: File watcher detects XAML/C# changes
2. **Delta Propagation**: Incremental updates or type replacement
3. **Metadata Update**: `MetadataUpdateHandler` invoked with changed types
4. **UI Update**: Visual tree traversed, elements replaced if type changed

**Extensibility:**
```csharp
[assembly: ElementMetadataUpdateHandler(typeof(Frame), typeof(FrameUpdateHandler))]

static class FrameUpdateHandler
{
    static void BeforeVisualTreeUpdate(Type[]? updatedTypes) { }
    static void AfterVisualTreeUpdate(Type[]? updatedTypes) { }
    static void ElementUpdate(FrameworkElement element, Type[]? types) { }
    static void BeforeElementReplaced(FrameworkElement old, FrameworkElement @new, Type[]? types) { }
    static void AfterElementReplaced(FrameworkElement old, FrameworkElement @new, Type[]? types) { }
}
```

**Pause/Resume UI Updates:**
```csharp
TypeMappings.Pause();   // Pause updates
TypeMappings.Resume();  // Resume updates
```

## Commit Message Format

All commits **must** follow [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>([optional scope]): <description>

[optional body]

[optional footer(s)]
```

**Common types:**
- `fix:` - Bug fix
- `feat:` - New functionality
- `docs:` - Documentation changes
- `test:` - Adding tests
- `perf:` - Performance improvement
- `chore:` - Catch-all for other commits

**Scope examples:** `fix(listview)`, `feat(webview)`, `fix(reg)` (regression)

**Breaking changes:** Add `BREAKING CHANGE:` in message body, optionally append `!` after type/scope

**Examples:**
```
fix(webview): Fixed video display in WebView on Android

feat(imageBrush): [iOS][macOS] Add support of WriteableBitmap

fix(resourcedictionary)!: Make ResourceDictionary.Lookup() internal

BREAKING CHANGE: This method isn't part of the public .NET contract on WinUI. Use item indexing or TryGetValue() instead.
```

## Pull Requests

- Submit all PRs to the **master** branch
- Ensure repository builds and all tests pass
- Follow current coding guidelines
- Provide tests for every bug/feature (except scenarios deemed "too hard" by team)
- Close popups/cleanup state in tests to avoid interference

## Key Build Properties

- `UnoTargetFrameworkOverride` - Set in `crosstargeting_override.props` to build for single platform
- `UnoNugetOverrideVersion` - Override local NuGet cache version for debugging
- `AccelerateBuildsInVisualStudio` - Use reference assemblies for faster builds
- `OptimizeImplicitlyTriggeredBuild` - Disable analyzers for faster iteration (remove before PR)
- `UnoUISourceGeneratorDebuggerBreak` - Attach debugger to source generator
- `XamlSourceGeneratorTracingFolder` - Dump source generator diagnostics
- `UnoDisableNetAnalyzers` - Disable .NET analyzers for faster builds

## Technology Stack

- **.NET 9.0/10.0** - Multi-target framework support
- **C# & XAML** - Primary languages
- **TypeScript** - WebAssembly and Web APIs
- **Skia Graphics** - Cross-platform rendering engine
- **MSBuild** - Build orchestration
- **Roslyn** - Source generators and analyzers
- **NerdBank.GitVersioning** - Semantic versioning
- **NUnit/MSTest** - Testing frameworks
- **DocFx** - Documentation generation

## Common Pitfalls and Gotchas

1. **DependencyObject is an interface**: On Android/iOS/macOS, don't expect to inherit from DependencyObject - implement the interface
2. **Generated files are regenerated**: Never edit files in `Generated/` folders - they're overwritten by SyncGenerator
3. **Visual tree differs by platform**: Android/iOS use native view hierarchy; WebAssembly uses DOM; Skia uses rendering tree
4. **Partial methods**: Used extensively for user extensibility points (e.g., `OnLoaded()`, `OnUnloaded()`)
5. **Platform conditionals**: Use `#if __ANDROID__`, `#if __IOS__`, `#if __WASM__`, `#if __SKIA__`
6. **NuGet cache corruption**: If debugging with override fails, delete `%USERPROFILE%\.nuget\packages\uno.ui` and rebuild
7. **Long paths**: Windows requires registry setting for long path support (see build troubleshooting)
8. **Android Resource.designer.cs**: Generation is disabled for performance - manually copy needed IDs (see building-uno-ui.md)

## Reference Documentation

- Main docs: https://platform.uno/docs/articles/intro.html
- Contributors guide: https://platform.uno/docs/articles/uno-development/contributing-intro.html
- Building Uno.UI: https://platform.uno/docs/articles/uno-development/building-uno-ui.html
- WebAssembly samples (master): https://aka.platform.uno/wasm-samples-app
- Discord: https://platform.uno/discord
