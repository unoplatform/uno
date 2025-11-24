---
uid: Uno.Development.UwpVsWinUI3
---

# WinUI 3 and Uno Platform

WinUI 3 is the [next generation of Microsoft's Windows UI library](https://learn.microsoft.com/windows/apps/winui/). It succeeds the UWP XAML framework as Microsoft's actively developed native UI platform for Windows. WinUI 3 supports Windows Desktop apps through [Windows AppSDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/).

> [!TIP]
> If you just want to add Fluent styles to legacy Uno Platform projects, [check the guide here](features/using-winui2.md).

## How does WinUI 3 differ from UWP XAML?

WinUI 3 differs in minor ways from UWP XAML in terms of API, and in more substantial ways in its technical comportment.

### API

The main difference between WinUI 3 and UWP XAML is the change of namespace. UWP XAML namespaces begin with 'Windows' - `Windows.UI.Xaml`, `Windows.UI.Composition`, and so on. WinUI 3 namespaces begin with 'Microsoft' - `Microsoft.UI.Xaml`, `Microsoft.UI.Composition`, and so on. Aside from that change, the API surface is very similar. Some of the remaining differences are listed in our [guide to upgrading to WinUI 3](updating-to-winui3.md).

### Technical

Below the surface, the differences are more substantial. The UWP XAML stack is part of the Windows OS. The WinUI 3 stack is decoupled from the Windows OS. This means application developers can use the newest features without worrying that they might not be supported on the end user's system.

WinUI 3 is also decoupled from the application model. The UWP XAML stack is only compatible with the 'UWP model' in which the application runs in a secure sandbox. WinUI 3 is compatible both with the 'UWP model' and with the traditional 'Win32' or 'desktop' application model in which the application has largely-unrestricted access to the rest of the OS.

## How does this affect Uno Platform?

Uno Platform is only affected by the API change - the technical changes don't apply on non-Windows platforms.

When you create a new Uno Platform application, you can choose to create a WinUI 3-compatible application (using the WinUI 3 API, and building with WinUI 3 on the Windows head project) instead of a UWP XAML-compatible application [using the `dotnet new` templates](get-started-dotnet-new.md#uno-platform-blank-application-for-winappsdk---winui-3).


### Do you plan to publish on Windows?

If Windows is one of your target platforms, then the [technical differences](#technical) discussed above apply. Probably the key question is, can your application run in the sandboxed 'UWP model', or is it better served by the unrestricted 'Win32 model'?

### Do you depend on features that haven't yet migrated to WinUI 3?

[Check the WinUI 3 roadmap](https://github.com/microsoft/microsoft-ui-xaml/blob/master/docs/roadmap.md#winui-30-feature-roadmap) for a list of features that won't be available in the initial supported release, like `InkCanvas`.

## Further reading

* [Create a new WinUI 3-flavored Uno Platform app with Project Reunion support](get-started-winui3.md)
* [Migrate an existing Uno Platform app to WinUI 3](updating-to-winui3.md)
