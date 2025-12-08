namespace Uno.Helpers;

internal static class RuntimePlatformHelper
{
	internal static UnoRuntimePlatform SkiaPlatform { get; set; } = UnoRuntimePlatform.Unknown;

	internal static UnoRuntimePlatform Current => GetPlatform();

	private static UnoRuntimePlatform GetPlatform() =>
#if __ANDROID__
		UnoRuntimePlatform.NativeAndroid;
#elif __IOS__
		UnoRuntimePlatform.NativeIOS;
#elif __MACCATALYST__
		UnoRuntimePlatform.NativeMacCatalyst;
#elif __WASM__
		UnoRuntimePlatform.NativeWasm;
#elif __SKIA__
		SkiaPlatform;
#else
		UnoRuntimePlatform.Unknown;
#endif
}
