# ALC Secondary App Support Specification

## Overview & Objectives

This specification documents the AssemblyLoadContext (ALC) feature for hosting secondary Uno Platform applications within a primary (host) application.

### Purpose

Enable hosting of complete Uno Platform applications loaded from secondary AssemblyLoadContexts within a primary host application's visual tree. This allows scenarios such as:

- **Plugin systems**: Dynamically load and display UI from plugin assemblies
- **Dynamically loaded modules**: Load application modules on demand with full XAML/resource support
- **Embedded applications**: Host complete third-party Uno applications within a host app's UI
- **Hot-swappable UI**: Replace UI modules at runtime without restarting the host application

### Key Objectives

1. **Run Full App Binaries**: Enable complete Uno Platform application binaries to run inside another host application without interfering with the outer app's state or resources
2. **Resource Isolation**: Each ALC has its own `Application.Resources` and resource dictionary registrations, preventing resource conflicts between host and secondary apps
3. **Window Lifecycle Management**: Secondary ALC windows participate in proper lifecycle events (Activated, Closed, VisibilityChanged, SizeChanged)
4. **Content Redirection**: Secondary app `Window.Content` automatically redirects to the host's `ContentHostOverride`

---

## Platform Support

| Platform | Support | Notes |
|----------|---------|-------|
| Skia Win32 (Windows) | **Supported** | Full functionality |
| Skia X11 (Linux) | **Supported** | Full functionality |
| Skia macOS | **Partial** | Under investigation (core dump issues observed) |
| WebAssembly | **Supported** | Full functionality |
| iOS | **Supported** | Full functionality |
| Android | **Supported** | Full functionality |

---

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Host Application                          │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    Main Window                           │    │
│  │  ┌─────────────────────────────────────────────────┐    │    │
│  │  │              AlcContentHost                      │    │    │
│  │  │  (WindowHelper.ContentHostOverride)              │    │    │
│  │  │  ┌───────────────────────────────────────────┐  │    │    │
│  │  │  │     Secondary ALC App Content             │  │    │    │
│  │  │  │  (Window.Content redirected here)         │  │    │    │
│  │  │  └───────────────────────────────────────────┘  │    │    │
│  │  └─────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Application.Current (Default ALC)                               │
│  └── Application.Resources                                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ Shared Uno Framework Assemblies
                              │ (Uno.UI, Uno.UI.Composition, etc.)
                              │
┌─────────────────────────────────────────────────────────────────┐
│                   Secondary AssemblyLoadContext                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              Secondary Application                       │    │
│  │  Window (ALC mode) ──► Content redirected to host       │    │
│  │  Application.Current ──► Registered per-ALC             │    │
│  │  └── Application.Resources (ALC-scoped)                 │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

### Key Files

| File | Purpose |
|------|---------|
| `src/Uno.UI/UI/Xaml/Window/Window.cs` | Main Window class with `ContentHostOverride` static property and content redirection logic |
| `src/Uno.UI/UI/Xaml/Window/Window.alc.cs` | ALC-specific window lifecycle management (~356 lines) |
| `src/Uno.UI/UI/Xaml/Window/AlcContentHost.cs` | Resource-aware ContentControl for hosting secondary ALC content (~93 lines) |
| `src/Uno.UI/UI/Xaml/Window/WindowHelper.cs` | Public API exposing `ContentHostOverride` |
| `src/Uno.UI/UI/Xaml/Application.cs` | ALC registry (`_applicationsByAlc`), `HasSecondaryApps` flag |
| `src/Uno.UI/UI/Xaml/ResourceResolver.cs` | ALC-scoped resource dictionary registrations and ambient ALC resolution context |
| `src/Uno.UI/UI/Xaml/Navigation/PageStackEntry.Uno.cs` | ALC-aware type descriptors for navigation |
| `src/SourceGenerators/Uno.UI.SourceGenerators/Content/Uno.UI.SourceGenerators.props` | `UnoEnableAlcAppSupport` MSBuild property |

---

## Public API Reference

### `WindowHelper.ContentHostOverride`

**Namespace**: `Uno.UI.Xaml`

**Type**: `static ContentControl?`

**Purpose**: Entry point for host applications to enable secondary ALC content hosting.

