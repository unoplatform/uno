---
uid: uno.publishing.windows.sideload.packaged.unsigned
---

# Build an unsigned packaged app

This guide will show how to create an unsigned packaged app using Windows App SDK.

> [!IMPORTANT]
> Building your app requires using the msbuild command (`dotnet build` is not compatible as of WinAppSDK 1.5).

## Package the unsigned app

Packaging the app without a code signature allows for installing the app on a machine without having to install the signer's certificate. This portion of the guide is derived from the [official Windows App SDK documentation](https://learn.microsoft.com/en-us/windows/msix/package/unsigned-package).

To package your app:

- Update the `Package.appxmanifest` file with the following `Identity` node to include the following `OID`:

  ```xml
  <Identity Name="MyApp"
            Publisher="CN=AppModelSamples, OID.2.25.311729368913984317654407730594956997722=1"
            Version="1.0.0.0" />
  ```

- Navigate to the folder of the app's `.csproj` (Building at the solution level is not supported)
- Build your app using the following command:

    ```pwsh
    msbuild /r /p:TargetFramework=net9.0-windows10.0.26100 /p:Configuration=Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=Sideloading /p:AppxPackageDir="C:/temp/output/" /p:AppxPackageSigningEnabled=false
    ```

In order to build for additional platforms, change the `Platform` parameter to `x86` or `arm64` to create additional MSIX files.

## Considerations for solutions with class library projects

If your app references multiple library projects, you will need to split the above build command into two parts, one to restore NuGet packages, and the other one to create the package.

To build your solution:

- Add the following to your app's csproj:

  ```xml
  <PropertyGroup Condition=" '$(PublishUnsignedPackage)' == 'true' ">
    <GenerateAppxPackageOnBuild>true</GenerateAppxPackageOnBuild>
    <AppxBundle>Never</AppxBundle>
    <UapAppxPackageBuildMode>Sideloading</UapAppxPackageBuildMode>
    <AppxPackageSigningEnabled>false</AppxPackageSigningEnabled>  
  </PropertyGroup>
  ```

- Run this command to restore the NuGet packages:

  ```pwsh
  msbuild /r /t:Restore /p:Configuration=Release
  ```

- Then run this command:

  ```pwsh
  msbuild /p:TargetFramework=net9.0-windows10.0.26100 /p:Configuration=Release /p:Platform=x64 /p:PublishUnsignedPackage=true /p:AppxPackageDir="C:/temp/output/"
  ```

  Notice that this command does not contain the `/r`.

## Install the unsigned Windows app

To install the app:

- Start an elevated PowerShell command prompt (Search for PowerShell in the start menu, right-click on it then Run as administrator):
- In the folder containing the `.msix` file, execute the following command :

    ```pwsh
    Add-AppPackage -AllowUnsigned ".\MyApp.appx"
    ```

For more information, see the [official documentation](https://learn.microsoft.com/en-us/windows/msix/package/unsigned-package#install-an-unsigned-package).
