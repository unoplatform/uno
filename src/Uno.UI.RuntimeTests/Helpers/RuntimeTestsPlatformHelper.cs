using System;
using System.Linq;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

internal static class RuntimeTestsPlatformHelper
{
	private static readonly Lazy<RuntimeTestPlatforms> _currentPlatform = new Lazy<RuntimeTestPlatforms>(GetCurrentPlatform);

	/// <summary>
	/// Returns the current runtime test platform.
	/// </summary>
	public static RuntimeTestPlatforms CurrentPlatform => _currentPlatform.Value;

	private static RuntimeTestPlatforms GetCurrentPlatform()
	{
		var values = Enum.GetValues<RuntimeTestPlatforms>();
		var currentPlatform = default(RuntimeTestPlatforms);
		var counter = 0;
		foreach (var value in values.Where(HasSingleFlag))
		{
			if (IsCurrentTarget(value))
			{
				currentPlatform |= value;
				counter++;
			}
		}

		if (counter == 0)
		{
			throw new InvalidOperationException("Unrecognized runtime platform.");
		}

		if (counter > 1)
		{
			throw new InvalidOperationException($"Multiple runtime platforms detected ({currentPlatform:g})");
		}

		return currentPlatform;
	}

	private static bool HasSingleFlag(RuntimeTestPlatforms value)
	{
		var numericValue = Convert.ToInt64(value);

		// Check if exactly one bit is set (i.e., power of two)
		return numericValue != 0 && (numericValue & (numericValue - 1)) == 0;
	}

	private static bool IsCurrentTarget(RuntimeTestPlatforms singlePlatform)
	{
		return singlePlatform switch
		{
			RuntimeTestPlatforms.NativeWinUI => IsWinUI(),
			RuntimeTestPlatforms.NativeWasm => IsNativeWasm(),
			RuntimeTestPlatforms.NativeAndroid => IsNativeAndroid(),
			RuntimeTestPlatforms.NativeIOS => IsNativeIOS(),
			RuntimeTestPlatforms.NativeMacCatalyst => IsNativeMacCatalyst(),
			RuntimeTestPlatforms.NativeTvOS => IsNativetvOS(),
			RuntimeTestPlatforms.SkiaWpf => IsSkia() && IsSkiaWpf(),
			RuntimeTestPlatforms.SkiaWin32 => IsSkia() && IsSkiaWin32(),
			RuntimeTestPlatforms.SkiaX11 => IsSkia() && IsSkiaX11(),
			RuntimeTestPlatforms.SkiaMacOS => IsSkia() && IsSkiaMacOS(),
			RuntimeTestPlatforms.SkiaIslands => IsSkia() && IsSkiaIslands(),
			RuntimeTestPlatforms.SkiaFrameBuffer => IsSkia() && IsSkiaFrameBuffer()
			RuntimeTestPlatforms.SkiaWasm => IsSkia() && OperatingSystem.IsBrowser(),
			RuntimeTestPlatforms.SkiaAndroid => IsSkia() && OperatingSystem.IsAndroid(),
			RuntimeTestPlatforms.SkiaIOS => IsSkia() && OperatingSystem.IsIOS(),
			RuntimeTestPlatforms.SkiaTvOS => IsSkia() && OperatingSystem.IsTvOS(),
			RuntimeTestPlatforms.SkiaMacCatalyst => IsSkia() && OperatingSystem.IsMacCatalyst(),
			RuntimeTestPlatforms.SkiaFrameBuffer => IsSkia() && IsSkiaFrameBuffer(),
			_ => throw new ArgumentException(nameof(singlePlatform)),
		};
	}

	private static bool IsSkiaHostAssembly(string name)
#if __SKIA__
		=> Microsoft.UI.Xaml.Application.Current.Host?.GetType().Assembly.GetName().Name == name;
#else
		=> false;
#endif

	private static bool IsSkia() =>
#if __SKIA__
		true;
#else
		false;
#endif

	private static bool IsWinUI() =>
#if WINAPPSDK
		true;
#else
		false;
#endif

	private static bool IsSkiaWpf()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Wpf");

	private static bool IsSkiaWin32()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Win32");

	private static bool IsSkiaX11()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.X11");

	private static bool IsSkiaFrameBuffer()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Linux.FrameBuffer");

	private static bool IsSkiaMacOS()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.MacOS");

	private static bool IsSkiaBrowser()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.WebAssembly.Browser");

	private static bool IsSkiaFrameBuffer()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.WebAssembly.Browser");

	private static bool IsSkiaIslands()
#if __SKIA__
		=> Microsoft.UI.Xaml.Application.Current.Host is null;
#else
		=> false;
#endif

	private static bool IsNativeWasm()
	{
#if __WASM__
		return true;
#else
		return false;
#endif
	}

	private static bool IsNativeAndroid()
	{
#if __ANDROID__
		return true;
#else
		return false;
#endif
	}

	private static bool IsNativeIOS()
	{
#if __IOS__
		return true;
#else
		return false;
#endif
	}

	private static bool IsNativetvOS()
	{
#if __TVOS__
		return true;
#else
		return false;
#endif
	}

	private static bool IsNativeMacCatalyst()
	{
#if __MACCATALYST__
		return true;
#else
		return false;
#endif
	}
}
