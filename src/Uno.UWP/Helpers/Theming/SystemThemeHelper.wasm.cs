#nullable enable

using System;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;

#if NET7_0_OR_GREATER
using NativeMethods = __Uno.Helpers.Theming.SystemThemeHelper.NativeMethods;
#endif

namespace Uno.Helpers.Theming;

internal static partial class SystemThemeHelper
{
	private static SystemTheme GetSystemTheme()
	{
#if NET7_0_OR_GREATER
		var serializedTheme = NativeMethods.GetSystemTheme();
#else
		var serializedTheme = WebAssemblyRuntime.InvokeJS("Uno.Helpers.Theming.SystemThemeHelper.getSystemTheme()");
#endif

		if (serializedTheme != null)
		{
			if (Enum.TryParse(serializedTheme, true, out SystemTheme theme))
			{
				if (typeof(SystemThemeHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Information))
				{
					typeof(SystemThemeHelper).Log().Info("Setting OS preferred theme: " + theme);
				}
				return theme;
			}
			else
			{
				throw new InvalidOperationException($"{serializedTheme} theme is not a supported OS theme");
			}
		}

		//OS has no preference or API not implemented, use light as default
		if (typeof(SystemThemeHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Information))
		{
			typeof(SystemThemeHelper).Log().Info("No preferred theme, using Light instead");
		}
		return SystemTheme.Light;
	}

	static partial void ObserveThemeChangesPlatform()
	{
#if NET7_0_OR_GREATER
		NativeMethods.ObserveSystemTheme();
#else
		WebAssemblyRuntime.InvokeJS("Uno.Helpers.Theming.SystemThemeHelper.observeSystemTheme()");
#endif
	}

	public static int DispatchSystemThemeChange()
	{
		RefreshSystemTheme();
		return 0;
	}
}
