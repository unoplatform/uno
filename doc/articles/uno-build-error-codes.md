---
uid: Build.Solution.error-codes
---
# Uno error codes

## MSBuild Errors

### UNOB0001: Cannot build with both Uno.WinUI and Uno.UI NuGet packages referenced

This error code means that a project has determined what both `Uno.WinUI` and `Uno.UI` packages are referenced.

To fix this issue, you may be explicitly referencing `Uno.UI` and `Uno.WinUI` in your `csproj`, or you may be referencing a NuGet package that is incompatible with your current project's configuration.

For instance, if your project references `Uno.WinUI`, and you try to reference `SkiaSharp.View.Uno`, you will get this error. To fix it, you'll need to reference `SkiaSharp.View.Uno.WinUI` instead.

### UNOB0002: Project XX contains a reference to Uno Platform but does not contain a WinAppSDK compatible target framework

This error code means that a WinAppSDK project is referencing a project in your solution which is not providing a `net7.0-windows10.xx` TargetFramework.

This can happen if a project contains only a `net7.0` TargetFramework and has a NuGet reference to `Uno.WinUI`.

To fix this, it is best to start from a `Cross Platform Library` project template provided by the Uno Platform [visual studio extension](xref:Guide.HowTo.Create-Control-Library), or using [`dotnet new`](xref:Uno.GetStarted.dotnet-new).

### UNOB0003: Ignoring resource XX, could not determine the language

This error may occur during resources (`.resw`) analysis if the framework does not recognize the specified language code.

For instance, the language code `zh-CN` is not recognized and `zh-Hans` should be used instead.

## Compiler Errors

### UNO0001

A member is not implemented, see [this page](xref:Uno.Development.NotImplemented) for more details.

### UNO0002

**Do not call Dispose() on XXX**

On iOS and Catalyst, calling `Dispose()` or `Dispose(bool)` on a type inheriting directly from `UIKit.UIView` can lead to unstable results. It is not needed to call `Dispose` as the runtime will do so automatically during garbage collection.

Invocations to `Dispose` can cause the application to crash in `__NSObject_Disposer drain`, cause `ObjectDisposedException` exception to be thrown. More information can be found in [xamarin/xamarin-macios#19493](https://github.com/xamarin/xamarin-macios/issues/19493).
