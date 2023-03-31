---
uid: Uno.Contributing.ApiExtensions
---

# API Extensions

Uno provides a simple mechanism which allows for external provides to provide an implementation for some known interfaces. The goal behind this is two fold:

- Provide a way to remove some types of dependencies from the main Uno.UI package, reducing the payload size of an app if some features are not required
- Provide a way for developers to provide a custom implementation of a known WinRT or WinUI api for which the default implementation is not provided

A common scenario can be found on Android, where adding an external dependency can increase the build time and payload size unnecessarily.

## Declaring an extension

In a nuget package, depending on the Uno.UI package, define the follow code:

```csharp
[assembly: Uno.Foundation.Extensibility.ApiExtension(typeof(Windows.ISomeExtensibleType), typeof(MySomeExtensibleType))]

public class MySomeExtensibleType : ISomeExtensibleType
{
    public MySomeExtensibleType(object owner)
    {
    
    }
}
```

When a nuget package containing such a declaration is found in the currently built application, the App.InitializeComponent() method will automatically add the following code to the app startup:

```csharp
ApiExtensibility.Register(typeof(Windows.ISomeExtensibleType), o => new MySomeExtensibleType(o));
```

This will make the `Windows.ISomeExtensibleType` available for the `ApiExtensibility.CreateInstance<Windows.ISomeExtensibleType(...)` invocation to be available.

## Available Extensible APIs

- `IApplicationViewSpanningRects` to provide a implementation for `ApplicationView.GetSpanningRects()`
