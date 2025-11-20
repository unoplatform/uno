---
uid: uno.publishing.windows.sideload.packaged.signed
---

# Build a signed packaged app

This guide will show how to create a signed packaged app using Windows App SDK.

> [!IMPORTANT]
> Building your app requires using the msbuild command (`dotnet build` is not compatible as of WinAppSDK 1.5).

## Package signed the app

This guide uses a self-signed certificate.

To package your app:

- Create a self-signed certificate:
  - Open the solution in Visual Studio 2022/2026
  - Ensure that the active debugging target framework is `net9.0-windows10.0.xxxxx` (Please upvote [this Visual Studio issue](https://developercommunity.visualstudio.com/t/Double-clicking-on-a-PackageAppxmanifes/10658683))
  - Double-click on the `Package.appxmanifest` file
  - Navigate to the `Packaging` tab
  - Click the **Choose certificate** button
  - Click the **Create** button, then set a **publisher common name**, then OK. Do not set a password.
  - Close the **Choose a Certificate** window by clicking OK.
  - Click the **Save file** button in the Visual Studio toolbar
- Navigate to the folder of the app's `.csproj` (Building at the solution level is not supported)
- Build the app on the command line with the following command:

  ```shell
  msbuild /r /p:TargetFramework=net9.0-windows10.0.26100 /p:Configuration=Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=Sideloading /p:AppxPackageDir="C:/temp/output/" /p:AppxPackageSigningEnabled=true
  ```

To package your app for the Microsoft App Store, the process is similar to creating a self-signed app package with just a minor difference:

- Instead of linking to a self-signed certificate, associate your project with a Microsoft Store Application by right-clicking on your project in the solution explorer, then the **Publish**, **Associate App with Store...** menu item.
- Build the app on the command line with the following command:

  ```shell
  msbuild /r /p:TargetFramework=net9.0-windows10.0.26100 /p:Configuration=Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=StoreUpload /p:AppxPackageDir="C:/temp/output/"
  ```

In order to build for additional platforms, change the `Platform` parameter to `x86` or `arm64` to create additional MSIX files.

> [!IMPORTANT]
> Single package msix bundling is [not yet supported from msbuild the command line](https://learn.microsoft.com/windows/apps/windows-app-sdk/single-project-msix?tabs=csharp#automate-building-and-packaging-your-single-project-msix-app). The individual msix packages can be assembled after creation using Microsoft's `makeappx.exe` tool installed with the Windows SDK in 'Windows Kits' folder, for example `C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe`.

To bundle the individual msix packages, move them all to a common folder, for example, "C:\Temp\Output\MyApp", and run the following command:

```shell
  "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe" bundle /d "C:\Temp\Output\MyApp" /p "C:\Temp\Output\MyApp.msixbundle"
```

> [!TIP]
> The `makeappx.exe` tool is also available from the environment when opening a [**Developer Command Prompt for VS 2022**](https://learn.microsoft.com/en-us/visualstudio/ide/reference/command-prompt-powershell?view=vs-2022)

## Considerations for solutions with class library projects

If your app references multiple library projects, you will need to split the above build command into two parts, one to restore NuGet packages, and the other one to create the package.

To build your solution:

- Add the following to your app's csproj:

  ```xml
  <PropertyGroup Condition=" '$(PublishSignedPackage)' == 'true' ">
    <GenerateAppxPackageOnBuild>true</GenerateAppxPackageOnBuild>
    <AppxBundle>Never</AppxBundle>
    <UapAppxPackageBuildMode>Sideloading</UapAppxPackageBuildMode>
    <AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>  
  </PropertyGroup>
  ```

- Run this command to restore the NuGet packages:

  ```shell
  msbuild /r /t:Restore /p:Configuration=Release
  ```

- Then run this command:

  ```shell
  msbuild /p:TargetFramework=net9.0-windows10.0.26100 /p:Configuration=Release /p:Platform=x64 /p:PublishSignedPackage=true /p:AppxPackageDir="C:/temp/output/"
  ```

  Notice that this command does not contain the `/r`.

## Install the signed Windows app

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
