---
uid: uno.publishing.windows.sideload.unpackaged.unsigned
---

# Publish an unpackaged app

This guide will show you how to create an unpackaged app using Windows App SDK.

> [!IMPORTANT]
> Building your app requires using the msbuild command (`dotnet build` is not compatible as of WinAppSDK 1.5).
> [!NOTE]
> Uno Platform also supports building apps for using the `net10.0-desktop` target framework, using Uno Platform's own Skia Desktop implementation.

## Build the app for publishing

Packaging the app without a code signature allows the app to be installed on a machine without installing the signer's certificate. This guide portion is derived from the [official Windows App SDK documentation](https://learn.microsoft.com/en-us/windows/msix/package/unsigned-package).

To publish your app:

1. Navigate to the folder of the app's `.csproj` (Building at the solution level is not supported)
2. Build your app using the following command:

    ```pwsh
    msbuild /r /t:publish /p:TargetFramework=net10.0-windows10.0.26100 /p:Configuration=Release /p:Platform=x64 /p:PublishDir=c:\temp\myoutput
    ```

In order to build for additional platforms, change the `Platform` parameter to `x86` or `arm64` to create additional MSIX files.

## Publish the Windows app

Publishing your app can be done through different means:

- [ClickOnce](https://learn.microsoft.com/visualstudio/deployment/quickstart-deploy-using-clickonce-folder?view=vs-2022)
- Using a Zip file, then running the app using `[yourapp].exe`
