---
uid: uno.publishing.maccatalyst
---

# Publishing Your App for Mac Catalyst

## Preparing For Publish

- [Performance Profiling](xref:Uno.Tutorials.ProfilingApplications)

## Building your app

### Packaging your app using Mac Catalyst

> [!IMPORTANT]
> Publishing for Mac Catalyst is only supported on macOS

First, you'll need to setup to be able to build Mac Catalyst apps. You can follow [these steps for publishing .NET Mac Catalyst apps](https://learn.microsoft.com/en-us/dotnet/maui/mac-catalyst/deployment).

> [!IMPORTANT]
> If you intend to publish your app on TestFlight, make sure that the provisioning profile has the "Profile type" set to "Mac" and not "Mac Catalyst". Using "Mac Catalyst" will crash the app in similar ways to [this .NET issue](https://github.com/xamarin/xamarin-macios/issues/14686).

Then, you'll need to setup your `csproj` to include the signing information:

```xml
<Choose>
  <When Condition="'$(TargetFramework)'=='net8.0-maccatalyst'">
    <PropertyGroup Condition="'$(Configuration)'=='Release'">
      <CodeSigningKey>Apple Distribution: [YourCompany]. (Your ID)</CodeSigningKey>
      <PackageSigningKey>3rd Party Mac Developer Installer</PackageSigningKey>
      <EnablePackageSigning>true</EnablePackageSigning>
      <EnableCodeSigning>true</EnableCodeSigning>
      <CodesignProvision>[Your provisioning profile name]</CodesignProvision>
    </PropertyGroup>
  </When>
</Choose>
```

To build your app from the CLI on macOS:

- Open a terminal, command line, or powershell
- Navigate to your `csproj` folder
- Publish the app using:

  ```shell
  dotnet publish -f net8.0-maccatalyst -c Release -p:CreatePackage=true -o ./publish
  ```

The output `.pkg` file will be in the `publish` folder.

> [!TIP]
> To create a `.app` folder, set the `CreatePackage` parameter to `false`.

## Publishing your app om the App Store

Publishing your app is done through [the transporter app](https://developer.apple.com/help/app-store-connect/manage-builds/upload-builds) on macOS.
