using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

[Flags]
public enum RuntimeTestPlatform
{
	SkiaGtk = 1 << 0,
	SkiaWpf = 1 << 1,
	SkiaX11 = 1 << 2,
	SkiaMacOS = 1 << 3,
	SkiaBrowser = 1 << 4,
	SkiaIslands = 1 << 5,
	Wasm = 1 << 6,
	Android = 1 << 7,
	iOS = 1 << 8,
	MacCatalyst = 1 << 9,
	tvOS = 1 << 10,
	SkiaWasm = 1 << 11,
	SkiaAndroid = 1 << 12,
	SkiaiOS = 1 << 13,
	SkiatvOS = 1 << 14,
	SkiaMacCatalyst = 1 << 15,
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public partial class ConditionalTestAttribute : TestMethodAttribute
{
	private static readonly RuntimeTestPlatform _currentPlatform;

	public const RuntimeTestPlatform SkiaUIKit = RuntimeTestPlatform.SkiaiOS | RuntimeTestPlatform.SkiatvOS | RuntimeTestPlatform.SkiaMacCatalyst;
	public const RuntimeTestPlatform SkiaMobile = RuntimeTestPlatform.SkiaAndroid | SkiaUIKit;
	public const RuntimeTestPlatform SkiaDesktop = RuntimeTestPlatform.SkiaGtk | RuntimeTestPlatform.SkiaWpf | RuntimeTestPlatform.SkiaX11 | RuntimeTestPlatform.SkiaMacOS | RuntimeTestPlatform.SkiaIslands;
	public const RuntimeTestPlatform Skia = SkiaDesktop | RuntimeTestPlatform.SkiaBrowser | SkiaMobile;

	static ConditionalTestAttribute()
	{
		var values = Enum.GetValues<RuntimeTestPlatform>();
		var platform = default(RuntimeTestPlatform);
		var counter = 0;
		foreach (var value in values)
		{
			if (ShouldRun(value))
			{
				platform |= value;
				counter++;
			}
		}

		if (counter != 1)
		{
			throw new InvalidOperationException("One and exactly one platform is expected to be true.");
		}

		_currentPlatform = platform;
	}

	public RuntimeTestPlatform IgnoredPlatforms { get; set; }

	public bool ShouldRun()
		=> !IgnoredPlatforms.HasFlag(_currentPlatform);

	private static bool ShouldRun(RuntimeTestPlatform singlePlatform)
	{
		return singlePlatform switch
		{
			RuntimeTestPlatform.SkiaGtk => IsSkiaGtk(),
			RuntimeTestPlatform.SkiaWpf => IsSkiaWpf(),
			RuntimeTestPlatform.SkiaX11 => IsSkiaX11(),
			RuntimeTestPlatform.SkiaMacOS => IsSkiaMacOS(),
			RuntimeTestPlatform.SkiaBrowser => IsSkiaBrowser(),
			RuntimeTestPlatform.SkiaIslands => IsSkiaIslands(),
			RuntimeTestPlatform.Wasm => IsWasmNative(),
			RuntimeTestPlatform.Android => IsAndroidNative(),
			RuntimeTestPlatform.iOS => IsIOSNative(),
			RuntimeTestPlatform.MacCatalyst => IsMacCatalystNative(),
			RuntimeTestPlatform.tvOS => IsTvOSNative(),
			RuntimeTestPlatform.SkiaAndroid => IsSkia() && OperatingSystem.IsAndroid(),
			RuntimeTestPlatform.SkiaiOS => IsSkia() && OperatingSystem.IsIOS(),
			RuntimeTestPlatform.SkiatvOS => IsSkia() && OperatingSystem.IsTvOS(),
			RuntimeTestPlatform.SkiaMacCatalyst => IsSkia() && OperatingSystem.IsMacCatalyst(),
			RuntimeTestPlatform.SkiaWasm => IsSkia() && OperatingSystem.IsBrowser(),
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

	private static bool IsSkiaGtk()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Gtk");

	private static bool IsSkiaWpf()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Wpf");

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

	private static bool IsWasmNative()
	{
#if __WASM__
		return true;
#else
		return false;
#endif
	}

	private static bool IsAndroidNative()
	{
#if __ANDROID__
		return true;
#else
		return false;
#endif
	}

	private static bool IsIOSNative()
	{
#if __IOS__
		return true;
#else
		return false;
#endif
	}

	private static bool IsTvOSNative()
	{
#if __TVOS__
		return true;
#else
		return false;
#endif
	}

	private static bool IsMacCatalystNative()
	{
#if __MACCATALYST__
		return true;
#else
		return false;
#endif
	}
}
