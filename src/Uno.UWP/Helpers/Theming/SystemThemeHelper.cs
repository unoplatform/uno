#nullable enable

using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Uno.Helpers.Theming;

/// <summary>
/// Provides access to current OS theme
/// and notifications about its changes.
/// </summary>
internal static partial class SystemThemeHelper
{
	private static EventHandler? _systemThemeChanged;
	private static bool _changesObserved;
	private static SystemTheme? _lastSystemTheme;

	/// <summary>
	/// Gets the current system theme.
	/// </summary>
	internal static SystemTheme SystemTheme => _lastSystemTheme ??= GetSystemTheme();

	/// <summary>
	/// Triggered when system theme changes.
	/// </summary>
	internal static event EventHandler? SystemThemeChanged
	{
		add
		{
			_systemThemeChanged += value;
			ObserveThemeChanges();
		}
		remove => _systemThemeChanged -= value;
	}

	/// <summary>
	/// Starts observing system theme changes.
	/// </summary>
	internal static void ObserveThemeChanges()
	{
		if (_changesObserved)
		{
			return;
		}

		_changesObserved = true;

		// Cache the initial theme for future comparisons
		_lastSystemTheme = GetSystemTheme();

		ObserveThemeChangesPlatform();

		// Ensure to check for theme changes when app leaves background
		// as we might miss a theme change if the app gets suspended.
		CoreApplication.LeavingBackground += (s, e) => RefreshSystemTheme();
		CoreApplication.Resuming += (s, e) => RefreshSystemTheme();
	}

	/// <summary>
	/// Raises SystemThemeChanged if the theme
	/// has changed since we last checked it.
	/// </summary>
	internal static void RefreshSystemTheme()
	{
		var cachedTheme = _lastSystemTheme;
		var currentTheme = GetSystemTheme();
		if (cachedTheme != currentTheme)
		{
			_lastSystemTheme = currentTheme;
			RaiseSystemThemeChanged();
		}
	}

	/// <summary>
	/// This is just a partial method, as on some platforms (e.g. Android and iOS)
	/// the theme change is listened to on the Application/Window level.
	/// </summary>
	static partial void ObserveThemeChangesPlatform();

	private static void RaiseSystemThemeChanged() => _systemThemeChanged?.Invoke(null, EventArgs.Empty);
}
