namespace uno56droidioswasmskia;

// Reports which asset of each package the head runs, and whether its platform-specific C# and XAML
// resolved as expected. Only compiles when the platform-specific asset of Uno56NugetLibrary is referenced.
internal static class PlatformAssetValidation
{
    public static string Run()
    {
        var multiTargeted = new UnoLibrary2.PlatformMarkerControl().XamlMarkers;
        var plain = new UnoPlainLibrary.PlatformMarkerControl().XamlMarkers;

#if __ANDROID__
        var actual = $"multi-targeted: C#={UnoLibrary2.PlatformMarker.Asset} (API {UnoLibrary2.PlatformMarker.AndroidOnlyApi()}) XAML={multiTargeted}; "
            + $"plain: C#={UnoPlainLibrary.PlatformMarker.Asset} runtime={UnoPlainLibrary.PlatformMarker.RuntimePlatform} XAML={plain}";
        var expected = UnoLibrary2.PlatformMarker.Asset == "android" && multiTargeted == "android,not_ios"
            && UnoPlainLibrary.PlatformMarker.Asset == "generic" && UnoPlainLibrary.PlatformMarker.RuntimePlatform == "android" && plain == "not_android,not_ios";
#elif __IOS__
        var actual = $"multi-targeted: C#={UnoLibrary2.PlatformMarker.Asset} (iOS {UnoLibrary2.PlatformMarker.IOSOnlyApi()}) XAML={multiTargeted}; "
            + $"plain: C#={UnoPlainLibrary.PlatformMarker.Asset} runtime={UnoPlainLibrary.PlatformMarker.RuntimePlatform} XAML={plain}";
        var expected = UnoLibrary2.PlatformMarker.Asset == "ios" && multiTargeted == "ios,not_android"
            && UnoPlainLibrary.PlatformMarker.Asset == "generic" && UnoPlainLibrary.PlatformMarker.RuntimePlatform == "ios" && plain == "not_android,not_ios";
#else
        var actual = $"multi-targeted: C#={UnoLibrary2.PlatformMarker.Asset} XAML={multiTargeted}; plain: C#={UnoPlainLibrary.PlatformMarker.Asset} XAML={plain}";
        var expected = true;
#endif

        return $"{(expected ? "PASS" : "FAIL")} {actual}";
    }
}
