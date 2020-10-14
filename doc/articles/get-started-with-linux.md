# Getting started with Uno Platform support for Linux

The Uno Platform for Linux current comes with a rendering backend using Skia, and a Shell support with Gtk3.

It is possible to develop :
- Using Visual Studio on Windows directly, or using the Windows Subsystem for Linux.
- Using VS Code under Linux

## Setting for Windows and WSL

Using VS 2019 16.6 or later:
- Install the [VS WSL Extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.Dot-Net-Core-Debugging-With-Wsl2)
- Install [WSL Ubuntu 18.04 or later](https://docs.microsoft.com/en-us/windows/wsl/install-win10)
- Install the prerequisites for Linux mentioned in the next section, in your installed distribution using the Ubuntu shell
- Install [vcXsrv](https://sourceforge.net/projects/vcxsrv/), an X11 server for Windows
    - You'll need to start the server in "Multiple windows" mode, starting with "no client" mode.
- Install the [GTK3 runtime](https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases).
- Using the WSL 1 mode is generally best for performance and ease of use for the X11 server
    - You can switch between versions of a distribution using `wsl --set-version "Ubuntu-20.04" 1`
    - You can list your active distributions with `wslconfig /l` and change the default with `wslconfig /s "Ubuntu-20.04"`
    - You can change the used distribution with the `"distributionName": "Ubuntu-20.04"` launch profile parameter of the VS WSL 2 Extension.
    - If you have a insider preview of Windows 10, you may [use the wayland server](https://devblogs.microsoft.com/commandline/the-windows-subsystem-for-linux-build-2020-summary/#wsl-gui).
    - If you still want to use WSL 2 anyways, you can try [following those steps](https://skeptric.com/wsl2-xserver).
- Install the [`dotnet new` templates](get-started-dotnet-new.md):
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
- In the launch profile file, set:
```json
"environmentVariables": {
    "DISPLAY": ":0",
},
```
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
    
    
    
## Setting up for ArchLinux 5.8.14 or later / Manjaro:
- update system and packages
    ```bash
    pacman -Syu
    ```
- install the necessary dependencies
    ```bash
    sudo pacman -S gtk3 dotnet-targeting-pack dotnet-sdk dotnet-host dotnet-runtime mono python mono-msbuild ninja gn aspnet-runtime 
    ```
- Install the `dotnet new` templates:
    ```bash
    dotnet new -i Uno.ProjectTemplates.Dotnet::3.1-dev*
    ```
- Then create a new project using:
    ```bash
    dotnet new unoapp -o MyUnoApp
    ```

Now let's run the application:
- Open the folder created by `dotnet new`
- In the terminal, build and run the application:
    ```bash
    cd MyUnoApp.Skia.Gtk
    dotnet run
    ```
Run the WebAssembly head with:
    ```bash
    cd .. 
    cd MyUnoApp.Wasm
    dotnet run
    ```
    
turns on the browser and type
```
http://localhost:5000/
```  
