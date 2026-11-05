---
uid: Uno.Features.Reactor
---

<!-- markdownlint-disable MD001 MD025 -->

# Using Reactor

[Microsoft.UI.Reactor](https://github.com/unoplatform/uno.microsoft-ui-reactor) is a declarative, component-based UI framework for WinUI. You write your UI in C# as components that return a description of the UI from a `Render()` method, and keep their state in hooks such as `UseState`. Reactor turns that description into real WinUI controls and updates only what changed when the state changes.

Uno Platform runs the same Reactor code on desktop (Windows, macOS, Linux), WebAssembly, Android and iOS.

> [!NOTE]
> Reactor support is in preview and requires Uno Platform 7.0 or later, with a .NET 10 SDK. A project that uses Reactor runs on the Uno Platform targets only; it can't also target WinAppSDK (see [UNOB0031](xref:Build.Solution.error-codes#unob0031-the-reactor-uno-feature-is-not-supported-on-winappsdk)).

## Create an app

You can write a Reactor app as a single `.cs` file, or start a full project from the `unoapp-reactor` template.

# [**Single file**](#tab/single-file)

A Reactor app can be a single `.cs` file, with no project file.

Save this code as `Counter.cs`:

```csharp
// Use a Uno.Sdk version 7.0 or later
#:sdk Uno.Sdk@x.y.z
#:property TargetFramework=net10.0-desktop
#:property OutputType=Exe
#:property UnoSingleProject=true
#:property UnoFeatures=Reactor
#:property PublishAot=false
#:property PackAsTool=false

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

ReactorApp.Run<CounterApp>("Counter", width: 480, height: 360);

class CounterApp : Component
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

> [!TIP]
> To keep the version out of the file, write `#:sdk Uno.Sdk` without a version, and name the Uno.Sdk version in a `global.json` next to the file:
>
> ```json
> {
>   "msbuild-sdks": {
>     "Uno.Sdk": "x.y.z"
>   }
> }
> ```

`UnoFeatures=Reactor` adds the [Uno.Microsoft.UI.Reactor](https://www.nuget.org/packages/Uno.Microsoft.UI.Reactor) package, at the version that matches your Uno.Sdk.

Then run it:

```dotnetcli
dotnet run Counter.cs
```

A single file targets one platform at a time. To run it on WebAssembly or Android, change the `TargetFramework` line to `net10.0-browserwasm` or `net10.0-android`. A single file can't run on iOS yet: for iOS, create a project from the template instead.

# [**From the template**](#tab/template)

The `unoapp-reactor` template comes with the Uno Platform 7.0 templates. Until 7.0 is released, a plain `dotnet new install Uno.Templates` installs the stable templates, which don't include it, so name a 7.0 version (this one or a later one):

```dotnetcli
dotnet new install Uno.Templates@7.0.0-dev.161
```

For more about the templates, see [dotnet new templates for Uno Platform](xref:Uno.GetStarted.dotnet-new).

Then create and run the app:

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

### What's in the project

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

---

## Hot Reload

When you run the app from an IDE with the Uno Platform extension, [Hot Reload](xref:Uno.Features.HotReload) applies your changes to a component's `Render()` method while the app is running, and the values held by `UseState` are kept. This works on desktop, WebAssembly, Android and iOS, and needs no Reactor-specific setup.

From the command line, run the desktop target with `dotnet watch`. A project from the template lists several target frameworks, so override the list with the one to run:

```dotnetcli
dotnet watch --property:TargetFrameworks=net10.0-desktop
```

## Next steps

- Build a complete app step by step with the [Counter tutorial](xref:Uno.Workshop.Counter.Reactor).
- For the controls and features supported on each platform, see the [Reactor on Uno Platform README](https://github.com/unoplatform/uno.microsoft-ui-reactor/blob/uno/src/Reactor.Uno/README.md).
- To learn more about components, hooks and the element factories, see Microsoft's [Reactor guide](https://microsoft.github.io/microsoft-ui-reactor/). Its code runs on Uno Platform unchanged, but skip its setup steps (`winapp`, MSIX packaging), which are for Windows: create your app as shown above instead.
