# Getting Started on JetBrains Rider

## Prerequisites
* [**Rider Version 2020.2 Early Access**](https://www.jetbrains.com/rider/nextversion/)
* [**Rider Xamarin Android Support Plugin**](https://plugins.jetbrains.com/plugin/12056-rider-xamarin-android-support/)

## Creating a new Uno project
At this time, there isn't a template for the Rider IDE like there is for Visual Studio, so you can create a new project
[using dotnet new](get-started-dotnet-new.md) by following these steps:

1. In your terminal, navigate to the folder that contains your Rider solutions.

2. Run these commands:

Installs Uno template:  
```bash
dotnet new -i Uno.ProjectTemplates.Dotnet
```
Creates a new project:  
```bash
dotnet new unoapp -o MyApp
```

You should now have a folder structure that looks like this:  
![rider-folder-structure](Assets/quick-start/rider-folder-structure.JPG)

### Android
1. Remove the following line from the `YourProject.Droid.csproj` file:  
`<Target Name="GenerateBuild" DependsOnTargets="SignAndroidPackage" AfterTargets="Build" Condition="'$(BuildingInsideVisualStudio)'==''" />`
2. Set Android as your startup project. Run.  
![run-android-rider](Assets/quick-start/run-android-rider.JPG)

Note: Whether you're using a physical device or the emulator, the app will install but will not automatically open.
You will have to manually open.

### Wasm
1. Select Wasm as your startup project. Run.  
![run-wasm-rider](Assets/quick-start/run-wasm-rider.JPG)  
A new browser window will automatically run your application.  

Note: There is no debugging for Wasm within Rider, but you debug using the built in Chrome tools. 

### MacOS
You will be able to build the MacOS project.  
![run-ios-rider](Assets/quick-start/run-ios-rider.JPG)  
Alternatively, you can use a tool like VNC to run the simulator on a mac.  

### UWP
You will be able to build the UWP project, however, Rider currently does not support debugging or deploying for UWP.   
![run-uwp-rider](Assets/quick-start/run-uwp-rider.JPG)  


### Video Tutorial
[![Getting Started Rider Video](Assets/rider-cover.JPG)](http://www.youtube.com/watch?v=HgwL0al5bfo "")
