# Get Started on VS Code

This guide will walk you through the set-up process for building WebAssembly apps with Uno under Windows, Linux, or macOS.

## Prerequisites

* [**Visual Studio Code**](https://code.visualstudio.com/)
* **.NET SDK**
    * [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/5.0) (**version 6.0 (SDK 6.0.100)** or later)
    > Use `dotnet --version` from the terminal to get the version installed.
* The [Uno Platform Visual Studio Code](https://marketplace.visualstudio.com/search?term=uno%20platform) Extension

You can use [`uno-check`](https://github.com/unoplatform/uno.check) to make your installation compatible with Uno Platform.

## Create an Uno Platform project

### Install Uno Platform Template

Launch Visual Studio Code and open a new terminal.

In the terminal, type the following to install the [Uno Platform templates](get-started-dotnet-new.md):

```bash
dotnet new -i Uno.ProjectTemplates.Dotnet
```

### Create the project

In the terminal, type the following to create a new project:

```bash
dotnet new unoapp -o MyApp -ios=false -android=false -macos=false -uwp=false --vscodeWasm
```

> `MyApp` is the name you want to give to your project.

This will create a solution that only contains the WebAssembly platform support.

## Prepare the WebAssembly application for debugging

1. Open the project using Visual Studio Code. In the terminal type

    ```bash
    code ./MyApp
    ```

    > For this command to work you need to previously have configured Visual Studio Code to be launched from the terminal.

3. Visual Studio Code will ask to restore the NuGet packages.

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

## Run and Debug the application

1. Starting the app with the WebAssembly debugger is a two-step process. Move to the **Run** tab on Visual Studio Code and

    * Start the app first using the **.NET Core Launch (Uno Platform App)** launch configuration
    * Then start the browser using the **.NET Core Debug Uno Platform WebAssembly in Chrome** launch configuration (requires Chrome).

        > To use the latest stable release of Edge instead of Chrome, change the type of the launch configuration in `.vscode/launch.json` from `pwa-chrome` to `pwa-msedge`

2. Place a breakpoint inside the `OnClick` method
3. Click the button in the app, and the breakpoint will hit

## Updating an existing application to work with VS Code

**TBD**

## Troubleshooting Uno Platform Installation Issues

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](uno-check.md) should be your first step.

## Getting Help

If you continue experiencing issues with Visual Studio and Uno Platform, please visit our [Discord](https://www.platform.uno/discord) - #uno-platform channel or [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform) where our engineering team and community will be able to help you. 