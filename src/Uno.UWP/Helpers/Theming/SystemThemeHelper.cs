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
	private static bool _changesObserved;
	private static SystemTheme? _lastSystemTheme;

	/// <summary>
	/// Gets the current system theme.
	/// </summary>
	internal static SystemTheme SystemTheme => _lastSystemTheme ??= GetSystemTheme();

	/// <summary>
	/// Triggered when system theme changes.
	/// </summary>
	internal static event EventHandler? SystemThemeChanged;

	/// <summary>
	/// Starts observing system theme changes.
	/// </summary>
	internal static void ObserveThemeChanges()
	{
		if (!_changesObserved)
		{
			// Cache the initial theme for future comparisons
			_lastSystemTheme = GetSystemTheme();

			ObserveThemeChangesPartial();

			// Ensure to check for theme changes when app leaves background
			// as we might miss a theme change if the app gets suspended.
			CoreApplication.LeavingBackground += (s, e) => RefreshSystemTheme();
			CoreApplication.Resuming += (s, e) => RefreshSystemTheme();
			CoreWindow.Main!.Activated += (s, e) => RefreshSystemTheme();
			CoreWindow.Main!.VisibilityChanged += (s, e) => RefreshSystemTheme();

			_changesObserved = true;
		}
	}

	/// <summary>
	/// Raises SystemThemeChanged if the theme
	/// has changed since we last checked it.
	/// </summary>
	private static void RefreshSystemTheme()
	{
		var cachedTheme = _lastSystemTheme;
		var currentTheme = GetSystemTheme();
		if (cachedTheme != currentTheme)
		{
			_lastSystemTheme = currentTheme;
			RaiseSystemThemeChanged();
		}
	}

	static partial void ObserveThemeChangesPlatform();

	private static void RaiseSystemThemeChanged() => SystemThemeChanged?.Invoke(null, EventArgs.Empty);
}
