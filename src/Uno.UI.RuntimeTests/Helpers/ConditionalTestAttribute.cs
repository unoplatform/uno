using System;
using System.Linq;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

[Flags]
public enum RuntimeTestPlatform
{
	SkiaGtk = 1 << 0,
	SkiaWpf = 1 << 1,
	SkiaX11 = 1 << 2,
	SkiaMacOS = 1 << 3,
	Wasm = 1 << 4,
	Android = 1 << 5,
	iOS = 1 << 6,
	macOSCatalyst = 1 << 7,
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed partial class ConditionalTestAttribute : TestMethodAttribute
{
	private readonly RuntimeTestPlatform _platforms;
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

	public ConditionalTestAttribute(RuntimeTestPlatform platforms)
	{
		_platforms = platforms;
	}

	public bool ShouldRun()
		=> _platforms.HasFlag(_currentPlatform);

	private static bool ShouldRun(RuntimeTestPlatform singlePlatform)
	{
		return singlePlatform switch
		{
			RuntimeTestPlatform.SkiaGtk => IsSkiaGtk(),
			RuntimeTestPlatform.SkiaWpf => IsSkiaWpf(),
			RuntimeTestPlatform.SkiaX11 => IsSkiaX11(),
			RuntimeTestPlatform.SkiaMacOS => IsSkiaMacOS(),
			RuntimeTestPlatform.Wasm => IsWasm(),
			RuntimeTestPlatform.Android => IsAndroid(),
			RuntimeTestPlatform.iOS => IsIOS(),
			RuntimeTestPlatform.macOSCatalyst => IsMacOSCatalyst(),
			_ => throw new ArgumentException(nameof(singlePlatform)),
		};
	}

	private static bool IsHostAssembly(string name)
		=> Microsoft.UI.Xaml.Application.Current.Host.GetType().Assembly.GetName().Name == name;

	private static bool IsSkiaGtk()
		=> IsHostAssembly("Uno.UI.Runtime.Skia.Gtk");

	private static bool IsSkiaWpf()
		=> IsHostAssembly("Uno.UI.Runtime.Skia.Wpf");

	private static bool IsSkiaX11()
		=> IsHostAssembly("Uno.UI.Runtime.Skia.X11");

	private static bool IsSkiaMacOS()
		=> IsHostAssembly("Uno.UI.Runtime.Skia.MacOS");

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
