# TemplateManager - Dynamic DataTemplate Updates

## Overview

`TemplateManager` is a development tooling feature that enables runtime updates of `DataTemplate` instances and automatic refresh of controls that use them. This is primarily designed for debugging and development scenarios where you need to modify templates dynamically without restarting the application.

## Key Features

- **Runtime Template Updates**: Modify the factory function of existing `DataTemplate` instances
- **Automatic Control Refresh**: Controls that subscribe to template updates automatically refresh their content
- **Opt-in System**: Disabled by default to avoid any performance impact in production
- **Cross-platform Support**: Works on all Uno Platform targets

## How It Works

1. **Enable the System**: Call `TemplateManager.EnableUpdateSubscriptions()` at application startup
2. **Subscribe Controls**: Controls use `TemplateManager.SubscribeToTemplate()` to listen for template changes
3. **Update Templates**: Use `TemplateManager.UpdateDataTemplate()` to modify template factories
4. **Automatic Refresh**: Subscribed controls automatically refresh their materialized content

## Usage

### Enable Dynamic Updates (Application Startup)

```csharp
// Enable the dynamic template update system
// WARNING: This is a debugging tool - do not use in production
Uno.UI.TemplateManager.EnableUpdateSubscriptions();
```

### Production Release Configuration (Not Recommended)

⚠️ **WARNING**: The dynamic template update system is designed as a development/debugging tool. However, if you absolutely need to enable it in a production release build, you must configure both the MSBuild property and runtime activation:

#### Step 1: Enable MSBuild Feature Flag

Add this to your application's `.csproj` file:

```xml
<PropertyGroup>
  <!-- DANGER: Only enable this if you absolutely need dynamic templates in production -->
  <!-- This ensures the code survives compilation and linking -->
  <UnoEnableDynamicDataTemplateUpdate>true</UnoEnableDynamicDataTemplateUpdate>
</PropertyGroup>
```

#### Step 2: Runtime Activation

Even with the MSBuild flag enabled, you still need to explicitly activate the system at application startup:

```csharp
public partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // DANGER: Only call this if you absolutely need dynamic templates in production
        // This has performance implications and should only be used for debugging
        Uno.UI.TemplateManager.EnableUpdateSubscriptions();
        
        // Rest of your application initialization...
    }
}
```

#### Why Both Steps Are Required

1. **MSBuild Flag (`UnoEnableDynamicDataTemplateUpdate`)**: 
   - Defaults to `false` to exclude the feature code from release builds
   - When `false`, the linker removes all dynamic template update code
   - Must be `true` to include the necessary infrastructure

2. **Runtime Activation (`EnableUpdateSubscriptions()`)**: 
   - Even when the code is included, the system remains disabled by default
   - Must be explicitly enabled to avoid any performance overhead
   - Allows you to conditionally enable the feature based on runtime conditions

#### Production Considerations

- **Performance Impact**: The subscription system adds overhead to template operations
- **Memory Usage**: Template subscriptions consume additional memory
- **Security**: Dynamic code execution capabilities may not be suitable for all environments
- **Debugging Only**: This feature is primarily intended for development scenarios

**Recommended Alternative**: Instead of enabling this in production, consider using conditional compilation symbols to enable it only in debug builds:

```csharp
#if DEBUG
Uno.UI.TemplateManager.EnableUpdateSubscriptions();
#endif
```

### Update a DataTemplate

```csharp
// Update a template's factory function (simple view factory)
if (Resources["MyTemplate"] is DataTemplate template)
{
    Uno.UI.TemplateManager.UpdateDataTemplate(template, () =>
    {
        // Return new UI element - must be compatible with View type
        return new TextBlock { Text = "Updated Content" };
    });
}

// Alternative: Update using factory updater (advanced)
if (Resources["MyTemplate"] is DataTemplate template)
{
    Uno.UI.TemplateManager.UpdateDataTemplate(template, oldFactory =>
    {
        // Return a new factory that creates different content
        return (NewFrameworkTemplateBuilder)((_, _) => new TextBlock { Text = "Advanced Update" });
    });
}
```

### Subscribe to Template Updates (Custom Controls)

If you're developing a custom control that uses `DataTemplate.LoadContent()` and want it to react to template changes:

```csharp
public class MyCustomControl : FrameworkElement
{
    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(MyCustomControl),
            new PropertyMetadata(null, OnItemTemplateChanged));

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MyCustomControl c)
        {
            // Subscribe to template updates using the owner-based API
            // No need to keep a member IDisposable anymore
            Uno.UI.TemplateManager.SubscribeToTemplate(
                c,
                (DataTemplate) e.NewValue,
                () => c.RefreshContent());
        
            c.RefreshContent();
        }
    }

    private void RefreshContent()
    {
        var currentTemplate = ItemTemplate;
        if (currentTemplate != null)
        {
            // Clear existing content
            Children.Clear();
            
            // Materialize new content
            var content = currentTemplate.LoadContent() as UIElement;
            if (content != null)
            {
                Children.Add(content);
            }
        }
    }
}
```

## Built-in Support

The following Uno Platform controls already support dynamic template updates when the feature is enabled:

- `ItemsRepeater` - Full support with comprehensive reentrancy protection
- `ItemsControl` - Basic support using internal `TemplateUpdateSubscription.Attach`
- `ListView` and `GridView` (through inheritance from `ItemsControl`)

**Note**: `ContentPresenter` and `ContentControl` have the infrastructure but may not have dynamic template updates fully implemented yet.

## Important Notes

### Development Tooling Only

This feature is intended **exclusively for development and debugging scenarios**. It should not be enabled in production applications as it:

- Has memory overhead for template subscriptions
- Can cause unexpected UI refreshes
- Is not optimized for performance

