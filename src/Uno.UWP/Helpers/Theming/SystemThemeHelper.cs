#nullable enable

using System;
using Windows.ApplicationModel.Core;

namespace Uno.Helpers.Theming
{
	internal static partial class SystemThemeHelper
	{
		private static bool _changesObserved = false;
		private static SystemTheme? _lastSystemTheme;

		internal static SystemTheme SystemTheme
		{
			get
			{
				_lastSystemTheme = GetSystemTheme();
				return _lastSystemTheme.Value;
			}
		}

		internal static event EventHandler? SystemThemeChanged;

		internal static void OnSystemThemeChanged() => SystemThemeChanged?.Invoke(null, EventArgs.Empty);

		internal static void ObserveThemeChanges()
		{
			if (!_changesObserved)
			{
				// Cache the initial theme for future comparisons
				_lastSystemTheme = GetSystemTheme();

				ObserveThemeChangesPartial();

				// Ensure to check for theme changes when app leaves background
				CoreApplication.LeavingBackground += (s, e) => RefreshSystemTheme();

				_changesObserved = true;
			}
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
				OnSystemThemeChanged();
			}
		}

		static partial void ObserveThemeChangesPartial();

#if NET461 || __NETSTD_REFERENCE__
		private static SystemTheme GetSystemTheme() => SystemTheme.Light;
#endif
	}
}
