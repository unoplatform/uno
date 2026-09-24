namespace UnoPlainLibrary;

// A plain netX.0 asset carries no platform symbol; platform checks have to happen at runtime.
public static class PlatformMarker
{
#if __ANDROID__
    public static string Asset => "android";
#elif __IOS__
    public static string Asset => "ios";
#else
    public static string Asset => "generic";
#endif

    public static string RuntimePlatform =>
        OperatingSystem.IsAndroid() ? "android"
        : OperatingSystem.IsIOS() ? "ios"
        : "other";
}
