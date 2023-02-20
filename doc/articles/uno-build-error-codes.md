---
uid: Build.Solution.error-codes
---
# Uno Build error codes

## UNOB0001: Cannot build with both Uno.WinUI and Uno.UI NuGet packages referenced

This error code means that a project has determined what both `Uno.WinUI` and `Uno.UI` packages are referenced.

To fix this issue, you may be explicitly referencing `Uno.UI` and `Uno.WinUI` in your `csproj`, or you may be referencing a NuGet package that is incompatible with your current project's configuration.

For instance, if your project references `Uno.WinUI`, and you try to reference `SkiaSharp.View.Uno`, you will get this error. To fix it, you'll need to reference `SkiaSharp.View.Uno.WinUI` instead.

## UNOB0002: Project XX contains a reference to Uno Platform but does not contain a WinAppSDK compatible target framework.

This error code means that a WinAppSDK project is referencing a project in your solution which is not providing a `net6.0-windows10.xx` TargetFramework.

This can happen if a project contains only a `net7.0` TargetFramework and has a NuGet reference to `Uno.WinUI`.

To fix this, it is best to start from a `Cross Platform Library` project template provided by the Uno Platform [visual studio extension](xref:Guide.HowTo.Create-Control-Library), or using [`dotnet new`](xref:GetStarted.dotnet-new).
