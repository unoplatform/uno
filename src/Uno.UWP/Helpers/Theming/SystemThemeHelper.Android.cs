namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		private static SystemTheme GetSystemTheme()
		{
			if ((int)Build.VERSION.SdkInt >= 28)
			{
				var uiModeFlags = Android.App.Application.Context.Resources.Configuration.UiMode & UiMode.NightMask;
				if (uiModeFlags == UiMode.NightYes)
				{
					return ApplicationTheme.Dark;
				}
			}
			return ApplicationTheme.Light;
		}
	}
}
