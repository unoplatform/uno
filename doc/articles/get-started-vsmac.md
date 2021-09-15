# Get Started on Visual Studio For Mac

While it is easier to create apps using Uno Platform on Windows, you can also create all but UWP/WinUI apps on your Mac.

## Prerequisites
* [**Visual Studio for Mac 8.8**](https://visualstudio.microsoft.com/vs/mac/)
* [**Xcode**](https://apps.apple.com/us/app/xcode/id497799835?mt=12) 10.0 or higher
* An [**Apple ID**](https://support.apple.com/en-us/HT204316)
* **.NET Core SDK**
    * [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) (**version 3.1.8 (SDK 3.1.402)** or later)
    * [.NET Core 5.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/5.0) (**version 5.0 (SDK 5.0.100)** or later)
* [**GTK+3**](https://formulae.brew.sh/formula/gtk+3) for running the Skia/GTK projects

## Installing the dotnet new templates
In order to create a new Uno Project, you'll need to install the [`dotnet new` Uno Platform templates](get-started-dotnet-new.md).

## Create a new project using the IDE
1. To create a new project, from the command line:
    ```
    cd src
    dotnet new unoapp -o MyApp01
    ```

1. Once created, open the `MyApp-vsmac.slnf` file
    - This `slnf` is called a solution filter, which automatically excludes projects which are not compatible with Visual Studio for mac.
    - If you have a warning symbol on your iOS project, make sure you have the minimum version of Xcode installed.
![update-xcode](Assets/quick-start/xcode-version-warning.jpg)\

To update, go to `Visual Studio > Preferences > Projects > SDK Locations > Apple` and select Xcode 12 or higher.
Restart Visual Studio.
1. You can now run on iOS, Android, macOS and Skia.GTK projects by changing your startup project and start the debugger.
   
Note: You will not be able to build the UWP and WPF projects on a Mac. All changes to this project must be made on Windows.

## Create a other projects types using the command line

You can create a new Uno Platfom solution with the following terminal command:
    ```bash
    dotnet new unoapp -o MyProject --wasm=false
    ```

Once created, you can open it using the Visual Studio IDE.

### Build and Run for WebAssembly

Building for WebAssembly takes a few more steps:

1. Set `MyProject.Wasm` to startup project
1. Build the project
1. In the terminal, navigate to your build output path. This will typically be: `MyProject.Wasm > bin > Debug > net5.0 > dist`
1. Install `dotnet serve`:
    ```
    dotnet tool install -g dotnet-serve
    ```
1. Once installed type `dotnet serve`.
1. Navigate to the url presented by the tool to run your application

### Video Tutorial
[![Getting Started Visual Studio Mac Video](Assets/vsmac-cover.JPG)](http://www.youtube.com/watch?v=ESGJr6kHQg0 "")

### Getting Help

If you have issues with Visual Studio and Uno Platform, please visit our [Discord](https://www.platform.uno/discord) - #uno-platform channel or [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform) where our engineering team and community will be able to help you. 