```csharp
namespace Uno.UI.Xaml;

public static class WindowHelper
{
    /// <summary>
    /// Gets or sets the global content host override used for secondary
    /// AssemblyLoadContext hosting scenarios.
    /// </summary>
    public static ContentControl? ContentHostOverride { get; set; }
}
```

**Usage**:
```csharp
// In host application, before loading secondary ALC
var contentHost = new AlcContentHost();
WindowHelper.ContentHostOverride = contentHost;
MainWindow.Content = contentHost;
```

---

### `AlcContentHost`

**Namespace**: `Uno.UI.Xaml.Controls`

**Base Class**: `ContentControl`

**Purpose**: A specialized ContentControl that hosts content from a secondary AssemblyLoadContext, inheriting resources from the secondary ALC's `Application.Current.Resources`.

```csharp
namespace Uno.UI.Xaml.Controls;

/// <summary>
/// A specialized ContentControl that hosts content from a secondary AssemblyLoadContext,
/// inheriting resources from the secondary ALC's Application.Current.Resources.
/// </summary>
public sealed partial class AlcContentHost : ContentControl
{
    public AlcContentHost();

    // Resources from the secondary ALC's Application are automatically
    // merged into this control's Resources property
}
```

**Behavior**:
- Stretches to fill available space by default
- Automatically merges `MergedDictionaries` from the secondary app's `Application.Resources`
- Copies `ThemeDictionaries` from the secondary app
- Copies direct resources from the secondary app

---

## Internal API Reference

### `Application.HasSecondaryApps`

**Visibility**: `internal static`

**Type**: `bool`

**Purpose**: Flag indicating whether secondary ALC applications are registered. When `true`, resource resolution uses ALC-aware lookup.

```csharp
/// <summary>
/// Indicates whether the application has secondary Application instances running
/// in separate AssemblyLoadContexts. When true, resource resolution will use
/// ALC-aware lookup to ensure resources are resolved from the correct ALC.
/// </summary>
internal static bool HasSecondaryApps { get; set; }
```

**Note**: Automatically set to `true` when secondary ALC applications register. Can be set manually by host applications before loading secondary ALCs.

---

### `Application.GetForInstance` / `GetForType` / `GetForAssemblyLoadContext`

**Visibility**: `internal static`

**Purpose**: Resolve the correct `Application` instance for a given object, type, or ALC.

```csharp
internal static Application GetForInstance(object instance);
internal static Application GetForType(Type type);
internal static Application GetForAssemblyLoadContext(AssemblyLoadContext alc);
```

**Usage**: Used internally to route resource lookups to the correct Application instance based on the ALC context.

---

### `Window.ContentHostOverride` (Internal)

**Visibility**: `internal static`

**Purpose**: The underlying static property that `WindowHelper.ContentHostOverride` delegates to.

```csharp
/// <summary>
/// Gets or sets an internal static content host override for scenarios like
/// secondary AssemblyLoadContext (ALC) hosting.
/// </summary>
/// <remarks>
/// Global effect: This property is static and affects all Window instances.
/// Usage: When set, Window.Content from secondary ALCs will redirect to this ContentControl.
/// </remarks>
internal static ContentControl? ContentHostOverride { get; set; }
```

---

### `UnoEnableAlcAppSupport` MSBuild Property

**Purpose**: Enables ALC app support in the XAML source generator, allowing proper resource dictionary registration for secondary ALC applications.

**Location**: `src/SourceGenerators/Uno.UI.SourceGenerators/Content/Uno.UI.SourceGenerators.props`

```xml
<PropertyGroup>
    <UnoEnableAlcAppSupport>true</UnoEnableAlcAppSupport>
</PropertyGroup>
```

**Note**: When `UnoEnableAlcAppSupport` is set to `false` (the default), linker substitutions remove the ALC-related code paths, reducing binary size for applications that don't need secondary ALC support.

---

## Usage Guide

### Host Application Setup

1. **Create an AlcContentHost**:
```csharp
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;

// Create the content host
var contentHost = new AlcContentHost();

// Set as the global content host override
WindowHelper.ContentHostOverride = contentHost;

// Add to your window's visual tree
MainWindow.Content = new Grid
{
    Children = { contentHost }
};
```

2. **Enable ALC-aware resource resolution** (optional, auto-enabled when secondary apps register):
```csharp
Application.HasSecondaryApps = true;
```

