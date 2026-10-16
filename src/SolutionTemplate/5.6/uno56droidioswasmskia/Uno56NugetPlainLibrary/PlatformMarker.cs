namespace UnoPlainLibrary;

// A plain netX.0 asset carries no platform symbol; platform checks have to happen at runtime.
public static class PlatformMarker
{
#if __ANDROID__
    public static string Asset => "android";
#elif __IOS__
    public static string Asset => "ios";
#elif __WASM__
    public static string Asset => "wasm";
#elif __DESKTOP__
    public static string Asset => "desktop";
#elif WINDOWS
    public static string Asset => "win";
#else
    public static string Asset => "generic";
#endif

    public static string RuntimePlatform =>
        OperatingSystem.IsAndroid() ? "android"
        : OperatingSystem.IsIOS() ? "ios"
        : OperatingSystem.IsBrowser() ? "browser"
        : "other";
}
