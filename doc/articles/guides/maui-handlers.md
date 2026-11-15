---
uid: Uno.Guides.MauiHandlers
---

# Running a .NET MAUI app on the web and desktop

[Uno.Maui.Handlers](https://www.nuget.org/packages/Uno.Maui.Handlers) runs an existing .NET MAUI app with Uno Platform, in the browser (WebAssembly) and on Skia desktop (Windows, macOS and Linux). MAUI keeps rendering your pages with its own Windows (WinUI) handlers, which Uno Platform runs on these targets, so your XAML, C#, images, fonts and the third-party MAUI libraries you use stay as they are.

> [!IMPORTANT]
> Uno.Maui.Handlers is an experimental preview. Its versions are `0.1.0-dev.*`, and its behavior can change in any version. It supports .NET MAUI 10.0.101.

To show MAUI controls inside an Uno Platform app instead, see [.NET MAUI Embedding](xref:Uno.Extensions.Maui.Overview).

## Choose how to add Uno Platform

There are two ways to set up the app:

* **[A project next to your app](#option-1-add-an-uno-platform-project-next-to-your-app).** Your MAUI app is not modified and keeps building for Android, iOS and Windows with .NET MAUI. A small Uno Platform project next to it compiles the app's files for the web and desktop.
* **[Your app's own project on Uno.Sdk](#option-2-switch-your-apps-project-to-unosdk).** The MAUI project becomes an Uno Platform single project. Every target it lists, including Android and iOS, then runs on Uno Platform, and the project no longer builds with .NET MAUI's own platforms.

## Prerequisites

* A .NET MAUI app on .NET 10 that uses `Microsoft.Maui.Controls` 10.0.101.
* The .NET 10 SDK and the prerequisites of the targets you build. [uno-check](xref:UnoCheck.UsingUnoCheck) installs them.
* Third-party MAUI libraries need a `net10.0-windows` build. Uno.Maui.Handlers uses the library's Windows build on every Uno Platform target.

Both options use `Uno.Sdk`. In the folder of your solution, add it to the `msbuild-sdks` section of `global.json`, or create the file:

```json
{
  "msbuild-sdks": {
    "Uno.Sdk": "7.0.5"
  }
}
```

## Option 1: Add an Uno Platform project next to your app

### Step 1 - Add the project

Next to your MAUI app's folder (not inside it), create a folder `MyApp.Uno` with a project file `MyApp.Uno.csproj`. In this example, the MAUI app is `MyApp/MyApp.csproj`:

```xml
<Project Sdk="Uno.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0-desktop;net10.0-browserwasm</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UnoSingleProject>true</UnoSingleProject>
    <RootNamespace>MyApp</RootNamespace>
    <ApplicationTitle>MyApp</ApplicationTitle>
    <ApplicationId>com.companyname.myapp</ApplicationId>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <MauiVersion>10.0.101</MauiVersion>
  </PropertyGroup>

  <!-- The MAUI app's files, from its own folder -->
  <ItemGroup>
    <Compile Include="../MyApp/**/*.cs" Exclude="../MyApp/bin/**;../MyApp/obj/**;../MyApp/Platforms/**" />
    <MauiXaml Include="../MyApp/**/*.xaml" Exclude="../MyApp/bin/**;../MyApp/obj/**;../MyApp/Platforms/**" Link="%(RecursiveDir)%(Filename)%(Extension)" />
    <MauiIcon Include="../MyApp/Resources/AppIcon/appicon.svg" ForegroundFile="../MyApp/Resources/AppIcon/appiconfg.svg" Color="#512BD4" />
    <MauiSplashScreen Include="../MyApp/Resources/Splash/splash.svg" Color="#512BD4" BaseSize="128,128" />
    <MauiImage Include="../MyApp/Resources/Images/*" />
    <MauiFont Include="../MyApp/Resources/Fonts/*" />
    <MauiAsset Include="../MyApp/Resources/Raw/**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Uno.Maui.Handlers" Version="0.1.0-dev.4" />
    <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <!-- The other packages of your MAUI app, for example: -->
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="10.0.0" />
  </ItemGroup>
</Project>
```

* Copy the `MauiIcon`, `MauiSplashScreen`, `MauiImage`, `MauiFont` and `MauiAsset` items from your MAUI project file, with the `../MyApp/` prefix in front of each path.
* Add the `PackageReference` items of your MAUI app, such as `CommunityToolkit.Maui`.
* Uno.Maui.Handlers adds the start-up code for each target, which calls your `MauiProgram.CreateMauiApp()`. You don't write any Uno Platform code.

### Step 2 - Add the browser files

Add the [browser files](#browser-files) to the `MyApp.Uno` project.

### Step 3 - Run the app

From the `MyApp.Uno` folder:

```bash
dotnet run -f net10.0-desktop
dotnet run -f net10.0-browserwasm
```

The browser target prints the app's address, such as `http://127.0.0.1:5000/`. Open it in a Chromium-based browser.

## Option 2: Switch your app's project to Uno.Sdk

### Step 1 - Change the project file

In your MAUI app's project file:

1. Replace `Sdk="Microsoft.NET.Sdk"` with `Sdk="Uno.Sdk"`.
1. Replace the `TargetFrameworks` elements with the Uno Platform targets you need.
1. Replace `<UseMaui>true</UseMaui>` and `<SingleProject>true</SingleProject>` with `<UnoSingleProject>true</UnoSingleProject>` and `<MauiVersion>10.0.101</MauiVersion>`.
1. Remove the `SupportedOSPlatformVersion` and `TargetPlatformMinVersion` elements. Uno.Sdk sets the minimum versions of each platform.
1. Add the `Uno.Maui.Handlers` package, and the Android item shown below.

The `Maui*` items and your other packages stay as they are. For the MAUI template app, the result is:

```xml
<Project Sdk="Uno.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0-desktop;net10.0-browserwasm;net10.0-android;net10.0-ios</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>MyApp</RootNamespace>
    <UnoSingleProject>true</UnoSingleProject>
    <MauiVersion>10.0.101</MauiVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ApplicationTitle>MyApp</ApplicationTitle>
    <ApplicationId>com.companyname.myapp</ApplicationId>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
  </PropertyGroup>

  <ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4" />
    <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#512BD4" BaseSize="128,128" />
    <MauiImage Include="Resources\Images\*" />
    <MauiImage Update="Resources\Images\dotnet_bot.png" Resize="True" BaseSize="300,185" />
    <MauiFont Include="Resources\Fonts\*" />
    <MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Uno.Maui.Handlers" Version="0.1.0-dev.4" />
    <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
    <!-- MAUI's Android dependency would duplicate classes that Uno Platform brings -->
    <PackageReference Include="Xamarin.AndroidX.Fragment.Ktx" Version="1.9.0" ExcludeAssets="all" />
  </ItemGroup>
</Project>
```

Uno.Maui.Handlers adds the start-up code for each target, which calls your `MauiProgram.CreateMauiApp()`. You don't write any Uno Platform code.

### Step 2 - Replace the Platforms folder

1. Delete the app's `Platforms` folder. Uno.Maui.Handlers supplies the platform start-up code.
1. Add the [browser files](#browser-files).
1. For iOS, add `Platforms/iOS/Info.plist`, which names Uno Platform's scene delegate:

    ```xml
    <?xml version="1.0" encoding="UTF-8"?>
    <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
    <plist version="1.0">
    <dict>
      <key>LSRequiresIPhoneOS</key>
      <true/>
      <key>UILaunchScreen</key>
      <dict/>
      <key>UIDeviceFamily</key>
      <array>
        <integer>1</integer>
        <integer>2</integer>
      </array>
      <key>UISupportedInterfaceOrientations</key>
      <array>
        <string>UIInterfaceOrientationPortrait</string>
        <string>UIInterfaceOrientationLandscapeLeft</string>
        <string>UIInterfaceOrientationLandscapeRight</string>
      </array>
      <key>ITSAppUsesNonExemptEncryption</key>
      <false/>
      <key>UIApplicationSceneManifest</key>
      <dict>
        <key>UIApplicationSupportsMultipleScenes</key>
        <true/>
        <key>UISceneConfigurations</key>
        <dict>
          <key>UIWindowSceneSessionRoleApplication</key>
          <array>
            <dict>
              <key>UISceneConfigurationName</key>
              <string>__UNO_DEFAULT_SCENE_CONFIGURATION__</string>
              <key>UISceneDelegateClassName</key>
              <string>SceneDelegate</string>
            </dict>
          </array>
        </dict>
      </dict>
    </dict>
    </plist>
    ```

### Step 3 - Run the app

From the app's folder:

```bash
dotnet run -f net10.0-desktop
dotnet run -f net10.0-browserwasm
dotnet build -f net10.0-android
dotnet build -f net10.0-ios
```

To run on Android or iOS, deploy the app from your IDE, as for any Uno Platform app.

## Browser files

The WebAssembly target needs two files in the project.

`Platforms/WebAssembly/manifest.webmanifest`:

```json
{
  "background_color": "#ffffff",
  "description": "MyApp",
  "display": "standalone",
  "name": "MyApp",
  "short_name": "MyApp",
  "start_url": "/index.html",
  "theme_color": "#ffffff",
  "scope": "/"
}
```

`Platforms/WebAssembly/WasmScripts/AppManifest.js`:

```js
var UnoAppManifest = {
    displayName: "MyApp"
}
```

## Things to check in your app

* **Platform code.** The code of MAUI's `Platforms` folder doesn't run. On the web and desktop, Uno.Maui.Handlers defines `WINDOWS`, so code under `#if WINDOWS` runs there. Code in it that uses types from `Platforms/Windows`, or Windows App SDK window APIs such as `AppWindow.MoveAndResize`, fails on these targets. On Android and iOS (option 2), MAUI also runs through its Windows handlers, and `ANDROID` and `IOS` aren't defined, so code under `#if ANDROID` or `#if IOS` isn't compiled.
* **Libraries without a Windows build.** A MAUI library that ships only Android and iOS builds can't be used.
* **`BlazorWebView`** is not supported.
* **Blocking calls.** Code that waits on asynchronous work with `.Result` or `.Wait()` blocks the browser's only thread.
