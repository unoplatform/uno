namespace Uno.UI.Helpers;

internal static class UnoRuntimePlatformExtensions
{
    public static bool IsSkia(UnoRuntimePlatform platform)
    {
        return platform == UnoRuntimePlatform.SkiaFrameBuffer
                           || platform == UnoRuntimePlatform.SkiaGtk
                           || platform == UnoRuntimePlatform.SkiaMacOS
                           || platform == UnoRuntimePlatform.SkiaWpf
                           || platform == UnoRuntimePlatform.SkiaX11;
    }

    public static bool IsAndroid(this UnoRuntimePlatform platform)
    {
        return platform == UnoRuntimePlatform.Android;
    }

    public static bool IsiOS(this UnoRuntimePlatform platform)
    {
        return platform == UnoRuntimePlatform.iOS;
    }

    public static bool IsMacCatalyst(this UnoRuntimePlatform platform)
    {
        return platform == UnoRuntimePlatform.MacCatalyst;
    }

    public static bool IsMacOS(this UnoRuntimePlatform platform)
    {
        return platform == UnoRuntimePlatform.MacOSX;
    }

    public static bool IsWebAssembly(this UnoRuntimePlatform platform)
    {
        return platform == UnoRuntimePlatform.WebAssembly;
    }
}