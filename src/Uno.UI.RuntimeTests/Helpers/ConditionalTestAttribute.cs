using System;
using System.Linq;
using Private.Infrastructure;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

[Flags]
public enum RuntimeTestPlatform
{
	SkiaGtk = 1 << 0,
	SkiaWpf = 1 << 1,
	SkiaX11 = 1 << 2,
	SkiaMacOS = 1 << 3,
	SkiaIslands = 1 << 4,
	Wasm = 1 << 5,
	Android = 1 << 6,
	iOS = 1 << 7,
	macOSCatalyst = 1 << 8,
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed partial class ConditionalTestAttribute : TestMethodAttribute
{
	private static readonly RuntimeTestPlatform _currentPlatform;

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
			RuntimeTestPlatform.SkiaIslands => IsSkiaIslands(),
			RuntimeTestPlatform.Wasm => IsWasm(),
			RuntimeTestPlatform.Android => IsAndroid(),
			RuntimeTestPlatform.iOS => IsIOS(),
			RuntimeTestPlatform.macOSCatalyst => IsMacOSCatalyst(),
			_ => throw new ArgumentException(nameof(singlePlatform)),
		};
	}

	private static bool IsSkiaHostAssembly(string name)
#if __SKIA__
		=> Windows.UI.Xaml.Application.Current.Host?.GetType().Assembly.GetName().Name == name;
#else
		=> false;
#endif

	private static bool IsSkiaGtk()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Gtk");

	private static bool IsSkiaWpf()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.Wpf");

	private static bool IsSkiaX11()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.X11");

	private static bool IsSkiaMacOS()
		=> IsSkiaHostAssembly("Uno.UI.Runtime.Skia.MacOS");

	private static bool IsSkiaIslands()
#if __SKIA__
		=> Windows.UI.Xaml.Application.Current.Host is null;
#else
		=> false;
#endif

	private static bool IsWasm()
	{
#if __WASM__
		return true;
#else
		return false;
#endif
	}

	private static bool IsAndroid()
	{
#if __ANDROID__
		return true;
#else
		return false;
#endif
	}

	private static bool IsIOS()
	{
#if __IOS__
		return true;
#else
		return false;
#endif
	}

	private static bool IsMacOSCatalyst()
	{
#if __MACOS__
		return true;
#else
		return false;
#endif
	}
}
