using Android.App;
using Android.Views;
using Uno.Foundation.Logging;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Uno.UI.ViewManagement.Helpers;

internal static class StatusBarHelper
{
	public static Color? BackgroundColor
	{
		get
		{
			var activity = Uno.UI.ContextHelper.Current as Android.App.Activity;
			if (activity?.Window is not { } window)
			{
				return Colors.Transparent;
			}

			return Color.FromAndroidInt(activity.Window.StatusBarColor);
		}
		set
		{
			var activity = Uno.UI.ContextHelper.Current as Android.App.Activity;
			activity?.Window?.SetStatusBarColor(value ?? Colors.Transparent);
		}
	}

	public static Color? ForegroundColor
	{
		get
		{
			var foregroundType = GetStatusBarForegroundType();
			switch (foregroundType)
			{
				case StatusBarForegroundType.Light:
					return Colors.White;
				case StatusBarForegroundType.Dark:
					return Colors.Black;
				default:
					return null;
			}
		}
		set
		{
			if (!value.HasValue)
			{
				return;
			}

			var foregroundType = ColorToForegroundType(value.Value);

			SetStatusBarForegroundType(foregroundType);
		}
	}

	internal static StatusBarForegroundType? ForegroundType { get; set; }

	private static StatusBarForegroundType GetStatusBarForegroundType()
	{
		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
		{
			var activity = ContextHelper.Current as Activity;
#pragma warning disable 618
#pragma warning disable CA1422 // Validate platform compatibility
			int uiVisibility = (int)activity.Window.DecorView.SystemUiVisibility;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore 618

			var isForegroundDark = (int)SystemUiFlags.LightStatusBar == (uiVisibility & (int)SystemUiFlags.LightStatusBar);

			return isForegroundDark
				? StatusBarForegroundType.Dark
				: StatusBarForegroundType.Light;
		}
		else
		{
			// The status bar foreground is always light below Android M (API 23)
			return StatusBarForegroundType.Light;
		}
	}


	private static StatusBarForegroundType ColorToForegroundType(Color color)
	{
		// Source: https://en.wikipedia.org/wiki/Luma_(video)
		var y = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;

		return y < 128
			? StatusBarForegroundType.Dark
			: StatusBarForegroundType.Light;
	}


	private static void SetStatusBarForegroundType(StatusBarForegroundType foregroundType)
	{
		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
		{
			ForegroundType = foregroundType;
			StatusBar.GetForCurrentView().UpdateSystemUiVisibility();
		}
		else
		{
			typeof(StatusBarHelper).Log().Warn("The status bar foreground color couldn't be changed. This API is only available starting from Android M (API 23).");
		}
	}
}
