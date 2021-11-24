# Get Started on VS Code

This guide will walk you through the set-up process for building WebAssembly apps with Uno under Windows, Linux, or macOS.

## Prerequisites

* [**Visual Studio Code**](https://code.visualstudio.com/)
* **.NET SDK**
    * [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/5.0) (**version 6.0 (SDK 6.0.100)** or later)
    > Use `dotnet --version` from the terminal to get the version installed.
* The [Uno Platform Visual Studio Code](https://marketplace.visualstudio.com/search?term=uno%20platform) Extension

You can use [`uno-check`](https://github.com/unoplatform/uno.check) to make your installation compatible with Uno Platform.

## Developing an Uno Platform project

### Create the project

In the terminal, type the following to create a new project:

```bash
dotnet new unoapp -o MyApp -ios=false -android=false -macos=false -skia-tizen=false -skia-wpf=false -skia-linux-fb=false --vscodeWasm
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
1. Once the project has been loaded, in the status bar, `MyApp.sln` is selected by default. Select `MyApp.Wasm.csproj` or `MyApp.Skia.Gtk.csproj` instead.

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

## Updating an existing application to work with VS Code

**Coming soon**

### Known limitations for Code support
- C# Debugging is not supported when running in a remote Linux Container, Code Spaces or GitPod.
- C# Hot Reload for WebAssembly only supports modifying method bodies. Any other modification is rejected by the compiler.
- C# Hot Reload for Skia supports modifying method bodies, adding properties, adding methods, adding classes. A more accurate list is provided here in Microsoft's documentation.
- Adding new pages, controls or resources is not yet supported from the UI or command line.

## Troubleshooting Uno Platform VS Code issues

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](uno-check.md) should be your first step.

The Uno Platform extension provides multiple output windows to troubleshoot its activities:
- **Uno Platform**, which indicates general messages about the extension
- **Uno Platform - Hot Reload**, which provides activity messages about the Hot Reload feature
- **Uno Platform - XAML**, which provides activity messages about the XAML Code Completion feature

## Getting Help

If you continue experiencing issues with Visual Studio and Uno Platform, please visit our [Discord](https://www.platform.uno/discord) - #uno-platform channel or [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform) where our engineering team and community will be able to help you. 