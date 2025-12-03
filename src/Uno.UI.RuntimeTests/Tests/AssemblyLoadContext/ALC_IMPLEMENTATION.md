# Secondary AssemblyLoadContext (ALC) Hosting Implementation

## Overview

This implementation enables Uno Platform applications to run inside a secondary AssemblyLoadContext (ALC) from another host application. This is useful for plugin architectures, isolated app hosting, and dynamic module loading scenarios.

## Key Components

### 1. Window.ContentHostOverride API

**Location**: `src/Uno.UI/UI/Xaml/Window/Window.cs`

A new internal property that allows redirecting Window.Content to a custom ContentControl:

```csharp
internal ContentControl? ContentHostOverride { get; set; }
```

Access this property through `Uno.UI.Xaml.WindowHelper.ContentHostOverride`.

**Behavior**:
- When `ContentHostOverride` is set, `Window.Content` setter redirects to `ContentHostOverride.Content`
- The getter also checks `ContentHostOverride` first before returning the actual window content
- This enables transparent hosting of secondary ALC app content

### 2. AlcContentHost Control

**Location**: `src/Uno.UI/UI/Xaml/Window/AlcContentHost.cs`

A specialized ContentControl that automatically inherits resources from a source Application:

```csharp
internal sealed class AlcContentHost : ContentControl
{
   internal Application? SourceApplicationOverride { get; set; }
}
```

**Features**:
- Automatically determines the correct Application based on the hosted content's AssemblyLoadContext
- Optionally allows setting `SourceApplicationOverride` (internal) for testing scenarios
- Merges ResourceDictionaries and theme dictionaries from the resolved application
- Updates resources when content changes

### 3. Test Infrastructure

**Location**: `src/Uno.UI.RuntimeTests/Tests/AssemblyLoadContext/`

Contains:
- `AlcApp/`: A minimal test application that can be loaded into a secondary ALC
  - `App.cs`: Simple application class
  - `MainPage.xaml`: Test page with styled content
  - `AppResources.xaml`: Resource dictionary for testing resource inheritance
  - `Uno.UI.RuntimeTests.AlcApp.csproj`: Project file configured for ALC loading

- `Given_AlcContentHost.cs`: Runtime tests validating:
  - Content redirection through ContentHostOverride
  - Resource inheritance from secondary ALC's Application
  - Merged dictionary support

## Usage Example

```csharp
// 1. Create a secondary AssemblyLoadContext
var alcContext = new AssemblyLoadContext("PluginContext", isCollectible: true);

// 2. Load the plugin/secondary app assembly
var pluginAssembly = alcContext.LoadFromAssemblyPath(pluginPath);
var appType = pluginAssembly.GetType("PluginNamespace.App");

// 3. Create an instance of the secondary app
var secondaryApp = Activator.CreateInstance(appType) as Application;

// 4. Create the ALC content host (it will resolve Application resources automatically)
var contentHost = new AlcContentHost();

// 5. Set the content host override on the main window
Uno.UI.Xaml.WindowHelper.ContentHostOverride = contentHost;

// 6. Launch the secondary app (this will set window.Content)
secondaryApp.OnLaunched(launchArgs);

// Now the secondary app's content is hosted in the main window
// and has access to its own resources from secondaryApp.Resources
```

## Implementation Details

### Content Redirection Flow

1. **Before**: `Window.Content = value` → Direct to window's visual tree
2. **With Override**: `Window.Content = value` → Checks `ContentHostOverride` → Sets `ContentHostOverride.Content = value`

### Resource Inheritance

When `AlcContentHost` receives content from a secondary ALC:
1. Resolves the owning `Application` via an internal lookup on that content's AssemblyLoadContext
2. Clears existing merged dictionaries to avoid duplicates
3. Merges all ResourceDictionaries from the resolved application
4. Copies theme dictionaries and direct resources

This ensures that controls within the secondary ALC app can resolve resources from their app's `Application.Resources` without any manual wiring.

### Window Implementation Updates

Modified both `CoreWindowWindow` and `DesktopWindow` implementations to:
- Check for `ContentHostOverride` in the Content getter
- Redirect to `ContentHostOverride.Content` when override is set
- Maintain backward compatibility when override is null

## Testing

### Unit Tests

Located in `src/Uno.UI.RuntimeTests/Tests/AssemblyLoadContext/Given_AlcContentHost.cs`:

1. **When_ContentHostOverride_Then_ContentRedirected**
   - Validates that Window.Content correctly redirects through ContentHostOverride

2. **When_AlcContentHost_Then_ResourcesInherited**
   - Verifies that direct resources from SourceApplication are accessible

3. **When_AlcContentHost_Then_MergedDictionariesInherited**
   - Confirms that merged dictionaries are properly inherited

4. **When_SecondaryAlcApp_Then_ContentHosted** (placeholder)
   - Full end-to-end test for loading and hosting a secondary ALC app
   - Currently marked as [Ignore] pending build infrastructure

### Test Application

The `AlcApp` test application includes:
- Custom accent brush in AppResources.xaml
- Styled TextBlocks to verify resource resolution
- Simple page structure for visual verification

## Benefits

1. **Isolation**: Secondary apps run in their own ALC with separate type identity
2. **Resource Scoping**: Each app maintains its own resource dictionaries
3. **Transparency**: From the secondary app's perspective, it's just setting Window.Content normally
4. **Unloadability**: ALCs can be unloaded, freeing memory

## Backward Compatibility

All changes are additive and internal:
- No breaking changes to public API
- `ContentHostOverride` is internal only
- `AlcContentHost` is internal only
- When `ContentHostOverride` is null, behavior is unchanged

## Future Enhancements

Potential areas for expansion:
- Public API for plugin/module hosting scenarios
- Support for multiple secondary apps in different windows
- Enhanced isolation/sandboxing features
- Performance optimizations for resource copying
