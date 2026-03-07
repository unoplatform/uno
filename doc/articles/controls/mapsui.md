---
uid: Uno.Controls.MapsUI
---

# Maps with MapsUI

[MapsUI](https://github.com/Mapsui/Mapsui) is the recommended community-based mapping solution for Uno Platform apps. It supports all Uno Platform targets including iOS, Android, WebAssembly, macOS, Linux, and Windows.

> [!NOTE]
> The `Uno.WinUI.Maps` package, which provides the `MapControl`, is deprecated. Use MapsUI instead for new applications.

## What is MapsUI?

MapsUI is an open-source .NET map control library that supports multiple platforms. It provides:

- Interactive map rendering using various tile sources (OpenStreetMap, Bing Maps, etc.)
- Support for adding markers and custom pins
- Support for polygons, lines, and other geometric shapes
- Map click/tap events
- User location display
- Cross-platform support for iOS, Android, WebAssembly, macOS, Linux, and Windows

## Getting Started

### 1. Install the NuGet package

Add the `Mapsui.Uno.WinUI` NuGet package to your Uno Platform project:

```xml
<PackageReference Include="Mapsui.Uno.WinUI" Version="x.x.x" />
```

> [!TIP]
> Check [NuGet.org](https://www.nuget.org/packages/Mapsui.Uno.WinUI) for the latest available version.

### 2. Add the map to your XAML

```xml
<Page x:Class="MyApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:mapsui="using:Mapsui.UI.WinUI">

    <Grid>
        <mapsui:MapControl x:Name="mapControl" />
    </Grid>
</Page>
```

### 3. Configure the map in code-behind

```csharp
using Mapsui;
using Mapsui.Tiling;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        mapControl.Map = CreateMap();
    }

    private static Map CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        return map;
    }
}
```

## Adding Markers (Pins)

```csharp
using Mapsui;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Layers;
using Mapsui.Nts;

private void AddMarker(double longitude, double latitude)
{
    // SphericalMercator.FromLonLat returns a (double x, double y) named tuple
    var point = SphericalMercator.FromLonLat(longitude, latitude);
    var feature = new PointFeature(point.x, point.y);
    feature.Styles.Add(new SymbolStyle
    {
        SymbolScale = 0.5,
        Fill = new Brush(Color.Red)
    });

    var memoryLayer = new MemoryLayer
    {
        Name = "Markers",
        Features = new[] { feature }
    };

    mapControl.Map.Layers.Add(memoryLayer);
}
```

## Handling Map Tap Events

```csharp
mapControl.Map.Info += OnMapInfo;

private void OnMapInfo(object? sender, MapInfoEventArgs e)
{
    if (e.MapInfo?.WorldPosition != null)
    {
        var lonLat = SphericalMercator.ToLonLat(
            e.MapInfo.WorldPosition.X,
            e.MapInfo.WorldPosition.Y);

        // Handle the tap at longitude: lonLat.lon, latitude: lonLat.lat
    }
}
```

## Navigating to a Location

```csharp
using Mapsui.Extensions;
using Mapsui.Projections;

private void NavigateTo(double longitude, double latitude, double zoomLevel = 14)
{
    // SphericalMercator.FromLonLat returns a (double x, double y) named tuple
    var point = SphericalMercator.FromLonLat(longitude, latitude);
    mapControl.Map.Navigator.NavigateTo(new MPoint(point.x, point.y), zoomLevel);
}
```

## Platform Support

MapsUI supports all major Uno Platform targets:

| Platform | Support |
|----------|:-------:|
| Android  |    ✓    |
| iOS      |    ✓    |
| macOS    |    ✓    |
| WebAssembly |  ✓   |
| Windows  |    ✓    |
| Linux (Skia) | ✓   |

## Additional Resources

- [MapsUI GitHub Repository](https://github.com/Mapsui/Mapsui)
- [MapsUI Documentation](https://mapsui.com/documentation/)
- [MapsUI NuGet Package (Uno.WinUI)](https://www.nuget.org/packages/Mapsui.Uno.WinUI)
- [MapsUI Samples](https://github.com/Mapsui/Mapsui/tree/master/Samples)
