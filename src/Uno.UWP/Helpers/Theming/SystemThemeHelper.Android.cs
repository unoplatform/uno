#nullable enable

using Android.Content.Res;
using Android.OS;

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		private static SystemTheme GetSystemTheme()
		{
			if ((int)Build.VERSION.SdkInt >= 28)
			{
				var configuration = Android.App.Application.Context?.Resources?.Configuration;
				if (configuration != null)
				{
					var uiModeFlags = configuration.UiMode & UiMode.NightMask;
					if (uiModeFlags == UiMode.NightYes)
					{
						return SystemTheme.Dark;
					}
				}
			}
			return SystemTheme.Light;
		}
	}
}
