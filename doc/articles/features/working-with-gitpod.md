---
uid: Uno.Features.Gitpod
---

# Using Ona

## Developing an Uno App using Ona

The easiest way to get started is to visit the [Uno.QuickStart](https://github.com/unoplatform/Uno.QuickStart) repository. It allows you to get started with minimal configuration or project creation steps.

### Creating the environment from scratch

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

1. Commit these changes and open a Ona workspace
1. Install the `unoplatform.vscode` extension from [OpenVSX](https://open-vsx.org/extension/unoplatform/vscode)
1. Open the command palette (Ctrl+Shift+P) and run the `Install the dotnet new templates` command to install the dotnet new templates
1. Create a new project using the following command:

    ```dotnetcli
    dotnet new unoapp -o MyApp -ios=false -android=false -macos=false -skia-tizen=false -skia-wpf=false -skia-linux-fb=false --vscode
    ```

1. Using the Ona top left menu, open the `MyApp` folder

You're ready to develop for WebAssembly or the Linux desktop (Skia Desktop).

## Contributing to Uno using Ona

To contribute to Uno using Ona:

1. [![Open Uno in Ona](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/unoplatform/uno)
1. In the opened shell, type the following to build the Uno solution:

    ```bash
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

Once the server is started, Ona will automatically open a browser window on the side to show the sample application.

You can make your changes in XAML directly, to view the changes through Hot Reload. If you make changes in the code, you'll need to rerun the `build-wasm.sh` script, then refresh the browser section on the side.

Once you're done with your changes, make a Pull Request through the [Gitpod's GitHub integration](https://www.gitpod.io/docs/58_pull_requests/) and let us know about it on our [Discord Server](https://platform.uno/discord)!
