---
uid: Uno.Development.SupportedLibraries
---

# Supported 3rd-party libraries

This article lists some of the 3rd-party libraries supported by Uno Platform.

> [!IMPORTANT]
> A library that ships Uno Platform-specific packages has to be built for the Uno Platform version your app uses. Builds made for Uno Platform 6.x or earlier are binary-incompatible with 7.0, so before you upgrade, check that each of those libraries has a release built for 7.0. Plain .NET libraries, such as CommunityToolkit.Mvvm or Refit, aren't affected. See [Migrating to Uno Platform 7](xref:Uno.Development.MigratingToUno7).

## Control libraries

* [Windows Community Toolkit](uno-community-toolkit.md)
* [WinUI.TableView](https://github.com/w-ahmad/WinUI.TableView) - Data grid control with sorting, filtering and export
* [Lightning Chart](https://platform.uno/blog/lightningchart-introduces-uno-platform-support/)
* [Live Charts](https://platform.uno/blog/livecharts-announces-support-for-uno-platform/)
* [ScottPlot](https://scottplot.net/quickstart/unoplatform/)
* [Mapsui](https://github.com/Mapsui/Mapsui) - Map component
* [LibVLCSharp](https://github.com/videolan/libvlcsharp) - Video and audio playback (Android, iOS and Windows)
* [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) - SVG rendering controls
* [FluentIcons](https://github.com/davidxuang/FluentIcons) - Fluent System Icons

## .NET MAUI controls

.NET MAUI control libraries can be used in an Uno Platform app through [.NET MAUI Embedding](xref:Uno.Extensions.Maui.Overview). They run only where .NET MAUI itself runs, so not on WebAssembly or Linux.

* [Syncfusion .NET MAUI controls](xref:Uno.Extensions.Maui.ThirdParty.Syncfusion)
* [Telerik UI for .NET MAUI](xref:Uno.Extensions.Maui.ThirdParty.Telerik)
* [DevExpress .NET MAUI controls](xref:Uno.Extensions.Maui.ThirdParty.DevExpress)
* [ComponentOne .NET MAUI controls](xref:Uno.Extensions.Maui.ThirdParty.GrapeCity)
* [Esri ArcGIS Maps SDK for .NET](xref:Uno.Extensions.Maui.ThirdParty.EsriMaps)
* [Grial UI Kit](xref:Uno.Extensions.Maui.ThirdParty.GrialKit)
* [.NET MAUI Community Toolkit](xref:Uno.Extensions.Maui.ThirdParty.MauiCommunityToolkit)

## Presentation frameworks

* [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
* [Prism](https://prismlibrary.com/)
* [ReactiveUI](https://www.reactiveui.net/)
* [Chinook.DynamicMvvm](https://github.com/nventive/Chinook.DynamicMvvm) and [Chinook.Navigation](https://github.com/nventive/Chinook.Navigation) - MVVM and navigation libraries

## Dependency injection

* [Pure.DI](https://github.com/DevTeam/Pure.DI/blob/master/readme/UnoApp.md) - Compile-time dependency injection with generated object graphs and no runtime container required

## Design and theming

* [Uno.Material](external/uno.themes/doc/material-getting-started.md)
* [Uno.Cupertino](external/uno.themes/doc/cupertino-getting-started.md)
* [Uno.Simple](external/uno.themes/doc/simple-getting-started.md)

## HTTP and API clients

* [Refit](xref:Uno.Extensions.Http.Overview#refit) - Strongly-typed REST API client
* [Kiota](xref:Uno.Extensions.Http.Overview#kiota) - OpenAPI/Swagger client generator

## Data and storage

* [SQLite](https://www.sqlite.org/) - Local database for all platforms including WebAssembly
* [LiteDB](https://www.litedb.org/) - Embedded .NET NoSQL database
* [Json.NET (Newtonsoft.Json)](https://www.newtonsoft.com/json) - Popular JSON framework for .NET

## Logging

* [Serilog](https://serilog.net/) - Flexible logging library

## Other libraries

* [SkiaSharp](https://www.nuget.org/packages/SkiaSharp.Views.Uno.WinUI)
* [Shiny .NET](https://shinylib.net/mediator/extensions/unoplatform/)
* [C# Markup 2](https://github.com/VincentH-Net/CSharpForMarkup) - Declarative C# UI markup

If we're missing a library here, [drop us a line](https://github.com/unoplatform/uno/issues/new/choose) or [create a Pull Request](https://github.com/unoplatform/uno/blob/master/doc/articles/supported-libraries.md)!
