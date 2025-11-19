---
uid: Uno.UI.CommonIssues.vs2022
---

# Issues related to Visual Studio 2022/2026

## Unable to select the `MyApp (Unpackaged WinAppSDK)` profile

A [Visual Studio issue](https://developercommunity.visualstudio.com/t/WinAppSDK-Unpackaged-profile-cannot-be-s/10643735) is preventing the Unpackaged profile if iOS/Android target frameworks are present in the project. In order for the unpackaged profile to be selected, you'll need to edit the `Properties/launchSettings.json` file to remove the `MyApp (Packaged WinAppSDK)` entry. Once it is removed, select the `MyApp (Unpackaged WinAppSDK)` then start the debugging of your app.

## An iOS fails to run with `No class inheriting from a valid Application Delegate found`

When using iOS Hot Restart on Visual Studio /2026, a [limitation of the environment](https://developercommunity.visualstudio.com/t/iOS-Hot-Restart-does-not-work-for-non-MA/10714660) prevents an Uno Platform app from starting properly when MAUI Embedding is referenced.

A workaround is to disable MAUI Embedding in the [`UnoFeatures` of your project](xref:Uno.Features.Uno.Sdk#uno-platform-features).

## App builds in Visual Studio 2022/2026 are taking a long time

Take a [look at our article](xref:Build.Solution.TargetFramework-override) in order to ensure that your solution is building and showing intellisense as fast as possible, and to avoid [this Visual Studio issue](https://developercommunity.visualstudio.com/t/Building-a-cross-targeted-project-with-m/651372?space=8&q=building-a-cross-targeted-project-with-many-target) (help the community by upvoting it!) where multi-targeted project libraries always build their full set of targets.

## My app is not running as fast as I want

There could be many reasons for being in this situation, but we've built a list of performance tips in [this article](xref:Uno.Development.Performance) that you can apply to your app. If you haven't found your answer, open a [discussion](https://github.com/unoplatform/uno/discussions) to tell us about it!

## C# Hot Reload troubleshooting

C# Hot Reload is provided by Visual Studio 2022/2026, and there may be occasions where updates are not applied, or the modified code is incorrectly reported as not compiling.

If that is the case:

- Make sure that the top left selector in the C# editor is showing the project head being debugged. For instance, if debugging with `net9.0-desktop`, select the `net9.0-desktop` project.
- Try recompiling the application completely (with the `Rebuild` command)

More troubleshooting information is available [in this section](xref:Uno.Features.HotReload).

## error NETSDK1148: A referenced assembly was compiled using a newer version of Microsoft.Windows.SDK.NET.dll

See [this article](features/winapp-sdk-specifics.md#adjusting-windows-sdk-references) to solve this issue.

### My application does not start under WSL

Your application may fail to run under WSL for multiple reasons:

- Your app is in a path that contains spaces and/or characters such as `[` or `]`
- [WSLg](xref:Uno.GetStarted.vs2022#additional-setup-for-windows-subsystem-for-linux-wsl) has not been installed
- [X11 dependencies](xref:Uno.GetStarted.vs2022#additional-setup-for-skia-desktop-projects) have not been installed

### New Projects in Existing Solutions

Creating a new Uno Platform project inside an existing solution that wasn’t originally created with “Place solution and project in the same directory” is not supported by the `unoapp` templates. You can work around this by following the guide: [Adding Platforms to an Existing Project](xref:Uno.Guides.AddAdditionalPlatforms).

## Legacy issues

### The XAML editor shows `The type 'page' does not support direct content` message

This issue has been fixed in Visual Studio 17.8 and later.

If you're using an earlier version, XAML Intellisense [is not working properly](https://developercommunity.visualstudio.com/content/problem/587980/xaml-intellisense-does-not-use-contentpropertyattr.html) in Visual Studio when the active target framework is not the WinAppSDK one.

To work around this issue, close all XAML editors, open a C# file and select the '[MyApp].Windows' in the top-left drop-down list of the text editor sector. Once selected, re-open the XAML file.

### `InitializeComponent` or `x:Name` variable is not available in code-behind

This issue has been fixed in Visual Studio 17.8 and later.

If you're using an earlier version, Visual Studio [does not refresh the intellisense cache](https://developercommunity.visualstudio.com/content/problem/588021/the-compile-itemgroup-intellisense-cache-is-not-re.html) properly, causing variables to be incorrectly defined.

To fix this issue, build your project once, close the solution and reopen it.

It is also important to note that Uno Platform uses a multi-project structure, for which each project has to be build individually for errors to disappear from the **Error List** window (notice the **Project** column values).

In order to clear the **Error List** window, build the whole solution completely once. Thereafter, build a specific project and prefer the use of the **Output** tool window (in the menu **View** -> **Output**), taking build messages by order of appearance.

### Event handler cannot be added automatically

Event handlers [cannot be automatically](https://github.com/unoplatform/uno/issues/1348#issuecomment-520300471) added using the XAML editor.

A workaround is to use the [`x:Bind` to events feature](features/windows-ui-xaml-xbind.md#examples). This feature allows to use a simpler syntax like `<Button Click="{x:Bind MyClick}" />` and declare a simple method `private void MyClick() { }` in the code-behind.

### WebAssembly: Hot Reload fails to start with Mixed Content: The page at XXX was loaded over HTTPS, but attempted to connect to the insecure WebSocket endpoint

This issue is caused by visual studio enforcing https connections for local content. You can work around this by either:

- Removing the https endpoint in the `Properties/launchSettings.json` file
- Unchecking the `Use SSL` option in the project's Debug launch profiles
- Selecting the project name instead of IISExpress in the toolbar debug icon drop down list

### Error MSB3030: Could not copy the file "MyProject.Shared\MainPage.xbf" because it was not found

This issue is present in Visual Studio 17.2 and 17.3, and can be addressed by [taking a look at this issue](https://github.com/unoplatform/uno/discussions/5007#discussioncomment-2583741).