3. **Load and start the secondary ALC application**:
```csharp
// Create a custom AssemblyLoadContext
var alc = new PluginAssemblyLoadContext(pluginDirectory);

// Load the plugin assembly
var pluginAssembly = alc.LoadFromAssemblyPath(pluginDllPath);

// Find and invoke the entry point
var programType = pluginAssembly.GetType("PluginApp.Program");
var mainMethod = programType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);

// Start on a background thread (Application.Start blocks)
var thread = new Thread(() => mainMethod.Invoke(null, new object[] { Array.Empty<string>() }))
{
    IsBackground = true,
    Name = "PluginApp-Main"
};
thread.Start();
```

### Custom AssemblyLoadContext Implementation

Secondary ALC applications **must share** Uno framework assemblies with the host. Create a custom ALC that delegates Uno assemblies to the default context:

```csharp
public class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _basePath;

    public PluginAssemblyLoadContext(string basePath)
        : base(name: "PluginALC", isCollectible: true)
    {
        _basePath = basePath;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;

        // Let Uno/Microsoft assemblies load from the default ALC (shared)
        if (name != null && (
            name.StartsWith("Uno.", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Uno", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Microsoft.UI.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("SkiaSharp", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("HarfBuzzSharp", StringComparison.OrdinalIgnoreCase)))
        {
            return null; // Delegate to default ALC
        }

        // Try to load from the plugin's directory
        var assemblyPath = Path.Combine(_basePath, name + ".dll");
        if (File.Exists(assemblyPath))
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null; // Fall back to default resolution
    }
}
```

### Secondary App Project Configuration

The secondary app project should be configured as a class library that can be loaded dynamically:

```xml
<Project Sdk="Uno.Sdk">
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <OutputType>Library</OutputType>

        <!-- Enable ALC app support for proper resource registration -->
        <UnoEnableAlcAppSupport>true</UnoEnableAlcAppSupport>
    </PropertyGroup>
</Project>
```

### Secondary App Structure

**Program.cs**:
```csharp
namespace PluginApp;

public class Program
{
    public static void Main(string[] args)
    {
        Application.Start(_ => new App());
    }
}
```

**App.cs**:
```csharp
namespace PluginApp;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new Window();

        // Content automatically redirects to ContentHostOverride
        window.Content = new MainPage();

        window.Activate();
    }
}
```

**App.xaml**:
```xml
<Application
    x:Class="PluginApp.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                <!-- Plugin-specific resources -->
                <ResourceDictionary Source="ms-appx:///PluginResources.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

---

## Implementation Details

### Content Redirection Flow

When `Window.Content` is set from a secondary ALC, the following flow occurs:

1. **Caller Detection**: `Assembly.GetCallingAssembly()` captures the calling assembly
2. **ALC Check**: `ShouldRedirectToContentHost()` determines if the caller or content is from a secondary ALC:
   - Content's assembly is in a non-default ALC
   - Caller's assembly is in a non-default ALC
   - Window was created from a secondary ALC (`_isWindowFromSecondaryAlc`)
3. **ALC Mode Initialization**: `InitializeAlcWindowMode()` is called on first redirect:
   - Creates `AlcWindowState` instance
   - Removes window from `ApplicationHelper.Windows` (to avoid blocking app closure)
   - Closes the native window (ALC windows render through `ContentHostOverride`)
   - Subscribes to host events (SizeChanged, Loaded, Unloaded)
4. **Content Marking**: Content is marked via `ConditionalWeakTable` for later identification
5. **Host Update**: `ContentHostOverride.Content` is set to the secondary ALC's content
6. **Resource Merging**: `AlcContentHost` merges resources from the secondary app

### ALC Window State Management

The `AlcWindowState` class encapsulates all ALC-specific window state:

```csharp
private sealed class AlcWindowState
{
    public bool IsClosed;
    public bool IsVisible;

    // Subscription handlers for cleanup
    public SizeChangedEventHandler? HostSizeChangedHandler;
    public RoutedEventHandler? HostLoadedHandler;
    public RoutedEventHandler? HostUnloadedHandler;
}
```

**Key behaviors**:
- `Bounds` returns dimensions from `ContentHostOverride` instead of native window
- `Visible` returns visibility based on `ContentHostOverride.IsLoaded`
- `Activate()` raises `Activated` event with `CodeActivated` state
- `Close()` clears content from host, raises `Closed` and `VisibilityChanged` events
- Events are forwarded from `ContentHostOverride` (SizeChanged, Loaded → VisibilityChanged)

### Resource Resolution with ALC-Scoped Registries

`ResourceResolver` maintains ALC-scoped dictionaries:

```csharp
private static readonly ConditionalWeakTable<AssemblyLoadContext, Dictionary<string, Func<ResourceDictionary>>>
    _registeredDictionariesByUriByAlc = [];
