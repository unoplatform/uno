---
uid: uno.publishing.ios
---

# Publishing Your App for iOS

## Preparing For Publish

- [Performance Profiling](xref:Uno.Tutorials.ProfilingApplications)

## Building your app

### Packaging your app using the CLI

> [!IMPORTANT]
> Publishing for iOS is only supported on macOS

First, you'll need to setup to be able to build iOS apps. You can follow [these steps for publishing .NET iOS apps](https://learn.microsoft.com/dotnet/maui/ios/deployment/?view=net-maui-8.0).

Then, you'll need to setup your `csproj` to include the signing information:

```xml
<Choose>
    <When Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
       <PropertyGroup Condition="'$(Configuration)'=='Release'">
         <CodesignKey>iPhone Distribution</CodesignKey>
         <RuntimeIdentifier>ios-arm64</RuntimeIdentifier>
       </PropertyGroup>
    </When>
</Choose>
```

To build your app from the CLI on macOS:

- Open a terminal, command line, or powershell
- Navigate to your `csproj` folder
- Publish the app using:

  ```shell
  dotnet publish -f net10.0-ios -c Release -o ./publish
  ```

The output `.ipa` file will be in the `publish` folder.

### App size on .NET 11

On `net11.0-ios`, Release builds run on CoreCLR and precompile every method that survives trimming into one composite ReadyToRun image, so anything the trimmer keeps costs app size twice. To keep that image small, Uno.Sdk trims these on iOS and tvOS Release builds:

- Binding to `ExpandoObject` and `DynamicObject` sources, which keeps `System.Linq.Expressions` alive. If your app binds to dynamic objects (for example with `Elmish.Uno` or the `FromJson` markup extension), opt back in:

  ```xml
  <PropertyGroup>
      <UnoEnableDynamicObjectBinding>true</UnoEnableDynamicObjectBinding>
  </PropertyGroup>
  ```

- `Microsoft.Extensions.DependencyInjection` is only kept whole in Debug builds, where it's needed for device logging. Set `UnoDisableMEDITrimmingWorkaround` to `true` to stop keeping it in Debug too.

## Publishing your app on the App Store

Publishing your app is done through [the transporter app](https://developer.apple.com/help/app-store-connect/manage-builds/upload-builds) on macOS.
