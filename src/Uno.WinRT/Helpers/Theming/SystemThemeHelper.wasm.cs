#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;

using NativeMethods = __Uno.Helpers.Theming.SystemThemeHelper.NativeMethods;

namespace Uno.Helpers.Theming;

internal static partial class SystemThemeHelper
{
	internal static SystemTheme GetSystemTheme()
	{
		var serializedTheme = NativeMethods.GetSystemTheme();

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
		NativeMethods.ObserveSystemTheme();
	}

	[JSExport]
	public static int DispatchSystemThemeChange()
	{
		RefreshSystemTheme();
		return 0;
	}
}
