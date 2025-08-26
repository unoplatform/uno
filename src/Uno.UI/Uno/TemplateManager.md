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
Uno.TemplateManager.EnableUpdateSubscriptions();
```

### Update a DataTemplate

```csharp
// Update a template's factory function (simple view factory)
if (Resources["MyTemplate"] is DataTemplate template)
{
    Uno.TemplateManager.UpdateDataTemplate(template, () =>
    {
        // Return new UI element - must be compatible with View type
        return new TextBlock { Text = "Updated Content" };
    });
}

// Alternative: Update using factory updater (advanced)
if (Resources["MyTemplate"] is DataTemplate template)
{
    Uno.TemplateManager.UpdateDataTemplate(template, oldFactory =>
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
    private IDisposable _templateSubscription;

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
            // Subscribe to template updates using the public API
            // This automatically disposes previous subscription
            Uno.TemplateManager.SubscribeToTemplate(
                (DataTemplate) e.NewValue,
                ref c._templateSubscription,
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

- `ItemsRepeater`
- `ItemsControl`
- `ListView` and `GridView` (through inheritance from `ItemsControl`)
- `ContentPresenter`
- `ContentControl`

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
    private IDisposable _templateSubscription;

    // ✅ CORRECT: Subscribe in DependencyProperty callback
    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MyControl c)
        {
            Uno.TemplateManager.SubscribeToTemplate(
                (DataTemplate)e.NewValue, 
                ref c._templateSubscription, 
                () => c.RefreshContent());
        }
    }

    // ✅ CORRECT: Clean up only on final disposal (optional)
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Uno.TemplateManager.UnsubscribeFromTemplate(ref _templateSubscription);
        }
        base.Dispose(disposing);
    }

    // ❌ INCORRECT: Don't unsubscribe in OnUnloaded
    // protected override void OnUnloaded()
    // {
    //     Uno.TemplateManager.UnsubscribeFromTemplate(ref _templateSubscription); // DON'T DO THIS
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
// ✅ CORRECT: Subscribe in DependencyProperty callback
private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    if (d is MyControl c)
    {
        Uno.TemplateManager.SubscribeToTemplate(
            (DataTemplate)e.NewValue, 
            ref c._templateSubscription, 
            () => c.RefreshContent());
    }
}

// ❌ INCORRECT: Don't subscribe in Loaded event
// protected override void OnLoaded()
// {
//     TemplateManager.SubscribeToTemplate(...); // Wrong place
// }
```

## API Reference

### TemplateManager

- `EnableUpdateSubscriptions()`: Enables the dynamic template update system
- `IsUpdateSubscriptionsEnabled`: Gets whether the system is enabled
- `UpdateDataTemplate(DataTemplate, Func<NewFrameworkTemplateBuilder?, NewFrameworkTemplateBuilder?>)`: Updates a template with a factory updater function
- `UpdateDataTemplate(DataTemplate, Func<View?>)`: Updates a template with a simple view factory function
- `SubscribeToTemplate(DataTemplate?, ref IDisposable?, Action)`: Subscribe to template update notifications
- `UnsubscribeFromTemplate(ref IDisposable?)`: Unsubscribe from template updates

### TemplateUpdateSubscription (Internal)

- `Attach(DataTemplate, ref IDisposable, Action)`: Internal subscription method
- **Use `TemplateManager.SubscribeToTemplate` instead** - this is the public API
- Returns `true` if subscriptions are enabled and the subscription was created

**Note**: Built-in Uno Platform controls currently use the internal `TemplateUpdateSubscription.Attach()` method directly for performance reasons. User code should use the public `TemplateManager.SubscribeToTemplate()` API.

## Example: Hot-Reload Scenario

```csharp
// Development tool that updates templates based on file changes
public class TemplateHotReloader
{
    public void Initialize()
    {
        Uno.TemplateManager.EnableUpdateSubscriptions();
        
        // Watch for XAML file changes (pseudo-code)
        FileWatcher.OnFileChanged += (file) =>
        {
            if (file.EndsWith(".xaml"))
            {
                var template = FindTemplateForFile(file);
                if (template != null)
                {
                    // Use the simple view factory overload
                    Uno.TemplateManager.UpdateDataTemplate(template, () =>
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
