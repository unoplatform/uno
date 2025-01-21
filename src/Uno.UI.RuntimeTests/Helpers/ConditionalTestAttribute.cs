using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

[Flags]
public enum RuntimeTestPlatform
{
	NativeWasm = 1 << 0,
	NativeAndroid = 1 << 1,
	NativeIOS = 1 << 2,
	NativeMacCatalyst = 1 << 3,
	NativeTvOS = 1 << 4,
	SkiaGtk = 1 << 5,
	SkiaWpf = 1 << 6,
	SkiaX11 = 1 << 7,
	SkiaMacOS = 1 << 8,
	SkiaWasm = 1 << 9,
	SkiaIslands = 1 << 10,
	SkiaAndroid = 1 << 11,
	SkiaIOS = 1 << 12,
	SkiaTvOS = 1 << 13,
	SkiaMacCatalyst = 1 << 14,
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public partial class ConditionalTestAttribute : TestMethodAttribute
{
	private static readonly RuntimeTestPlatform _currentPlatform;

	public const RuntimeTestPlatform SkiaUIKit = RuntimeTestPlatform.SkiaIOS | RuntimeTestPlatform.SkiaTvOS | RuntimeTestPlatform.SkiaMacCatalyst;
	public const RuntimeTestPlatform SkiaMobile = RuntimeTestPlatform.SkiaAndroid | SkiaUIKit;
	public const RuntimeTestPlatform SkiaDesktop = RuntimeTestPlatform.SkiaGtk | RuntimeTestPlatform.SkiaWpf | RuntimeTestPlatform.SkiaX11 | RuntimeTestPlatform.SkiaMacOS | RuntimeTestPlatform.SkiaIslands;
	public const RuntimeTestPlatform Skia = SkiaDesktop | RuntimeTestPlatform.SkiaWasm | SkiaMobile;

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
			RuntimeTestPlatform.NativeWasm => IsNativeWasm(),
			RuntimeTestPlatform.NativeAndroid => IsNativeAndroid(),
			RuntimeTestPlatform.NativeIOS => IsNativeIOS(),
			RuntimeTestPlatform.NativeMacCatalyst => IsNativeMacCatalyst(),
			RuntimeTestPlatform.NativeTvOS => IsNativetvOS(),
			RuntimeTestPlatform.SkiaGtk => IsSkiaGtk(),
			RuntimeTestPlatform.SkiaWpf => IsSkiaWpf(),
			RuntimeTestPlatform.SkiaX11 => IsSkiaX11(),
			RuntimeTestPlatform.SkiaMacOS => IsSkiaMacOS(),
			RuntimeTestPlatform.SkiaIslands => IsSkiaIslands(),
			RuntimeTestPlatform.SkiaWasm => IsSkia() && OperatingSystem.IsBrowser(),
			RuntimeTestPlatform.SkiaAndroid => IsSkia() && OperatingSystem.IsAndroid(),
			RuntimeTestPlatform.SkiaIOS => IsSkia() && OperatingSystem.IsIOS(),
			RuntimeTestPlatform.SkiaTvOS => IsSkia() && OperatingSystem.IsTvOS(),
			RuntimeTestPlatform.SkiaMacCatalyst => IsSkia() && OperatingSystem.IsMacCatalyst(),
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
