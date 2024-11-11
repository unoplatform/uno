using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Helpers;

internal static class PlatformRuntimeHelper
{
	internal static UnoRuntimePlatform SkiaPlatform { get; set; } = UnoRuntimePlatform.Skia;

	public static UnoRuntimePlatform Current => GetPlatform();

	private static UnoRuntimePlatform GetPlatform() =>
#if __ANDROID__
		UnoRuntimePlatform.Android;
#elif __IOS__
		UnoRuntimePlatform.iOS;
#elif __MACCATALYST__
		UnoRuntimePlatform.MacCatalyst;
#elif __MACOS__
		UnoRuntimePlatform.MacOSX;
#elif __WASM__
		UnoRuntimePlatform.WebAssembly;
#elif __SKIA__
		SkiaPlatform;
#else
		UnoRuntimePlatform.Unknown;
#endif

}
