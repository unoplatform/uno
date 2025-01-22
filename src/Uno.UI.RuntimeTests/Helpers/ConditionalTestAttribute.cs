using System;
using System.Linq;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

public enum RuntimeTestPlatforms
{
	Unknown = 0,

	// Native platforms
	NativeWinUI = 1 << 0,
	NativeWasm = 1 << 1,
	NativeAndroid = 1 << 2,
	NativeIOS = 1 << 3,
	NativeMacCatalyst = 1 << 4,
	NativeTvOS = 1 << 5,

	// Skia platforms
	SkiaGtk = 1 << 6,
	SkiaWpf = 1 << 7,
	SkiaWin32 = 1 << 8,
	SkiaX11 = 1 << 9,
	SkiaMacOS = 1 << 10,
	SkiaIslands = 1 << 11,
	SkiaWasm = 1 << 12,
	SkiaAndroid = 1 << 13,
	SkiaIOS = 1 << 14,
	SkiaMacCatalyst = 1 << 15,
	SkiaTvOS = 1 << 16,

	// Combined platforms
	SkiaUIKit = SkiaIOS | SkiaTvOS | SkiaMacCatalyst,
	SkiaMobile = SkiaAndroid | SkiaUIKit,
	SkiaDesktop = SkiaGtk | SkiaWpf | SkiaX11 | SkiaMacOS | SkiaIslands,
	Skia = SkiaDesktop | SkiaWasm | SkiaMobile,
	Native = NativeWasm | NativeAndroid | NativeIOS | NativeMacCatalyst | NativeTvOS | NativeWinUI,
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public partial class ConditionalTestAttribute : TestMethodAttribute
{
	private static readonly RuntimeTestPlatforms _currentPlatform;

	static ConditionalTestAttribute()
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

		_currentPlatform = currentPlatform;
	}

	private static bool HasSingleFlag(RuntimeTestPlatforms value)
	{
		var numericValue = Convert.ToInt64(value);

		// Check if exactly one bit is set (i.e., power of two)
		return numericValue != 0 && (numericValue & (numericValue - 1)) == 0;
	}

	public RuntimeTestPlatforms IgnoredPlatforms { get; set; }

	public bool ShouldRun()
		=> !IgnoredPlatforms.HasFlag(_currentPlatform);

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
			RuntimeTestPlatforms.SkiaGtk => IsSkia() && IsSkiaGtk(),
			RuntimeTestPlatforms.SkiaWpf => IsSkia() && IsSkiaWpf(),
			RuntimeTestPlatforms.SkiaWin32 => IsSkia() && IsSkiaWin32(),
			RuntimeTestPlatforms.SkiaX11 => IsSkia() && IsSkiaX11(),
			RuntimeTestPlatforms.SkiaMacOS => IsSkia() && IsSkiaMacOS(),
			RuntimeTestPlatforms.SkiaIslands => IsSkia() && IsSkiaIslands(),
			RuntimeTestPlatforms.SkiaWasm => IsSkia() && OperatingSystem.IsBrowser(),
			RuntimeTestPlatforms.SkiaAndroid => IsSkia() && OperatingSystem.IsAndroid(),
			RuntimeTestPlatforms.SkiaIOS => IsSkia() && OperatingSystem.IsIOS(),
			RuntimeTestPlatforms.SkiaTvOS => IsSkia() && OperatingSystem.IsTvOS(),
			RuntimeTestPlatforms.SkiaMacCatalyst => IsSkia() && OperatingSystem.IsMacCatalyst(),
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

	private static bool IsSkiaGtk()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Gtk");

	private static bool IsSkiaWpf()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Wpf");

	private static bool IsSkiaWin32()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Win32");

	private static bool IsSkiaX11()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.X11");

	private static bool IsSkiaMacOS()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.MacOS");

	private static bool IsSkiaBrowser()
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
