namespace UnoLibrary2;

// Lets the consuming app prove which TFM asset of this package it compiled against and runs.
public static class PlatformMarker
{
#if __ANDROID__
    public static string Asset => "android";

    public static Android.OS.BuildVersionCodes AndroidOnlyApi() => Android.OS.Build.VERSION.SdkInt;
#elif __IOS__
    public static string Asset => "ios";

    public static string IOSOnlyApi() => UIKit.UIDevice.CurrentDevice.SystemVersion;
#elif __WASM__
    public static string Asset => "wasm";
#elif __DESKTOP__
    public static string Asset => "desktop";
#elif WINDOWS
    public static string Asset => "win";
#else
    public static string Asset => "generic";
#endif
}