```

When `Application.HasSecondaryApps` is `true`:
- Resource dictionary registrations are stored per-ALC
- Resource lookups check the ALC-scoped registry first
- Falls back to the default ALC's resources if not found

#### Ambient ALC Resolution Context

The source generator statically resolves `ResourceDictionary.Source` references at compile time (e.g., generating `GlobalStaticResources.Xyz_ResourceDictionary`). However, when static resolution **fails** (e.g., cross-assembly references, or certain codegen differences), the fallback path calls `ResourceResolver.RetrieveDictionaryForSource(string, string)`.

This fallback is **not ALC-aware** by default because it has no caller context. Similarly, `ResourceDictionary.RetrieveDictionaryForSourceWithAlcAwareness` uses `GetType().Assembly` to determine the ALC, but `ResourceDictionary` is a shared Uno.UI type always in the default ALC.

To fix this, `ResourceResolver` provides an **ambient ALC context**:

```csharp
[ThreadStatic]
private static AssemblyLoadContext? _currentResolutionAlc;

internal static AssemblyLoadContext? CurrentResolutionAlc => _currentResolutionAlc;

[EditorBrowsable(EditorBrowsableState.Never)]
public static IDisposable SetResolutionContext(AssemblyLoadContext? alc);
```

**Source generator integration**: When `UnoEnableAlcAppSupport` is enabled, the generated `App.xaml.g.cs` wraps resource building with the ambient context:

```csharp
var __currentAlc = AssemblyLoadContext.GetLoadContext(typeof(GlobalStaticResources).Assembly);
var __isDefaultAlc = __currentAlc == AssemblyLoadContext.Default;

// ... ALC-specific initialization ...

using (global::Uno.UI.ResourceResolver.SetResolutionContext(__currentAlc))
{
    // RegisterAndBuildResources + BuildProperties
    // Any ResourceDictionary.Source setter during this block will
    // find resources in the correct ALC-scoped registry.
}
```

This ensures that `ResourceDictionary.Source` references (including subdirectory paths like `ms-appx:///Styles/CustomColors.xaml`) resolve correctly in secondary ALCs.

### Navigation with ALC-Aware Type Descriptors

For `Frame.Navigate()` to work correctly with page types from secondary ALCs, `PageStackEntry` uses a special descriptor format:

```
{AssemblyQualifiedTypeName}##{ALCName}
```

**Example**: `MyPlugin.MainPage, MyPlugin, Version=1.0.0.0##PluginALC`

**Implementation** (`PageStackEntry.Uno.cs`):

```csharp
internal static string BuildDescriptor(Type pageType)
{
    var assemblyQualifiedName = pageType.AssemblyQualifiedName;
    var alc = AssemblyLoadContext.GetLoadContext(pageType.Assembly);

    if (alc is not null && alc != AssemblyLoadContext.Default)
    {
        return $"{assemblyQualifiedName}##{alc.Name}";
    }

    return assemblyQualifiedName;
}

internal static Type ResolveDescriptor(string descriptor)
{
    if (!descriptor.Contains("##"))
    {
        return Type.GetType(descriptor);
    }

    var parts = descriptor.Split("##");
    var typeDescriptor = parts[0];
    var alcName = parts[1];

    var alc = AssemblyLoadContext.All.FirstOrDefault(a => a.Name == alcName);
    using (alc.EnterContextualReflection())
    {
        return ResolveTypeInAssemblyLoadContext(typeDescriptor, alc);
    }
}
```

---

## Test Infrastructure

### Test Class

**File**: `src/Uno.UI.RuntimeTests/Tests/AssemblyLoadContext/Given_AlcContentHost.cs`

**Lines**: ~767 lines, 20+ test methods

### Test App Structure

**Directory**: `src/Uno.UI.RuntimeTests/Tests/AssemblyLoadContext/AlcApp/`

