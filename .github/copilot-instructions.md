# Uno Platform Development Instructions

The Uno Platform is a cross-platform .NET UI framework that allows you to build native mobile, web, desktop, and embedded applications from a single C# codebase using WinUI/XAML. It targets Windows, macOS, Linux, iOS, Android, and WebAssembly.

**Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Architecture and Code Organization

### Technology Stack

- **.NET 10.0/9.0** - Multi-target framework support
- **C# & XAML** - Primary languages
- **TypeScript** - Only for WebAssembly and Web APIs
- **Skia Graphics** - Cross-platform rendering engine option
- **MSBuild** - Build orchestration
- **Roslyn** - Source generators and analyzers

### Platform-Specific Code Patterns

Uno uses file suffixes to organize platform-specific implementations:
- `.Android.cs` - Android-specific
- `.iOS.cs` - iOS-specific
- `.UIKit.cs` - iOS & tvOS specific
- `.Apple.cs` - Apple platforms (iOS & tvOS) specific (legacy sufiffix, prefer `.UIKit.cs`)
- `.wasm.cs` / `.Interop.wasm.cs` - WebAssembly
- `.skia.cs` - Skia
- `.reference.cs` - Reference implementation
- `.unittests.cs` - Test variant
- `.crossruntime.cs` - Shared runtime code

### Core Architecture Patterns

1. **Multi-Target Platform Abstraction**: Single C#/XAML codebase → WinUI 3 API → Platform-specific runtimes (Skia, WebAssembly, Native)

2. **Rendering Engines**:
   - **Skia**: Cross-platform rendering (Desktop Win32, macOS/AppKit, Linux/X11/FrameBuffer, Skia Android, Skia UIKit for iOS and tvOS)
   - **Native**: Platform controls (UIKit on iOS, Android Framework on Android, DOM elements on WebAssembly)

3. **Platform-Specific Base Classes**:
   - **Android native**: `UIElement` inherits from `Android.Views.ViewGroup` → `Uno.UI.UnoViewGroup` (Java) → `Uno.UI.Controls.BindableView` → `UIElement`
   - **iOS native**: `UIElement` inherits from `UIKit.UIView` → `Uno.UI.Controls.BindableUIView` → `UIElement`
   - **WebAssembly native**: UIElements map directly to DOM elements (default tag: "div")
   - **All Skia targets**: UIElements use `IRenderer` interface for rendering pipeline

4. **XAML Compilation**: XAML files are parsed to C# code via source generators (not .xbf like WinUI)
   - `XamlFileGenerator` in `Uno.UI.SourceGenerators`
   - Generates `InitializeComponent()` and named field backing
   - Supports x:Bind with compile-time type safety
   - XAML syntax matches WinUI XAML

5. **DependencyObject Implementation**:
   - On Android/iOS, `DependencyObject` is an **interface** (not base class) since `UIElement` has to inherit from native view classes
   - Source generators provide the implementation via `DependencyObjectGenerator`
   - Generated code provides `__Store` (DependencyObjectStore) for property storage

6. **Project Organization**: Most libraries have 5 variants:
   - Reference (API surface, netstandard2.0)
   - Skia (Desktop rendering)
   - WebAssembly (Browser)
   - NetCoreMobile (iOS/Android)
   - Tests (Unit tests)

7. **Runtime target selection**
   - For Native targets, projects are compiled against the "platform specific" .NET target - e.g. for Android it is `netX-android`
   - For Skia, libraries `src\SourceGenerators\Uno.UI.Tasks\RuntimeAssetsSelector\RuntimeAssetsSelectorTask.cs` ensures that for Uno.UI and all libraries referencing it, the `netX` (generic) target is used, as all Skia targets need the same source. Because of this all `#if __SKIA__` is evaluated as true, but other conditionals like `#if __ANDROID__` are evaluated as false. However, this is **not** the case for `Uno.UWP` and `Uno.Foundation` - for these, platform specific assembly is selected instead, even when targeting Skia rendering. E.g. for Skia Android, `Uno.UWP` with `netX-android` would be used, while `Uno.UI` would compile against `netX` only. For this reason, when platform-specific behavior is needed on Skia targets, runtime checks are needed, e.g. `OperatingSystem.IsAndroid()`.

### Key Source Directories

- `src/Uno.UI/` - Core UI framework (141+ WinUI controls)
- `src/Uno.UWP/` - Primarily non-UI APIs
- `src/Uno.Foundation/` - Foundation APIs
- `src/Uno.UI.Runtime.Skia.*/` - Platform-specific Skia runtimes
- `src/Uno.UI.Runtime.WebAssembly/` - WebAssembly runtime (native).
- `src/SourceGenerators/` - C# source generators (XAML parser, DependencyProperty generator)
- `src/Uno.UI.BindingHelper.Android/` - Java interop classes for performance (UnoViewGroup)
- `src/Uno.UI/Mixins/` - Platform-specific extension methods and mixins
- `src/SamplesApp/` - Sample application for feature validations, UI tests, and runtime tests execution
- `src/Uno.UI.RuntimeTests/` - Platform runtime tests
- `src/AddIns/` - Extensions (Lottie, MSAL, Maps, SVG, MediaPlayer)
- `build/` - Build infrastructure and NuGet configuration
- `doc/` - DocFx documentation

### NotImplemented Stubs

