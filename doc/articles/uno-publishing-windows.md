---
uid: uno.publishing.windows
---

# Publishing Your App for Windows

## Preparing For Publish

## Building your app

Building your app requires using the msbuild command (`dotnet build` is not compatible as of WinAppSDK 1.5).

### Side loading (without signing)

#### Package the unsigned app

Packaging the app without a code signature allows for installing the app on a machine without having to install the signer's certificate. This portion of the guide is derived from the [official Windows App SDK documentation](https://learn.microsoft.com/en-us/windows/msix/package/unsigned-package).

To package your app:

- Update the `Package.appxmanifest` file with the following `Identity` node to include the following `OID`:

  ```xml
  <Identity Name="MyApp"
            Publisher="CN=AppModelSamples, OID.2.25.311729368913984317654407730594956997722=1"
            Version="1.0.0.0" />
  ```

- Build your app in the folder of the app's `.csproj` by using the following command:

    ```pwsh
    msbuild /r /p:TargetFramework=net8.0-windows10.0.19041 /p:Configuration=Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=Sideloading /p:AppxPackageDir="C:/temp/output/" /p:AppxPackageSigningEnabled=false
    ```

In order to build for additional platforms, change the `Platform` parameter to `x86` or `arm64` to create additional MSIX files.

> [!IMPORTANT]
> IF your solution contains `net8.0` projects, you will need to split the above build command in two, one to restore NuGet packages, and the other one to create the package.
>
> First run this command:
>
> ```pwsh
> msbuild /r /t:Restore /p:Configuration=Release
> ```
>
> Then run the package creation command without the `/r` parameter.

#### Install the unsigned app

To install the app:

- Start an elevated PowerShell command prompt (Search for PowerShell in the start menu, right-click on it then Run as administrator):
- In the folder containing the `.msix` file, execute the following command :

    ```pwsh
    Add-AppPackage -AllowUnsigned ".\MyApp.appx"
    ```

For more information, see the [official documentation](https://learn.microsoft.com/en-us/windows/msix/package/unsigned-package#install-an-unsigned-package).

### Side loading (with signing)

#### Package signed the app

This guide uses a self-signed certificate.

To package your app:

- Create a self-signed certificate:
  - Open the solution in Visual Studio 2022
  - Double-click on the `Package.appxmanifest` file
  - Navigate to the `Packaging` tab
  - Click the **Choose certificate** button
  - Click the **Create** button, then set a **publisher common name**, then OK. Do not set a password.
  - Close the **Choose a Certificate** window by clicking OK.
  - Click the **Save file** button in the Visual Studio toolbar
- Build the app on the command line in the folder of the app's `.csproj`, use the following command:

  ```cmd
  msbuild /r /p:TargetFramework=net8.0-windows10.0.19041 /p:Configuration=Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=Sideloading /p:AppxPackageDir="C:/temp/output/" /p:AppxPackageSigningEnabled=true

> [!IMPORTANT]
> IF your solution contains `net8.0` projects, you will need to split the above build command in two, one to restore NuGet packages, and the other one to create the package.
>
> First run this command:
>
> ```pwsh
> msbuild /r /t:Restore /p:Configuration=Release
> ```
>
> Then run the package creation command without the `/r` parameter.

In order to build for additional platforms, change the `Platform` parameter to `x86` or `arm64` to create additional MSIX files.

> [!IMPORTANT]
> Single package msix bundling is [not yet supported from msbuild the command line](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/single-project-msix?tabs=csharp#automate-building-and-packaging-your-single-project-msix-app).

#### Install the signed app

To install the app:

- Install the certificate on the machine:
  - In the file explorer, right-click on the `.msix` file, then **Show More Options** on Windows 11, then **Properties**
  - Open the **Digital signatures** tab
  - Click on your certificate then **Details**
  - Click the **View Certificate** button
  - Click the **Install Certificate** button
  - Select **Local Machine** then **Next**
  - Click **Place all certificates in the following store** then **Browse**
  - Select **Trusted People** then **OK**
  - Click **Next**, then **Finish**
  - Close all the opened properties windows.
- Install the `.msix` by double-clicking on it

The app will start automatically once installed.

### Build your app for the Store

_To be documented._

## Publish your app to the store

_To be documented._
