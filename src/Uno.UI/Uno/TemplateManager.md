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
2. **Subscribe Controls**: Controls use `TemplateUpdateSubscription.Attach()` to listen for template changes
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
// Update a template's factory function
if (Resources["MyTemplate"] is DataTemplate template)
{
    Uno.TemplateManager.UpdateDataTemplate(template, () =>
    {
        // Return new UI element
        return new TextBlock { Text = "Updated Content" };
    });
}
```

### Subscribe to Template Updates (Custom Controls)

If you're developing a custom control that uses `DataTemplate.LoadContent()` and want it to react to template changes:

```csharp
public class MyCustomControl : FrameworkElement
{
    private IDisposable _templateSubscription;
    private DataTemplate _currentTemplate;

    public DataTemplate ItemTemplate
    {
        get => _currentTemplate;
        set
        {
            if (_currentTemplate != value)
            {
                _currentTemplate = value;
                
                // Subscribe to template updates using the public API
                Uno.TemplateManager.SubscribeToTemplate(
                    _currentTemplate, 
                    ref _templateSubscription, 
                    OnTemplateUpdated
                );
                
                RefreshContent();
            }
        }
    }

    private void OnTemplateUpdated()
    {
        // Template was updated - refresh the materialized content
        RefreshContent();
    }

    private void RefreshContent()
    {
        if (_currentTemplate != null)
        {
            // Clear existing content
            Children.Clear();
            
            // Materialize new content
            var content = _currentTemplate.LoadContent() as UIElement;
            if (content != null)
            {
                Children.Add(content);
            }
        }
    }

    protected override void OnUnloaded()
    {
        // Clean up subscription to avoid memory leaks
        Uno.TemplateManager.UnsubscribeFromTemplate(ref _templateSubscription);
        base.OnUnloaded();
    }
}
```

### Understanding the `ref IDisposable` Pattern

The subscription methods use `ref IDisposable?` to automatically manage subscription lifecycle:

```csharp
private IDisposable _subscription;

// When subscribing to a new template:
// - If _subscription already exists, it will be automatically disposed
// - A new subscription will be created and stored in _subscription
// - The method returns true if dynamic updates are enabled
bool subscribed = TemplateManager.SubscribeToTemplate(
    myTemplate, 
    ref _subscription,  // Note: ref keyword is required
    () => RefreshUI()
);

// When changing templates:
TemplateManager.SubscribeToTemplate(
    newTemplate, 
    ref _subscription,  // Automatically disposes previous subscription
    () => RefreshUI()
);

// When cleaning up:
TemplateManager.UnsubscribeFromTemplate(ref _subscription);
// After this call, _subscription will be null
```

## Built-in Support

The following Uno Platform controls already support dynamic template updates when the feature is enabled:

- `ItemsRepeater`
- `ListView` / `GridView`
- `ItemsControl`
- `ContentPresenter`
- `ContentControl`

## Important Notes

### Development Tool Only

This feature is intended **exclusively for development and debugging scenarios**. It should not be enabled in production applications as it:

- Has memory overhead for template subscriptions
- Can cause unexpected UI refreshes
- Is not optimized for performance

### Memory Management

Always dispose of template subscriptions in your custom controls to avoid memory leaks:

```csharp
// In OnUnloaded or Dispose
Uno.TemplateManager.UnsubscribeFromTemplate(ref _templateSubscription);
```

### Thread Safety

Template updates should be performed on the UI thread. The system does not provide automatic thread marshaling.

## API Reference

### TemplateManager

- `EnableUpdateSubscriptions()`: Enables the dynamic template update system
- `IsUpdateSubscriptionsEnabled`: Gets whether the system is enabled
- `UpdateDataTemplate(DataTemplate, Func<View>)`: Updates a template with a new factory function
- `SubscribeToTemplate(DataTemplate, ref IDisposable, Action)`: Subscribe to template update notifications
- `UnsubscribeFromTemplate(ref IDisposable)`: Unsubscribe from template updates

### TemplateUpdateSubscription (Internal)

- `Attach(DataTemplate, ref IDisposable, Action)`: Internal subscription method (use `TemplateManager.SubscribeToTemplate` instead)
- Returns `true` if subscriptions are enabled and the subscription was created

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
                    Uno.TemplateManager.UpdateDataTemplate(template, () =>
                    {
                        return LoadUpdatedTemplate(file);
                    });
                }
            }
        };
    }
}
```

This enables scenarios like XAML hot-reload where template changes can be reflected immediately in the running application without restart.
