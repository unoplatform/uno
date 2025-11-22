---
uid: Uno.UI.CommonIssues.AllIDEs
---

# Issues related to all development environments

## Could not resolve SDK "Uno.Sdk"

This error may happen for multiple reasons:

- Make sure to update your [Uno Platform extension](https://aka.platform.uno/vs-extension-marketplace) in VS 2022/2026 to 5.3.x or later. Earlier versions may automatically update to an incorrect version of the Uno.SDK.
- Make sure to [re-run Uno.Check](xref:UnoCheck.UsingUnoCheck) to get all the latest dependencies.
- Ensure that all [NuGet feeds are authenticated properly](https://learn.microsoft.com/nuget/consume-packages/consuming-packages-authenticated-feeds). When building on the command line, some enterprise NuGet feeds may not be authenticated properly.
- Ensure that no global package mappings are interfering with nuget restore. To validate that no package mappings are set, on Windows for Visual Studio 2022/2026:
  - Make a backup copy of `%AppData%\NuGet\NuGet.Config`
  - Open a visual studio instance that does not have any solution opened
  - Go to **Tools**, **Options**, **NuGet Package Manager**, then **Package Source Mappings**
  - If there are entries in the list, click then click **Remove All**
- Delete the `Uno.Sdk` folder in your development environment's Nuget packages `global-packages` folder:
  - Windows: `%userprofile%\.nuget\packages`
  - Mac/Linux: `~/.nuget/packages`
  [This folder may be overridden](https://learn.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders) using the `NUGET_PACKAGES` environment variable, the `globalPackagesFolder` or `repositoryPath` configuration settings (when using PackageReference and `packages.config`, respectively), or the `RestorePackagesPath` MSBuild property (MSBuild only). The environment variable takes precedence over the configuration setting.

Try building your project again.

## Runtime error `No parameterless constructor defined for XXXX`

This error is generally caused by some missing [IL Linker](https://github.com/dotnet/runtime/tree/main/src/tools/illink) configuration on WebAssembly. You may need to add some of your application assemblies in the LinkerConfig.xml file of your project. You can find [additional information in the documentation](xref:uno.articles.features.illinker).

Similar error messages using various libraries:

- `Don't know how to detect when XXX is activated/deactivated, you may need to implement IActivationForViewFetcher` (ReactiveUI)

## `Layout cycle detected` exception

Layout cycle means that the measuring of a specific part of the visual tree couldn't get stabilized. For example, during an element `Arrange` pass, its measure was invalidated, then it's measured again then arranged, and the app will fall into a layout cycle.

Uno Platform and WinUI run this loop for 250 iterations. If the loop hasn't stabilized, the app will fail with an exception with the message `Layout cycle detected`. For more information, see also [LayoutCycleTracingLevel in Microsoft Docs](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.debugsettings.layoutcycletracinglevel). Note that what Uno Platform logs may be quite different from what WinUI logs.

This error is sometimes tricky to debug. To get more information, in your `App.OnLaunched` method, you can call `DebugSettings.LayoutCycleTracingLevel = Microsoft.UI.Xaml.LayoutCycleTracingLevel.High` in order to get additional troubleshooting information printed out in the app's logs. You will also need to update your logging to `.SetMinimumLevel(LogLevel.Trace)`, as well as add `builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Trace);` if you set the log level to high and want to get stack trace information.

When the last 10 iterations out of 150 are reached, we will start logging some information as warnings. Those logs are prefixed with `[LayoutCycleTracing]` and include information such as when an element is measured or arranged, and when measure or arrange is invalidated.

One possible cause of layout cycle is incorrect usage of `LayoutUpdated` event. This event isn't really tied to a specific `FrameworkElement` and is fired whenever any element changes its layout in the visual tree. So, using this event to add or remove an element to the visual tree can lead to layout cycle. The simplest example is having XAML similar to the following

```xaml
<StackPanel x:Name="sp" LayoutUpdated="sp_LayoutUpdated" />
```

and code behind:

```csharp
private void sp_LayoutUpdated(object sender, object e)
{
    sp.Children.Add(new Button() { Content = "Button" });
}
```

In this case, when `LayoutUpdated` is first fired, you add a new child to the `StackPanel` which will cause visual tree root to have its measure invalidated, then `LayoutUpdated` gets fired again, causing visual tree root to have its measured invalidated again, and so on. This ends up causing a layout cycle.

## Cannot build with both Uno.WinUI and Uno.UI NuGet packages referenced

This issue generally happens when referencing an Uno.UI (using WinUI APIs) NuGet package in an application that uses Uno.WinUI (Using WinAppSDK APIs).

For instance, if your application has `<PackageReference Include="Uno.WinUI"` in the `csproj` files, this means that you'll need to reference WinUI versions of NuGet packages.

For instance:

- `Uno.UI` -> `Uno.WinUI`

## Abnormally long build times when using Roslyn analyzers

It is a good practice to use Roslyn analyzers to validate your code during compilation, but some generators may have difficulty handling the source generated by the Uno Platform (one notable example is [GCop](https://github.com/Geeksltd/GCop)). You may need to disable those for Uno projects or get an update from the analyzer's vendor.

## Build errors with .NET workloads

If you encounter errors such as: `error MSB4022: The result "" of evaluating the value "$(MonoTargetsTasksAssemblyPath)" of the "AssemblyFile" attribute in element <UsingTask> is not valid.`, this usually indicates a problem with your .NET workloads installation. If `uno-check` does not report issues but your project still fails to build, try running:

  ```dotnetcli
  dotnet workload repair
  ```

This command can repair missing or misconfigured workload components.
