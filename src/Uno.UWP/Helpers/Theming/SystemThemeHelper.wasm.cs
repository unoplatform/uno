#nullable enable

using System;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		private const string BaseUIXamlNamespace =
#if HAS_UNO_WINUI
			"Microsoft.UI.Xaml";
#else
			"Windows.UI.Xaml";
#endif

        private static SystemTheme GetSystemTheme()
		{
			var serializedTheme = WebAssemblyRuntime.InvokeJS(BaseUIXamlNamespace + ".Application.getDefaultSystemTheme()");

			if (serializedTheme != null)
			{
				if (Enum.TryParse(serializedTheme, out SystemTheme theme))
				{
					if (typeof(SystemThemeHelper).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
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
			if (typeof(SystemThemeHelper).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
			{
				typeof(SystemThemeHelper).Log().Info("No preferred theme, using Light instead");
			}
			return SystemTheme.Light;
		}
	}
}
