namespace Uno.UI.Helpers;

internal static class UnoRuntimePlatformExtensions
{
	public static bool IsSkia(UnoRuntimePlatform platform)
	{
		return platform == UnoRuntimePlatform.SkiaFrameBuffer
						   || platform == UnoRuntimePlatform.SkiaGtk
						   || platform == UnoRuntimePlatform.SkiaMacOS
						   || platform == UnoRuntimePlatform.SkiaWpfIslands
						   || platform == UnoRuntimePlatform.SkiaX11;
	}
}
