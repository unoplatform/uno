# Get Started on VS Code

This guide will walk you through the set-up process for building WebAssembly apps with Uno under Windows, Linux, or macOS.

See these sections for information about using Uno Platform with:
- [Codespaces](features/working-with-codespaces.md)
- [Gitpod](features/working-with-gitpod.md)

## Prerequisites

* [**Visual Studio Code**](https://code.visualstudio.com/)
* **.NET SDK**
    * [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/5.0) (**version 6.0 (SDK 6.0.100)** or later)
    > Use `dotnet --version` from the terminal to get the version installed.
* The [Uno Platform Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=unoplatform.vscode) Extension
* For Windows, install the [GTK+ 3 runtime](https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases) (See [this uno-check issue](https://github.com/unoplatform/uno.check/issues/12))

[!include[getting-help](use-uno-check-inline.md)]

## Developing an Uno Platform project

### Create the project

In the terminal, type the following to create a new project:

```bash
dotnet new unoapp -o MyApp -mobile=false --skia-wpf=false --skia-linux-fb=false --vscode
```

> `MyApp` is the name you want to give to your project.

This will create a solution that only contains the WebAssembly and Skia+GTK platforms support.

## Prepare the WebAssembly application

1. Open the project using Visual Studio Code. In the terminal type

    ```bash
    code ./MyApp
    ```

    > For this command to work you need to previously have configured Visual Studio Code to be launched from the terminal.

1. Visual Studio Code will ask to restore the NuGet packages.
1. Once the project has been loaded, in the status bar at the bottom left of VS Code, `MyApp.sln` is selected by default. Select `MyApp.Wasm.csproj` or `MyApp.Skia.Gtk.csproj` instead.

## Modify the template

1. In `MainPage.xaml`, replace the Grid's content with the following:

    ```xml
    <StackPanel>
        <TextBlock x:Name="txt"
                    Text="Hello, world!"
                    Margin="20"
                    FontSize="30" />
        <Button Content="click"
                Click="{x:Bind OnClick}" />
    </StackPanel>
    ```

2. In your `MainPage.xaml.cs`, add the following method:

    ```csharp
    private void OnClick()
    {
        var dt = DateTime.Now.ToString();
        txt.Text = dt;
    }
    ```

## Run and Debug application

### WebAssembly
1. In the debugger section of the Code activity bar, select `Debug (Chrome, WebAssembly)`
1. Press `F5` to start the debugging session
1. Place a breakpoint inside the `OnClick` method
1. Click the button in the app, and the breakpoint will hit

### Skia GTK
1. In the debugger section of the Code activity bar, select `Skia.GTK (Debug)`
1. Press `F5` to start the debugging session
1. Place a breakpoint inside the `OnClick` method
1. Click the button in the app, and the breakpoint will hit

Note that C# Hot Reload is not available when running with the debugger. In order to use C# Hot Reload, run the app using the following:
- On Windows, type the following:
    ```
    $env:DOTNET_MODIFIABLE_ASSEMBLIES="debug"
    dotnet run
    ```
- On Linux or macOS:
    ```
    export DOTNET_MODIFIABLE_ASSEMBLIES=debug
    dotnet run
    ```

## Using code snippets

### Adding a new Page
1. In the MyApp.Shared folder, create a new file named `Page2.xaml`
2. Type `page` then press the `tab` key to add the page markup
3. Adjust the name and namespaces as needed
4. In the MyApp.Shared folder, create a new file named `Page2.xaml.cs`
5. Type `page` then press the `tab` key to add the page code-behind C#
6. Adjust the name and namespaces as needed

### Adding a new UserControl
1. In the MyApp.Shared folder, create a new file named `UserControl1.xaml`
2. Type `usercontrol` then press they `tab` key to add the page markup
3. Adjust the name and namespaces as needed
4. In the MyApp.Shared folder, create a new file named `UserControl1.xaml.cs`
5. Type `usercontrol` then press the `tab` key to add the page code-behind C#
6. Adjust the name and namespaces as needed

### Adding a new ResourceDictionary
1. In the MyApp.Shared folder, create a new file named `ResourceDictionary1.xaml`
2. Type `resourcedict` then press they `tab` key to add the page markup

### Other snippets
* `rd` creates a new `RowDefinition`
* `cd` creates a new `ColumnDefinition`
* `tag` creates a new XAML tag
* `set` creates a new `Style` setter
* `ctag` creates a new `TextBlock` close XAML tag

## Updating an existing application to work with VS Code

An existing application needs additional changes to be debugged properly.

1. At the root of the workspace, create a folder named `.vscode`
2. Inside this folder, create a file named `launch.json` and copy the [contents of this file](https://github.com/unoplatform/uno/blob/master/src/SolutionTemplate/Uno.ProjectTemplates.Dotnet/content/unoapp/.vscode/launch.json).
3. Replace all instances of `UnoQuickStart` with your application's name in `launch.json`.
4. Inside this folder, create a file named `tasks.json` and copy the [contents of this file](https://github.com/unoplatform/uno/blob/master/src/SolutionTemplate/Uno.ProjectTemplates.Dotnet/content/unoapp/.vscode/tasks.json).

### Known limitations for Code support
- C# Debugging is not supported when running in a remote Linux Container, Code Spaces or GitPod.
- C# Hot Reload for WebAssembly only supports modifying method bodies. Any other modification is rejected by the compiler.
- C# Hot Reload for Skia supports modifying method bodies, adding properties, adding methods, adding classes. A more accurate list is provided here in Microsoft's documentation.

## Troubleshooting Uno Platform VS Code issues

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](external/uno.check/doc/using-uno-check.md) should be your first step.

The Uno Platform extension provides multiple output windows to troubleshoot its activities:
- **Uno Platform**, which indicates general messages about the extension
- **Uno Platform - Hot Reload**, which provides activity messages about the Hot Reload feature
- **Uno Platform - XAML**, which provides activity messages about the XAML Code Completion feature

If the extension is not behaving properly, try using the `Developer: Reload Window` (or `Ctrl+R`) command in the palette.

[!include[getting-help](getting-help.md)]
