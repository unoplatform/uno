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
				var uiMode = Android.App.Application.Context.Resources?.Configuration?.UiMode;
				if (uiMode != null)
				{
					var uiModeFlags = uiMode & UiMode.NightMask;
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
