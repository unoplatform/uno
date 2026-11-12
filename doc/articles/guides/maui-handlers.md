---
uid: Uno.Guides.MauiHandlers
---

# Running a .NET MAUI app on the web and desktop

[Uno.Maui.Handlers](https://www.nuget.org/packages/Uno.Maui.Handlers) runs an existing .NET MAUI app in the browser (WebAssembly) and on Skia desktop (Windows, macOS and Linux) with Uno Platform. MAUI keeps rendering your pages with its own Windows (WinUI) handlers, which Uno Platform runs on these targets, so your XAML, C#, images, fonts and the third-party MAUI libraries you use stay as they are.

Your MAUI app is not modified: it keeps building for Android, iOS and Windows. You add one small Uno Platform project next to it, which compiles the app's files for the web and desktop.

> [!IMPORTANT]
> Uno.Maui.Handlers is an experimental preview. Its versions are `0.1.0-dev.*`, and its behavior can change in any version. It supports .NET MAUI 10.0.101.

To show MAUI controls inside an Uno Platform app instead, see [.NET MAUI Embedding](xref:Uno.Extensions.Maui.Overview).

## Prerequisites

* A .NET MAUI app on .NET 10 that uses `Microsoft.Maui.Controls` 10.0.101.
* The .NET 10 SDK and the WebAssembly prerequisites. [uno-check](xref:UnoCheck.UsingUnoCheck) installs them.
* Third-party MAUI libraries need a `net10.0-windows` build. Uno.Maui.Handlers uses the library's Windows build on the web and desktop.

## Step 1 - Add the Uno.Sdk to global.json

In the folder of your solution, add `Uno.Sdk` to the `msbuild-sdks` section of `global.json`, or create the file:

```json
{
  "msbuild-sdks": {
    "Uno.Sdk": "7.0.5"
  }
}
```

## Step 2 - Add the Uno Platform project

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

## Step 3 - Add the browser files

The WebAssembly target needs two files in the Uno Platform project.

`MyApp.Uno/Platforms/WebAssembly/manifest.webmanifest`:

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

`MyApp.Uno/Platforms/WebAssembly/WasmScripts/AppManifest.js`:

```js
var UnoAppManifest = {
    displayName: "MyApp"
}
```

## Step 4 - Run the app

From the `MyApp.Uno` folder:

```bash
dotnet run -f net10.0-desktop
dotnet run -f net10.0-browserwasm
```

The browser target prints the app's address, such as `http://127.0.0.1:5000/`. Open it in a Chromium-based browser.

## Things to check in your app

* **Platform code.** The files in your app's `Platforms` folder are not compiled. Uno.Maui.Handlers defines `WINDOWS`, so code under `#if WINDOWS` runs on the web and desktop. Code there that uses types from `Platforms/Windows`, or Windows App SDK window APIs such as `AppWindow.MoveAndResize`, fails on these targets.
* **Libraries without a Windows build.** A MAUI library that ships only Android and iOS builds can't be used on these targets.
* **`BlazorWebView`** is not supported.
* **Blocking calls.** Code that waits on asynchronous work with `.Result` or `.Wait()` blocks the browser's only thread.
