---
uid: uno.publishing.desktop
---

<!-- markdownlint-disable MD001 MD051 -->

# Publishing Your App For Desktop

## Preparing For Publish

- [Profile your app with Visual Studio](https://learn.microsoft.com/en-us/visualstudio/profiling)
- [Profile using dotnet-trace and SpeedScope](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)

## Publish Using Visual Studio 2022/2026

- In the debugger toolbar drop-down, select the `net9.0-desktop` target framework
- Once the project has reloaded, right-click on the project and select **Publish**
- Select the **Folder** target for your publication then click **Next**
- Select the **Folder** target again then **Next**
- Choose an output folder then click **Finish**
- The profile is created, you can now **Close** the dialog
- In the opened editor, click `Show all settings`
- Set **Configuration** to `Release`
- Set **Target framework** to `net9.0-desktop`
- You can set **Deployment mode** to either `Framework-dependent` or `Self-contained`
  - If `Self-contained` is chosen and you're targeting Windows, **Target runtime** must match the installed .NET SDK runtime identifier
    as cross-publishing self-contained WPF apps (e.g. win-x64 to win-arm64) is not supported for now.
- You can set **Target runtime**, make sure it honors the above limitation, if it applies.
- Click **Save**
- Click **Publish**

## Publish Using The CLI

On Windows/macOS/Linux, open a terminal in your `csproj` folder and run:

```shell
dotnet publish -f net9.0-desktop
```

If you wish to do a self-contained publish, run the following instead:

```shell
dotnet publish -f net9.0-desktop -r {{RID}} -p:SelfContained=true -p:TargetFrameworks=net9.0-desktop
```

Where `{{RID}}` specifies [the chosen OS and Architecture](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog) (e.g. win-x64). When targeting Windows, cross-publishing to architectures other than the currently running one is not supported.

> [!IMPORTANT]
> Due to changes in the .NET SDK, when providing an `{{RID}}` you will also need to add the following parameter `-p:TargetFrameworks=net9.0-desktop` for the publish command to succeed.

### Single-file publish

[Single file](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli) publishing is supported with the following parameters:

```shell
dotnet publish -f net9.0-desktop -r {{RID}} -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true
```

Same as above, make sure to replace the `{{RID}}` with [a valid value](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog).

The `IncludeNativeLibrariesForSelfExtract` and `IncludeAllContentForSelfExtract` properties can also be set in a `PropertyGroup` in the `.csproj`.

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
    <TargetFramework>net9.0-desktop</TargetFramework>
    <UpdateEnabled>False</UpdateEnabled>
    <UpdateMode>Foreground</UpdateMode>
    <UpdateRequired>False</UpdateRequired>
    <WebPageFileName>Publish.html</WebPageFileName>

    <!-- Those two lines below need to be removed when building using "UnoClickOncePublishDir" -->
    <PublishDir>bin\Release\net9.0-desktop\win-x64\app.publish\</PublishDir>
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
> if iOS/Android are present in the TargetFrameworks list. In order to create
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
msbuild /m /r /target:Publish /p:Configuration=Release /p:PublishProfile="Properties\PublishProfiles\ClickOnceProfile.pubxml" /p:TargetFramework=net9.0-desktop
```

The resulting package will be located in the `bin\publish` folder. You can change the output folder using `/p:UnoClickOncePublishDir=your\output\directory`.

Depending on your deployment settings, you can run the `Setup.exe` file to install the application on a machine.

> [!IMPORTANT]
> At this time, publishing with the Visual Studio Publishing Wizard is not supported for
> multi-targeted projects. Using the command line above is required.