Uno auto-generates stubs for WinUI APIs not yet implemented, marked with `[Uno.NotImplemented]` attribute. These allow compilation but warn if referenced in app code. Located in `Generated` folders, generated by SyncGenerator tool. No logic should be placed in these files, as they will be overwritten next time the generator runs.

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
- **Platform-specific API implementations**: When platform-specific APIs need to be implemented `ApiExtensibility` can be used. `Uno.UWP` or `Uno.UI` provides an interface that the `Runtime.Skia` projects then implement and register.

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

### 1. **Build validation**:
```bash
cd src && dotnet build Uno.UI-UnitTests-only.slnf --no-restore
```

### 2. **Unit test validation**:
```bash
cd src && dotnet test Uno.UI/Uno.UI.Tests.csproj --no-build
```

### 3. **Sample app validation** (for UI changes):
```bash
cd src/SamplesApp/SamplesApp.Wasm && dotnet run
```
### 4. **Documentation validation** (for doc changes):
```bash
cd doc && npm install && npm run build
```

### 5. **Runtime test validation** (preferred for UI features):

Runtime tests are generally preferred over unit tests for testing UI features. Add new tests to the `Uno.UI.RuntimeTests` project and run them at runtime on the target platform. Ensure all new tests pass before committing by running them via the console app (see below).

**Key helpers for test implementation:**
- `WindowHelper.WindowContent` - Add/remove elements from visual tree
- `await WindowHelper.WaitForLoaded(element)` - Wait for element to load/measure/arrange
- `await WindowHelper.WaitForIdle()` - Wait for UI thread to settle
- `await WindowHelper.WaitFor(() => condition)` - Wait for specific condition
- Always close popups in `try/finally` to avoid interfering with other tests

**Adding new runtime tests**:
```bash
# Navigate to the runtime tests project
cd src/Uno.UI.RuntimeTests

# Add your test class following existing patterns
# Tests should be marked with [TestMethod] attribute
# Use the Given_When_Then naming convention
```

**Running runtime tests from command line (Skia Desktop)**:

Runtime tests can be executed headlessly without the interactive UI. This is how CI runs tests and is useful for validating new tests locally.

```bash
# Build SamplesApp.Skia.Generic
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0

# Run all runtime tests
cd src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

**Running specific tests with a filter**:

The `UITEST_RUNTIME_TESTS_FILTER` environment variable accepts a base64-encoded, pipe-separated list of fully qualified test names.

Windows PowerShell:
```powershell
$filter = "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Control.When_SomeScenario"
$env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($filter))
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

Linux/macOS:
```bash
export UITEST_RUNTIME_TESTS_FILTER=$(echo -n "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Control.When_SomeScenario" | base64)
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

Test results are output in NUnit XML format at the path specified by `--runtime-tests`.

**Agent workflow**: When adding new runtime tests for desktop (Skia), always build and run them using the commands above to verify they pass before committing. Skip this only for tests targeting non-desktop platforms (iOS/Android-specific features).

**Running runtime tests via SamplesApp UI**:
1. Build and run the SamplesApp for your target platform (e.g., `SamplesApp.Skia.Generic` for WebAssembly)
2. Click the test runner button in the top left corner of the application
3. In the test interface, enter the name of your test
4. Click "Run" to execute the test
5. View results in the log output showing passed/failed tests with detailed information

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
**"Windows XAML targets not found"**: Expected on Linux/macOS, use WebAssembly or Skia targets instead.
**"Package restore timeout"**: Network issue, retry with longer timeout
**"Solution filter fails"**: Ensure `crosstargeting_override.props` target matches the solution filter
**Persistent build issues**: Close VS 2022, delete `src/.vs` folder, then rebuild
**Last resort cleanup**: Run `git clean -fdx` (after closing VS) to remove all untracked files
**Windows long paths error**: Enable long paths: `reg ADD HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem /v LongPathsEnabled /t REG_DWORD /d 1`

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

## Key Build Properties

- `UnoTargetFrameworkOverride` - Set in `crosstargeting_override.props` to build for single platform
- `UnoNugetOverrideVersion` - Override local NuGet cache version for debugging
- `AccelerateBuildsInVisualStudio` - Use reference assemblies for faster builds
- `OptimizeImplicitlyTriggeredBuild` - Disable analyzers for faster iteration (remove before PR)
- `UnoUISourceGeneratorDebuggerBreak` - Attach debugger to source generator
- `XamlSourceGeneratorTracingFolder` - Dump source generator diagnostics
- `UnoDisableNetAnalyzers` - Disable .NET analyzers for faster builds

## Common Pitfalls and Gotchas

1. **DependencyObject is an interface**: On Android/iOS, don't expect to inherit from DependencyObject - implement the interface
2. **Generated files are regenerated**: Never edit files in `Generated/` folders - they're overwritten by SyncGenerator
3. **Visual tree differs by platform**: Android/iOS use native view hierarchy; WebAssembly uses DOM; Skia uses rendering tree
4. **Partial methods**: Used extensively for user extensibility points (e.g., `OnLoaded()`, `OnUnloaded()`)
5. **NuGet cache corruption**: If debugging with override fails, delete `%USERPROFILE%\.nuget\packages\uno.ui` and rebuild
6. **Long paths**: Windows requires registry setting for long path support
7. **Android Resource.designer.cs**: Generation is disabled for performance - manually copy needed IDs (see building-uno-ui.md)

## Reference Documentation

- Main docs: https://platform.uno/docs/articles/intro.html
- Contributors guide: https://platform.uno/docs/articles/uno-development/contributing-intro.html
- Building Uno.UI: https://platform.uno/docs/articles/uno-development/building-uno-ui.html
- WebAssembly samples (master): https://aka.platform.uno/wasm-samples-app
- Discord: https://platform.uno/discord

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