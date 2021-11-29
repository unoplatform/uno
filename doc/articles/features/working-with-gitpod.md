# Using Gitpod

## Developing an Uno App using Gitpod

The easiest way to get started is to visible the Uno.QuickStart repository. It allows you to get started with minimal configuration or project creation steps.

If you want to start from an empty repository, follow these steps:
1. Create an empty repository
1. Create a file named `.gitpod.yml` at the root of the repository, and set it to the following
    ```yml
    image:
      file: .gitpod.Dockerfile

    tasks:
      # Mitigation for https://github.com/gitpod-io/gitpod/issues/6460 
      - name: Postinstall .NET 6.0 and dev certificates
        init: |
          mkdir -p $DOTNET_ROOT && curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel $DOTNET_VERSION --install-dir $DOTNET_ROOT 
          dotnet dev-certs https 
    ```
1. Create a file named `.gitpod.Dockerfile`, with the following content:
    ```dockerfile
    FROM gitpod/workspace-dotnet-vnc

    USER gitpod
    #.NET installed via .gitpod.yml task until the following issue is fixed: https://github.com/gitpod-io/gitpod/issues/5090
    ENV DOTNET_VERSION=6.0
    ENV DOTNET_ROOT=/workspace/.dotnet
    ENV PATH=$DOTNET_ROOT:$PATH
    ```
1. Commit these changes and open a GitPod workspace
1. Install the `unoplatform.vscode` extension from [OpenVSX](https://open-vsx.org/extension/unoplatform/vscode)
1. Open the command palette (Ctrl+Shift+P) and run the `Install the dotnet new templates` command to install the dotnet new templates
1. Create a new project using the following command:
    ```
    dotnet new unoapp -o MyApp -ios=false -android=false -macos=false -skia-tizen=false -skia-wpf=false -skia-linux-fb=false --vscodeWasm
    ```
1. Using the Gitpod top left menu, open the `MyApp` folder

You're ready to develop for WebAssembly or the Linux desktop (Skia+Gtk).

### Developing for WebAssembly
1. Once the C# environment is setup, with the commmand palette use the command "Omnisharp: Select project" (or click on the project name in the status bar)
1. Select the `MyApp.Wasm` project
1. Using a terminal, navigate to the `MyApp.Wasm` folder
1. Type `dotnet run`
1. Once the compilation is done, a server will open on port 5000
1. Gitpod will suggest to open a new browser window or as a preview window

You can now use C# Hot Reload and XAML HotReload to develop your application.

See [the VS Code Getting started](../get-started-vscode.md) documentation for additional details about developing with VS Code.

### Developing for Skia.Gtk
1. As Gitpod will suggest, open the port 6080 as a browser preview to get access to the X11 environment.
1. Once the C# environment is setup, with the commmand palette use the command "Omnisharp: Select project" (or click on the project name in the status bar)
1. Select the `MyApp.Skia.Gtk` project
1. To run your application, either:
    - Using a terminal, navigate to the `MyApp.Skia.Gtk` folder and type `dotnet run`
    - In the debug activity section on the left, select `Skia.GTK (Debug)` in drop down, the press `F5` or Ctrl+F5 to start the application without the debugger.
1. Once the compilation is done, a server will open on port 5000
1. Gitpod will suggest to open a new browser window or as a preview window

You can now use C# Hot Reload and XAML HotReload to develop your application.

See [the VS Code Getting started](../get-started-vscode.md) documentation for additional details about developing with VS Code.

## Contributing to Uno using Gitpod

To contribute to Uno using GitPod:
1. [![Open Uno in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/unoplatform/uno)
1. In the opened shell, type the following to build the Uno solution:
    ```
    build/gitpod/build-wasm.sh
    ```
    The build should end without any errors
1. If you want to enable XAML Hot Reload, open another shell, then run:
    ```sh
    build/gitpod/serve-remote-control.sh
    ```
1. Open another shell, then start the Uno http server:
    ```sh
    build/gitpod/serve-sampleapp-wasm.sh
    ```

Once the server is started, Gitpod will automatically open a browser window on the side to show the sample application.

You can make your changes in XAML directly, to view the changes through Hot Reload. If you make changes in the code, you'll need to rerun the `build-wasm.sh` script, then refresh the browser section on the side.

Once you're done with your changes, make a Pull Request through the [Gitpod's GitHub integration](https://www.gitpod.io/docs/58_pull_requests/) and let us know about it on our [Discord Channel #uno-platform](https://discord.gg/eBHZSKG)!
