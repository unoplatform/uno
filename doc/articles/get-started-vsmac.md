# Getting Started on Visual Studio For Mac

While it is easier to create apps using Uno on Windows, you can also create all but UWP apps on your Mac.

## Prerequisites
* [**Visual Studio for Mac**](https://visualstudio.microsoft.com/vs/mac/)
* [**Xcode**](https://apps.apple.com/us/app/xcode/id497799835?mt=12) 10.0 or higher
* An [**Apple ID**](https://support.apple.com/en-us/HT204316)
* .NET Core 3.1

## Modifying Existing Uno App

1. Open project in Visual Studio for Mac
 ![new-project](Assets/quick-start/vs-mac-open-project.png)
Once open, you should see your folder structure set up like this:
![folder-structure](Assets/quick-start/vs-mac-folder-structure.png)\
If you have a warning symbol on your iOS project, make sure you have the minimum version of XCode.
![update-xcode](Assets/quick-start/xcode-version-warning.jpg)\
To update, go to `Visual Studio > Preferences > Projects > SDK Locations > Apple` and select XCode 10.0 or higher.
Restart Visual Studio.

2. You can now run on iOS, Android, and WebAssembly by setting your startup project and running.
![startup-projects](Assets/quick-start/vs-mac-build.png)
   
Note: You will not be able to build the UWP project on a Mac. All changes to this project must be made on Windows.

### Build for WASM

Building for WebAssembly takes a few more steps than iOS and Android:

1. Set yourProject.Wasm to startup project
2. Build the project
3. In the terminal, navigate to your build output. This will typically be: `yourProject.Wasm > bin > Debug > netstandard2.0 > dist > server.py` Run the `server.py` program.
4. In your browser, open localhost:8000. 

### Video Tutorial
[![Getting Started Visual Studio Mac Video](Assets/vsmac-cover.JPG)](http://www.youtube.com/watch?v=ESGJr6kHQg0 "")
