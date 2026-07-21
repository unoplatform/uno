#nullable enable

using System;
using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.OS;
using Android.Provider;
using Uno.Foundation.Logging;

namespace Uno.Helpers.Theming;

internal static partial class SystemThemeHelper
{
	private const string HighTextContrastSettingName = "high_text_contrast_enabled";

	private static readonly object _highContrastObservationGate = new();
	private static SettingsContentObserver? _highContrastContentObserver;

	internal static SystemTheme GetSystemTheme()
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

	static partial void ObserveThemeChangesPlatform()
	{
		var context = Android.App.Application.Context;
		var contentResolver = context?.ContentResolver;
		if (contentResolver is null)
		{
			typeof(SystemThemeHelper).LogDebug()?.Debug("Unable to observe high contrast because ContentResolver is unavailable.");
			return;
		}

		lock (_highContrastObservationGate)
		{
			if (_highContrastContentObserver is not null)
			{
				return;
			}

			var mainLooper = Looper.MainLooper;
			if (mainLooper is null)
			{
				typeof(SystemThemeHelper).LogDebug()?.Debug("Unable to observe high contrast because the main Looper is unavailable.");
				return;
			}

			if (Settings.Secure.GetUriFor(HighTextContrastSettingName) is { } highContrastUri)
			{
				_highContrastContentObserver = new SettingsContentObserver(
					new Handler(mainLooper),
					RefreshHighContrast);
				contentResolver.RegisterContentObserver(
					highContrastUri,
					true,
					_highContrastContentObserver);
			}
			else
			{
				typeof(SystemThemeHelper).LogDebug()?.Debug($"Unable to resolve the '{HighTextContrastSettingName}' settings URI.");
			}
		}
	}

	static partial void GetIsHighContrastEnabledPlatform(ref bool result)
	{
		result = IsHighContrastTextEnabled();
	}

	static partial void GetHighContrastSchemeNamePlatform(ref string result)
	{
		if (IsHighContrastTextEnabled())
		{
			result = GetMobileHighContrastSchemeName(GetSystemTheme());
		}
	}

	static partial void GetHighContrastSystemColorsPlatform(ref HighContrastSystemColors? result)
	{
		if (IsHighContrastTextEnabled())
		{
			result = GetMobileHighContrastSystemColors(GetSystemTheme());
		}
	}

	private static bool IsHighContrastTextEnabled()
	{
		var contentResolver = Android.App.Application.Context?.ContentResolver;
		return contentResolver is not null
			&& Settings.Secure.GetInt(contentResolver, HighTextContrastSettingName, 0) == 1;
	}

	private sealed class SettingsContentObserver : ContentObserver
	{
		private readonly Action _onChanged;

		public SettingsContentObserver(Handler handler, Action onChanged)
			: base(handler)
		{
			_onChanged = onChanged;
		}

		public override bool DeliverSelfNotifications() => true;

		public override void OnChange(bool selfChange)
		{
			base.OnChange(selfChange);
			try
			{
				_onChanged();
			}
			catch (Exception e)
			{
				typeof(SystemThemeHelper).LogError()?.Error("High contrast settings observer failed.", e);
			}
		}
	}
}