### Memory Management

**Important**: Template subscriptions should NOT be disposed in `OnUnloaded` as this would prevent them from working when the control is loaded again. Instead:

```csharp
public class MyControl : FrameworkElement
{
    // ✅ CORRECT: Subscribe in DependencyProperty callback (no member field required)
    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MyControl c)
        {
            Uno.UI.TemplateManager.SubscribeToTemplate(
                c,
                (DataTemplate)e.NewValue,
                () => c.RefreshContent());
        }
    }

    // ✅ OPTIONAL: Clean up all subscriptions associated with this control on final disposal
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Uno.UI.TemplateManager.UnsubscribeFromTemplate(this);
        }
        base.Dispose(disposing);
    }

    // ❌ INCORRECT: Don't unsubscribe in OnUnloaded
    // protected override void OnUnloaded()
    // {
    //     Uno.UI.TemplateManager.UnsubscribeFromTemplate(this); // DON'T DO THIS
    // }
}
```

### Thread Safety

Everything is happening in the UI Thread, so no need to shield against concurrent access.

### Limitations

- **Performance**: Each subscription has memory overhead - use sparingly
- **Template Identity**: Subscriptions are based on DataTemplate reference equality

### Best Practices

1. **Enable Once**: Call `EnableUpdateSubscriptions()` only once at app startup
2. **Minimal Subscriptions**: Only subscribe for templates that actually need dynamic updates
3. **Proper Disposal**: Let the automatic disposal handle cleanup, avoid manual unsubscribe in OnUnloaded
4. **UI Thread**: Always call `UpdateDataTemplate()` from the UI thread
5. **Testing**: Verify behavior works correctly across Unload/Load cycles

### DependencyProperty Integration

For custom controls, the subscription should be managed in the DependencyProperty change callback:

```csharp
// ✅ CORRECT: Subscribe in DependencyProperty callback (no member field required)
private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    if (d is MyControl c)
    {
        Uno.UI.TemplateManager.SubscribeToTemplate(
            c,
            (DataTemplate)e.NewValue,
            () => c.RefreshContent());
    }
}

// ❌ INCORRECT: Don't subscribe in Loaded event
// protected override void OnLoaded()
// {
//     TemplateManager.SubscribeToTemplate(...); // Wrong place
// }
```

### API Reference

### TemplateManager

- `EnableUpdateSubscriptions()`: Enables the dynamic template update system
- `IsDataTemplateDynamicUpdateEnabled`: Gets whether the feature is enabled via MSBuild configuration
- `IsUpdateSubscriptionsEnabled`: Gets whether the system is enabled (requires `EnableUpdateSubscriptions()` call)
- `UpdateDataTemplate(DataTemplate, Func<NewFrameworkTemplateBuilder?, NewFrameworkTemplateBuilder?>)`: Updates a template with a factory updater function
- `UpdateDataTemplate(DataTemplate, Func<View?>)`: Updates a template with a simple view factory function
- `SubscribeToTemplate(DependencyObject owner, DataTemplate? template, Action onUpdated)`: Preferred owner-based subscription; returns `bool` indicating success
- `SubscribeToTemplate(DependencyObject owner, string slotKey, DataTemplate? template, Action onUpdated)`: Owner-based subscription with a named slot for multiple subscriptions per control; returns `bool` indicating success
- `UnsubscribeFromTemplate(DependencyObject owner)`: Unsubscribes all owner-associated subscriptions
- `UnsubscribeFromTemplate(DependencyObject owner, string slotKey)`: Unsubscribes the specified slot subscription for the owner
- `UnsubscribeFromTemplate(DependencyObject owner, string slotKey)`: Unsubscribes the specified slot subscription for the owner

### TemplateUpdateSubscription (Internal)

- `Attach(DependencyObject owner, DataTemplate? template, Action onUpdated)`: Owner-based internal subscription
- `Attach(DependencyObject owner, string slotKey, DataTemplate? template, Action onUpdated)`: Owner-based internal subscription with slot
- `Detach(DependencyObject owner)`: Detach all subscriptions for owner
- `Detach(DependencyObject owner, string slotKey)`: Detach slot subscription for owner
- Returns `true` if subscriptions are enabled and the subscription was created

**Note**: Built-in Uno Platform controls use internal methods directly for performance reasons. The actual implementation in `ItemsControl` uses `TemplateUpdateSubscription.Attach` directly rather than the public `TemplateManager.SubscribeToTemplate()` API. User code should still use the public API.

## Example: Hot-Reload Scenario

```csharp
// Development tool that updates templates based on file changes
public class TemplateHotReloader
{
    public void Initialize()
    {
        Uno.UI.TemplateManager.EnableUpdateSubscriptions();
        
        // Watch for XAML file changes (pseudo-code)
        FileWatcher.OnFileChanged += (file) =>
        {
            if (file.EndsWith(".xaml"))
            {
                var template = FindTemplateForFile(file);
                if (template != null)
                {
                    // Use the simple view factory overload
                    Uno.UI.TemplateManager.UpdateDataTemplate(template, () =>
                    {
                        var updatedElement = LoadUpdatedTemplate(file);
                        return updatedElement; // Must return View? type
                    });
                }
            }
        };
    }
    
    private View? LoadUpdatedTemplate(string filePath)
    {
        // Implementation to load and parse XAML file
        // Returns the root UI element
        return null; // Implementation specific
    }
    
    private DataTemplate? FindTemplateForFile(string filePath)
    {
        // Implementation to find the DataTemplate associated with the file
        return null; // Implementation specific
    }
}
```

This enables scenarios like XAML hot-reload where template changes can be reflected immediately in the running application without restart.
