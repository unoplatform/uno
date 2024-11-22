---
uid: uno.publishing.desktop
---

<!-- markdownlint-disable MD001 MD051 -->

# Publishing Your App For Desktop

## Preparing For Publish

- [Profile your app with Visual Studio](https://learn.microsoft.com/en-us/visualstudio/profiling)
- [Profile using dotnet-trace and SpeedScope](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)

## Publish Using Visual Studio 2022

- In the debugger toolbar drop-down, select the `net8.0-desktop` target framework
- Once the project has reloaded, right-click on the project and select **Publish**
- Select the **Folder** target for your publication then click **Next**
- Select the **Folder** target again then **Next**
- Choose an output folder then click **Finish**
- The profile is created, you can now **Close** the dialog
- In the opened editor, click `Show all settings`
- Set **Configuration** to `Release`
- Set **Target framework** to `net8.0-desktop`
- You can set **Deployment mode** to either `Framework-dependent` or `Self-contained`
  - If `Self-contained` is chosen and you're targeting Windows, **Target runtime** must match the installed .NET SDK runtime identifier
    as cross-publishing self-contained WPF apps (e.g. win-x64 to win-arm64) is not supported for now.
- You can set **Target runtime**, make sure it honors the above limitation, if it applies.
- Click **Save**
- Click **Publish**

## Publish Using The CLI

On Windows/macOS/Linux, open a terminal in your `csproj` folder and run:

```shell
dotnet publish -f net8.0-desktop
```

If you wish to do a self-contained publish, run the following instead:

```shell
dotnet publish -f net8.0-desktop -r {{RID}} -p:SelfContained=true
```

Where `{{RID}}` specifies the chosen OS and Architecture (e.g. win-x64). When targeting Windows, cross-publishing to architectures other than the currently running one is not supported.

### Windows ClickOnce

Uno Platform supports publishing desktop apps using ClickOnce to Windows environments.

In order to do so, you'll need to create a `.pubxml` file using Visual Studio, or to use the file below:

# [**Using a Sample Profile**](#tab/windows)

Create a file named `Properties\PublishProfiles\ClickOnceProfile.pubxml` in your project with the following contents:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- https://go.microsoft.com/fwlink/?LinkID=208121. -->
<Project>
  <PropertyGroup>
    <ApplicationRevision>0</ApplicationRevision>
    <ApplicationVersion>1.0.0.*</ApplicationVersion>
    <BootstrapperEnabled>True</BootstrapperEnabled>
    <Configuration>Release</Configuration>
    <CreateWebPageOnPublish>False</CreateWebPageOnPublish>
    <GenerateManifests>true</GenerateManifests>
    <Install>True</Install>
    <InstallFrom>Disk</InstallFrom>
    <IsRevisionIncremented>True</IsRevisionIncremented>
    <IsWebBootstrapper>False</IsWebBootstrapper>
    <MapFileExtensions>True</MapFileExtensions>
    <OpenBrowserOnPublish>False</OpenBrowserOnPublish>
    <Platform>x64</Platform>
    <PublishProtocol>ClickOnce</PublishProtocol>
    <PublishReadyToRun>False</PublishReadyToRun>
    <PublishSingleFile>False</PublishSingleFile>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>True</SelfContained>
    <SignatureAlgorithm>(none)</SignatureAlgorithm>
    <SignManifests>False</SignManifests>
    <SkipPublishVerification>false</SkipPublishVerification>
    <TargetFramework>net8.0-desktop</TargetFramework>
    <UpdateEnabled>False</UpdateEnabled>
    <UpdateMode>Foreground</UpdateMode>
    <UpdateRequired>False</UpdateRequired>
    <WebPageFileName>Publish.html</WebPageFileName>

    <!-- Those two lines below need to be removed when building using "UnoClickOncePublishDir" -->
    <PublishDir>bin\Release\net8.0-desktop\win-x64\app.publish\</PublishDir>
    <PublishUrl>bin\publish\</PublishUrl>
  </PropertyGroup>
  <ItemGroup>
    <!-- This section needs to be adjusted based on the target framework -->
    <BootstrapperPackage Include="Microsoft.NetCore.DesktopRuntime.8.0.x64">
      <Install>true</Install>
      <ProductName>.NET Desktop Runtime 8.0.10 (x64)</ProductName>
    </BootstrapperPackage>
  </ItemGroup>
</Project>
```

# [**Using the Wizard**](#tab/vs-wizard)

> [!NOTE]
> An existing Visual Studio issue prevents the **Publish** context menu from being active
> if iOS/Android/maccatalyst are present in the TargetFrameworks list. In order to create
> the file, you can temporarily remove those target frameworks from `TargetFrameworks` in
> order to create the `.pubxml` file generated.

To use the Visual Studio publishing wizard:

- Select the `netX.0-desktop` target framework in the debugger drop-down
- In the Solution Explorer, right click on your project then select **Publish**
- Click the **+ New profile** button
- Select **ClickOnce**, then **Next**
- Configure your app publishing in all the following wizard pages
- In the **Configuration** section, make sure to select **Portable** for the **Target runtime**
- Click **Finish**.

The `Properties\PublishProfiles\ClickOnceProfile.pubxml` file will be created.

***

Once done, you can use the following command in your CI environment, or using a **Developer Command Prompt for Visual Studio**:

```shell
msbuild /m /r /target:Publish /p:Configuration=Release /p:PublishProfile="Properties\PublishProfiles\ClickOnceProfile.pubxml" /p:TargetFramework=net8.0-desktop
```

The resulting package will be located in the `bin\publish` folder. You can change the output folder using `/p:UnoClickOncePublishDir=your\output\directory`.

Depending on your deployment settings, you can run the `Setup.exe` file to install the application on a machine.

> [!IMPORTANT]
> At this time, publishing with the Visual Studio Publishing Wizard is not supported for
> multi-targeted projects. Using the command line above is required.

### macOS App Bundles

We now support generating `.app` bundles on macOS machines. From the CLI run:

```shell
dotnet publish -f net8.0-desktop -p:PackageFormat=app
```

You can also do a self-contained publish with:

```shell
dotnet publish -f net8.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=app
```

Where `{{RID}}` is either `osx-x64` or `osx-arm64`.

> [!NOTE]
> Code signing is planned but not supported yet.

### Snap Packages

We support creating .snap packages on **Ubuntu 20.04** or later.

#### Requirements

The following must be installed and configured:

```bash
sudo apt-get install -y snapd
sudo snap install core22
sudo snap install multipass
sudo snap install lxd
sudo snap install snapcraft
lxd init --minimal
sudo usermod --append --groups lxd $USER # In order for the current user to use LXD
```

> [!NOTE]
> In the above script, replace `core22` with `core20` if building on Ubuntu 20.04, or `core24` if building on Ubuntu 24.04.
> [!NOTE]
> Docker may interfere with Lxd causing network connectivity issues, for solutions see: https://documentation.ubuntu.com/lxd/en/stable-5.0/howto/network_bridge_firewalld/#prevent-connectivity-issues-with-lxd-and-docker

#### Generate a Snap file

To generate a snap file, run the following:

```shell
dotnet publish -f net8.0-desktop -p:SelfContained=true -p:PackageFormat=snap
```

The generated snap file is located in the `bin/Release/netX.0-desktop/linux-[x64|arm64]/publish` folder.

Uno Platform generates snap manifests in classic confinement mode and a `.desktop` file by default.

If you wish to customize your snap manifest, you will need to pass the following MSBuild properties:

- `SnapManifest`
- `DesktopFile`

The `.desktop` filename MUST conform to the [Desktop File](https://specifications.freedesktop.org/desktop-entry-spec/latest) spec.

If you wish, you can generate a default snap manifest and desktop file by running the command above, then tweak them.

> [!NOTE]
> .NET 9 publishing and cross-publishing are not supported as of Uno 5.5, we will support .NET 9 publishing soon.

#### Publish your Snap Package

You can install your app on your machine using the following:

```bash
sudo snap install MyApp_1.0_amd64.snap --dangerous --classic
```

You can also publish your app to the [Snap store](https://snapcraft.io/store).

## Limitations

- NativeAOT is not yet supported
- R2R is not yet supported
- Single file publish is not yet supported

> [!NOTE]
> Publishing is a [work in progress](https://github.com/unoplatform/uno/issues/16440)

## Links

- [Snapcraft.yaml schema](https://snapcraft.io/docs/snapcraft-yaml-schema)
- [Desktop Entry Specification](https://specifications.freedesktop.org/desktop-entry-spec/latest)
