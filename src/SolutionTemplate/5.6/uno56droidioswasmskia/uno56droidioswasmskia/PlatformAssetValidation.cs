namespace uno56droidioswasmskia;

// Reports which asset of each package the head runs, and whether its platform-specific C# and XAML
// resolved as expected: the multi-targeted package must use this head's platform asset, the plain one
// its netX.0 asset.
internal static class PlatformAssetValidation
{
#if __ANDROID__
    private const string Platform = "android";
    private const string RuntimePlatform = "android";
#elif __IOS__
    private const string Platform = "ios";
    private const string RuntimePlatform = "ios";
#elif __WASM__
    private const string Platform = "wasm";
    private const string RuntimePlatform = "browser";
#elif __DESKTOP__
    private const string Platform = "desktop";
    private const string RuntimePlatform = "other";
#elif WINDOWS
    private const string Platform = "win";
    private const string RuntimePlatform = "other";
#endif

    private static readonly string[] _platforms = ["android", "ios", "wasm", "desktop", "win"];

    public static string Run()
    {
        var multiTargeted = new UnoLibrary2.PlatformMarkerControl().XamlMarkers;
        var actual = $"multi-targeted: C#={UnoLibrary2.PlatformMarker.Asset}{PlatformOnlyApi()} XAML={multiTargeted}";
        var expected = UnoLibrary2.PlatformMarker.Asset == Platform && multiTargeted == ExpectedXaml(Platform);

#if WINDOWS
        // A plain netX.0 Uno library is built against Uno.UI, so the WinAppSDK head does not reference it.
        actual += "; plain: n/a";
#else
        var plain = new UnoPlainLibrary.PlatformMarkerControl().XamlMarkers;
        actual += $"; plain: C#={UnoPlainLibrary.PlatformMarker.Asset} runtime={UnoPlainLibrary.PlatformMarker.RuntimePlatform} XAML={plain}";
        expected &= UnoPlainLibrary.PlatformMarker.Asset == "generic"
            && UnoPlainLibrary.PlatformMarker.RuntimePlatform == RuntimePlatform
            && plain == ExpectedXaml(null);
#endif

        return $"{(expected ? "PASS" : "FAIL")} [{Platform}] {actual}";
    }

    // Only compiles when the platform-specific asset of Uno56NugetLibrary is the compile reference.
    private static string PlatformOnlyApi()
    {
#if __ANDROID__
        return $" (API {UnoLibrary2.PlatformMarker.AndroidOnlyApi()})";
#elif __IOS__
        return $" (iOS {UnoLibrary2.PlatformMarker.IOSOnlyApi()})";
#else
        return "";
#endif
    }

    // The positive prefix of the asset's own platform, plus every negated prefix except that platform's.
    private static string ExpectedXaml(string? platform)
    {
        string[] positive = platform is null ? [] : [platform];
        var negated = _platforms.Where(p => p != platform).Select(p => $"not_{p}");
        return string.Join(",", positive.Concat(negated));
    }
}
