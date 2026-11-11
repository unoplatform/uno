namespace Uno.UI.Helpers;

/// <summary>
/// Provides the platform the app is currently running on.
/// </summary>
public static class RuntimePlatformHelper
{
#if HAS_UNO
	private static UnoRuntimePlatform _hostPlatform;

	/// <summary>
	/// Gets the platform the app is currently running on.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="UnoRuntimePlatform.Unknown"/> until an Uno Platform host is created.
	/// </remarks>
	public static UnoRuntimePlatform Current
	{
		get
		{
			if (_hostPlatform != UnoRuntimePlatform.Unknown)
			{
				return _hostPlatform;
			}

			// Islands run without an Uno Platform host.
			return Microsoft.UI.Xaml.Application.Current is { Host: null }
				? UnoRuntimePlatform.SkiaIslands
				: UnoRuntimePlatform.Unknown;
		}
	}

	internal static void SetHostPlatform(UnoRuntimePlatform platform) => _hostPlatform = platform;
#else
	/// <summary>
	/// Gets the platform the app is currently running on.
	/// </summary>
	public static UnoRuntimePlatform Current => UnoRuntimePlatform.NativeWinUI;
#endif
}
