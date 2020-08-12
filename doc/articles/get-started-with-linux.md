# Getting started with Uno Platform support for Linux

The Uno Platform for Linux current comes with a rendering backend using Skia, and a Shell support with Gtk3.

It is possible to develop :
- Using Visual Studio on Windows directly, or using the Windows Subsystem for Linux.
- Using VS Code under Linux

## Setting for Windows and WSL

Using VS 2019 16.6 or later:
- Install the [VS WSL Extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.Dot-Net-Core-Debugging-With-Wsl2)
- Install [WSL Ubuntu 18.04 or later](https://docs.microsoft.com/en-us/windows/wsl/install-win10)
- Install the [vcXsrv](https://sourceforge.net/projects/vcxsrv/), an X11 server for Windows
- Install the [GTK3 runtime](https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases).
- In your user environment variables:
    - Add a variable named `Display`, with the value `:0`
    - Add a variable named `WSLENV`, with the value `DISPLAY/u` (See [this page](https://devblogs.microsoft.com/commandline/share-environment-vars-between-wsl-and-windows/) for nore details)
- Install the `dotnet new` templates:
    ```
    dotnet new -i Uno.ProjectTemplates.Dotnet::3.1-dev*
    ```
- Then create a new project using:
    ```
    dotnet new unoapp -o MyUnoApp
    ```

Now let's run the application:
- Open the solution using Visual Studio
- In the debugger menu, next to the green arrow, select **WSL 2**
- Start the debugger session
- Visual Studio may ask you to install **.NET Core 3.1**, press OK and let the installation finish, then restart the debugging session.

## Setting up for Linux

Using Ubuntu 18.04 or later:
- Install GTK3:
    ```
    sudo apt update
    sudo apt install gtk+3.0
    ```
- Install dotnet core 3.1
    ```
    echo "Installing .NET Core"
    wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb

    sudo dpkg -i packages-microsoft-prod.deb
    sudo add-apt-repository universe
    sudo apt-get -y install apt-transport-https
    sudo apt-get update
    sudo apt-get -y install dotnet-sdk-3.1
    ```
- Install the `dotnet new` templates:
    ```
    dotnet new -i Uno.ProjectTemplates.Dotnet::3.1-dev*
    ```
- Then create a new project using:
    ```
    dotnet new unoapp -o MyUnoApp
    ```

Now let's run the application:
- Open the folder created by `dotnet new`
- In the terminal, build and run the application:
    ```
    cd MyUnoApp.Skia.Gtk
    dotnet run
    ```