| File | Purpose |
|------|---------|
| `App.cs` | Secondary app entry point with `TestWindow` static field |
| `App.xaml` | Application resources including merged dictionaries |
| `MainPage.xaml` | Test page with named elements for verification |
| `MainPage.xaml.cs` | Code-behind |
| `AlcAppResources.xaml` | Test resources (brushes, styles, identifiers) |
| `Styles/CustomColors.xaml` | Subdirectory resource dictionary for testing ALC-aware subdirectory resolution |
| `Uno.UI.RuntimeTests.AlcApp.csproj` | Project file |

### Test Scenarios

| Test Method | Scenario |
|-------------|----------|
| `When_AlcContentHost_Then_ResourcesInherited` | Verifies resources from secondary app are accessible in `AlcContentHost.Resources` |
| `When_AlcContentHost_Then_MergedDictionariesInherited` | Verifies merged dictionaries are properly projected |
| `When_SecondaryAlcApp_Then_ContentHosted` | Basic content hosting verification |
| `When_AlcAppXaml_HasSourceDictionary_Then_ResolvesToAlcSpecificDictionary` | Verifies `ResourceDictionary.Source` resolves to ALC-specific dictionaries |
| `When_AlcAppXaml_HasSubdirectorySourceDictionary_Then_ResolvesCorrectly` | Verifies subdirectory `ResourceDictionary.Source` (e.g., `Styles/CustomColors.xaml`) resolves correctly via ambient ALC context |
| `When_AlcWindow_Activate_Then_ActivatedEventRaised` | Window.Activate() raises Activated event |
| `When_AlcWindow_Activate_WithFrameNavigation_Then_ActivatedEventRaised` | Activate works with Frame-based navigation |
| `When_AlcWindow_ActivatedRegisteredBeforeContent_Then_ActivatedEventRaised` | Event subscription before content is set |
| `When_AlcWindow_Then_VisibleReturnsHostVisibility` | Window.Visible reflects host visibility |
| `When_AlcWindow_Then_BoundsMatchesHostBounds` | Window.Bounds matches ContentHostOverride dimensions |
| `When_AlcWindow_HostSizeChanges_Then_SizeChangedEventRaised` | SizeChanged forwarded from host |
| `When_AlcWindow_Close_Then_ClosedEventRaised` | Window.Close() raises Closed event |
| `When_AlcWindow_Close_Then_ContentClearedFromHost` | Content is cleared from host on close |
| `When_AlcWindow_Close_Then_VisibilityChangedEventRaised` | VisibilityChanged raised on close |
| `When_AlcWindow_Close_Then_VisibleReturnsFalse` | Visible is false after close |
| `When_AlcWindow_ClosedEventHandled_Then_CloseIsCancelled` | Close can be cancelled via Handled flag |
| `When_AlcWindow_ActivateAfterClose_Then_ThrowsException` | Activate after close throws `InvalidOperationException` |
| `When_AlcWindow_Then_NotInApplicationHelperWindows` | ALC windows don't block app closure |
| `When_AlcContentHost_Then_FrameContentNavigates` | Frame.Navigate() works in secondary ALC |
| `When_SecondaryAlcApp_Then_AlcWindowModeEnabled` | IsAlcWindow property is true |
| `When_SecondaryAlcApp_Then_KeyboardInputStillWorks` | Keyboard input unaffected by loading secondary ALC |

---

## Limitations & Known Issues

### Current Limitations

1. **Single ContentHostOverride**: Only one `ContentHostOverride` can be active at a time. Multiple simultaneous secondary ALC apps are not supported.

2. **Shared Framework Assemblies**: Secondary ALCs **must** share Uno framework assemblies with the host. Loading different versions of `Uno.UI`, `Uno.UI.Composition`, or other Uno assemblies in the secondary ALC is not supported and will cause runtime errors.

3. **Hot Reload**: Hot Reload changes in the host application are not automatically propagated to secondary ALC content. The secondary ALC must be reloaded to pick up changes.

4. **Native Window**: Secondary ALC windows do not have their own native window. All rendering occurs through the host's `ContentHostOverride`.

### Known Issues

1. **macOS Core Dumps**: Secondary ALC support on Skia macOS is under investigation due to observed core dump issues.

2. **InputManager Sharing**: Secondary ALC content uses the host application's InputManager. This is intentional but means input handling cannot be customized per-ALC.

---

## Related Documentation

- [.NET AssemblyLoadContext Overview](https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
- [Uno Platform Window Documentation](https://platform.uno/docs/articles/features/windows.html)
