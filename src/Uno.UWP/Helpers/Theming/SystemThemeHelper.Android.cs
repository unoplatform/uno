#nullable enable

using System;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Views.Accessibility;
using Java.Util.Concurrent;

namespace Uno.Helpers.Theming;

internal static partial class SystemThemeHelper
{
	private const string HighTextContrastSettingName = "high_text_contrast_enabled";

	private static readonly object _highContrastObservationGate = new();
	private static SettingsContentObserver? _highContrastContentObserver;
	private static AccessibilityHighContrastTextStateChangeListener? _highContrastTextStateChangeListener;
	private static bool _isHighContrastObserved;

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
		var context = Application.Context;
		if (context is null)
		{
			return;
		}

		lock (_highContrastObservationGate)
		{
			if (_isHighContrastObserved)
			{
				return;
			}

			_isHighContrastObserved = true;

			if ((int)Build.VERSION.SdkInt >= 36
				&& context.GetSystemService(Context.AccessibilityService) is AccessibilityManager accessibilityManager
				&& context.MainExecutor is IExecutor executor)
			{
				_highContrastTextStateChangeListener ??= new AccessibilityHighContrastTextStateChangeListener(_ => RefreshHighContrast());
				accessibilityManager.AddHighContrastTextStateChangeListener(executor, _highContrastTextStateChangeListener);
			}

			if (Settings.Secure.GetUriFor(HighTextContrastSettingName) is { } highTextContrastUri
				&& context.ContentResolver is { } contentResolver)
			{
				_highContrastContentObserver ??= new SettingsContentObserver(new Handler(Looper.MainLooper!), RefreshHighContrast);
				contentResolver.RegisterContentObserver(highTextContrastUri, true, _highContrastContentObserver);
			}
		}
	}

	static partial void GetIsHighContrastEnabledPlatform(ref bool result)
	{
		result = IsAndroidHighContrastEnabled();
	}

	static partial void GetHighContrastSchemeNamePlatform(ref string result)
	{
		if (IsAndroidHighContrastEnabled())
		{
			result = GetMobileHighContrastSchemeName(GetSystemTheme());
		}
	}

	static partial void GetHighContrastSystemColorsPlatform(ref HighContrastSystemColors? result)
	{
		if (IsAndroidHighContrastEnabled())
		{
			result = GetMobileHighContrastSystemColors(GetSystemTheme());
		}
	}

	private static bool IsAndroidHighContrastEnabled()
	{
		var context = Application.Context;
		if (context is null)
		{
			return false;
		}

		if ((int)Build.VERSION.SdkInt >= 36
			&& context.GetSystemService(Context.AccessibilityService) is AccessibilityManager accessibilityManager)
		{
			return accessibilityManager.IsHighContrastTextEnabled;
		}

		return Settings.Secure.GetInt(context.ContentResolver, HighTextContrastSettingName, 0) == 1;
	}

	private sealed class SettingsContentObserver : ContentObserver
	{
		private readonly System.Action _onChanged;

		public SettingsContentObserver(Handler handler, System.Action onChanged)
			: base(handler)
		{
			_onChanged = onChanged;
		}

		public override bool DeliverSelfNotifications() => true;

		public override void OnChange(bool selfChange)
		{
			base.OnChange(selfChange);
			_onChanged();
		}
	}

	private sealed class AccessibilityHighContrastTextStateChangeListener : Java.Lang.Object, AccessibilityManager.IHighContrastTextStateChangeListener
	{
		private readonly Action<bool> _onChanged;

		public AccessibilityHighContrastTextStateChangeListener(Action<bool> onChanged)
		{
			_onChanged = onChanged;
		}

		public void OnHighContrastTextStateChanged(bool enabled)
		{
			_onChanged(enabled);
		}
	}
}
