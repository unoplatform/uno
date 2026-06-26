namespace Uno.Helpers;

internal static class RuntimePlatformHelper
{
	/// <summary>
	/// Set by the active Skia host (in its constructor) to the specific Skia platform it runs.
	/// </summary>
	internal static RuntimePlatform SkiaPlatform { get; set; } = RuntimePlatform.Unknown;

	internal static RuntimePlatform Current => GetPlatform();

	private static RuntimePlatform GetPlatform()
	{
		// A Skia host registers the actual runtime platform here. This takes precedence so that
		// Skia targets (incl. Skia on Android/Apple/WASM) are reported correctly regardless of the
		// compile-time symbols of the underlying Uno assembly.
		if (SkiaPlatform != RuntimePlatform.Unknown)
		{
			return SkiaPlatform;
		}

#if __ANDROID__
		return RuntimePlatform.NativeAndroid;
#elif __APPLE_UIKIT__
		return global::System.OperatingSystem.IsMacCatalyst()
			? RuntimePlatform.NativeMacCatalyst
			: global::System.OperatingSystem.IsTvOS()
				? RuntimePlatform.NativeTvOS
				: RuntimePlatform.NativeIOS;
#elif __WASM__
		return RuntimePlatform.NativeWasm;
#else
		return RuntimePlatform.Unknown;
#endif
	}
}
