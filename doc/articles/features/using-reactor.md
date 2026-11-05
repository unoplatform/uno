---
uid: Uno.Features.Reactor
---

# Using Reactor

[Microsoft.UI.Reactor](https://github.com/microsoft/microsoft-ui-reactor) is a declarative, component-based UI framework for WinUI. You write your UI in C# as components that return a description of the UI from a `Render()` method, and keep their state in hooks such as `UseState`. Reactor turns that description into real WinUI controls and updates only what changed when the state changes.

Uno Platform runs the same Reactor code on desktop (Windows, macOS, Linux), WebAssembly, Android and iOS.

> [!NOTE]
> Reactor support is in preview and requires Uno Platform 7.0 or later, with a .NET 10 SDK. A project that uses Reactor runs on the Uno Platform targets only; it can't also target WinAppSDK (see [UNOB0031](xref:Build.Solution.error-codes#unob0031-the-reactor-uno-feature-is-not-supported-on-winappsdk)).

## Create a Reactor app

Reactor apps are created with the `unoapp-reactor` template. If you don't have the Uno Platform templates installed, follow [dotnet new templates for Uno Platform](xref:Uno.GetStarted.dotnet-new).

```dotnetcli
dotnet new unoapp-reactor -o MyApp
cd MyApp
dotnet run -f net10.0-desktop
```

The template creates a single project for Android, iOS, WebAssembly and desktop. Use `-platforms` to choose fewer targets, for example `-platforms desktop wasm`.

To run on another target, change the target framework:

```dotnetcli
dotnet run -f net10.0-browserwasm   # prints a localhost URL to open in the browser
dotnet run -f net10.0-android       # installs and starts the app on the connected device or emulator
dotnet run -f net10.0-ios           # on a Mac: installs and starts the app in the iOS Simulator
```

Running on iOS needs a Mac with the Xcode version that your .NET iOS workload expects. With another version, the build stops with `This version of .NET for iOS (…) requires Xcode …`. See [Issues related to iOS projects](xref:Uno.UI.CommonIssues.Ios).

You can also open the project in [Visual Studio Code](xref:Uno.GetStarted.vscode) or [Rider](xref:Uno.GetStarted.Rider) and run it from there.

## What's in the project

**`Program.cs`** starts the app. Reactor creates the application and the window, so this one line is the whole entry point on every target:

```csharp
ReactorApp.Run<MainComponent>("MyApp", width: 480, height: 360);
```

**`MainComponent.cs`** contains the root component:

```csharp
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace MyApp;

public class MainComponent : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);

        return VStack(12,
            Heading($"Count: {count}"),
            HStack(8,
                Button("-", () => setCount(count - 1)),
                Button("+", () => setCount(count + 1))
            )
        ).Padding(24);
    }
}
```

- `Render()` returns the UI as a tree of elements. `VStack`, `Heading` and `Button` come from `Microsoft.UI.Reactor.Factories`, and each one stands for a WinUI control or panel.
- `UseState(0)` gives the component a value that survives re-renders, and a function to change it.
- Calling `setCount` re-runs `Render()`. Reactor compares the new tree with the previous one and updates only the controls that changed.
- Methods such as `.Padding(24)` set layout and appearance properties on an element.

There is no XAML and no `App.xaml` in a Reactor project.

## Add Reactor to an existing project

Add `Reactor` to the list of [`UnoFeatures`](xref:Uno.Features.Uno.Sdk) in the project:

```xml
<UnoFeatures>
  Reactor;
</UnoFeatures>
```

This references the [Uno.Microsoft.UI.Reactor](https://www.nuget.org/packages/Uno.Microsoft.UI.Reactor) package, at the version that matches your Uno.Sdk. Then start your root component from `Program.cs` with `ReactorApp.Run<T>()`, as shown above.

## Hot Reload

When you run the app from an IDE with the Uno Platform extension, [Hot Reload](xref:Uno.Features.HotReload) applies your changes to a component's `Render()` method while the app is running, and the values held by `UseState` are kept. This works on desktop, WebAssembly, Android and iOS, and needs no Reactor-specific setup.

From the command line, run the desktop target with `dotnet watch`. A project from the template lists several target frameworks, so override the list with the one to run:

```dotnetcli
dotnet watch --property:TargetFrameworks=net10.0-desktop
```

## Next steps

- For the controls and features supported on each platform, see the [Reactor on Uno Platform README](https://github.com/unoplatform/uno.microsoft-ui-reactor/blob/uno/src/Reactor.Uno/README.md).
- To learn more about components, hooks and the element factories, see the [Microsoft.UI.Reactor repository](https://github.com/microsoft/microsoft-ui-reactor).
