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
	private static SystemTheme? _systemThemeOverride;

	/// <summary>
	/// Gets the current system theme. When <see cref="SystemThemeOverride"/> is set (for testing),
	/// that value is reported instead of the real OS theme.
	/// </summary>
	/// <remarks>
	/// <see cref="_lastSystemTheme"/> only ever caches the real OS theme; the override is applied
	/// on top of it here, so it never poisons the cache.
	/// </remarks>
	internal static SystemTheme SystemTheme => _systemThemeOverride ?? (_lastSystemTheme ??= GetSystemTheme());

	/// <summary>
	/// Gets whether the OS high-contrast accessibility feature is currently active. High contrast is an
	/// OS/app-global dimension OR-ed onto the Light/Dark base theme (matching WinUI's
	/// <c>FrameworkTheming::GetTheme</c>). Sourced from
	/// <see cref="Uno.WinRTFeatureConfiguration.Accessibility.HighContrast"/>, which is settable, so it also
	/// serves as the override hook for runtime tests. Defaults to <c>false</c>.
	/// </summary>
	internal static bool IsHighContrast => Uno.WinRTFeatureConfiguration.Accessibility.HighContrast;

	/// <summary>
	/// Test hook (analogous to <c>ScaleOverride</c>): when set, <see cref="SystemTheme"/> reports this
	/// value instead of the real OS theme; set to <c>null</c> to restore real OS detection. Assigning
	/// raises <see cref="SystemThemeChanged"/> when the effective theme changes, mirroring a real OS
	/// theme change so observers (e.g. <c>Application.OnSystemThemeChanged</c>) react exactly as in
	/// production. Enables runtime tests to validate behavior under a deterministic ambient OS theme,
	/// and under runtime OS-theme changes, independently of the real machine theme.
	/// </summary>
	internal static SystemTheme? SystemThemeOverride
	{
		get => _systemThemeOverride;
		set
		{
			if (_systemThemeOverride == value)
			{
				return;
			}

			_systemThemeOverride = value;

			if (value is null)
			{
				// Restoring real OS detection: re-poll so the cache reflects any OS change
				// that occurred while the override was active.
				_lastSystemTheme = GetSystemTheme();
			}

			// Setting the override forces the system theme, so notify observers unconditionally — unlike
			// RefreshSystemTheme (which gates on the cached theme for OS polling). The app's current theme
			// can legitimately desync from the effective theme (e.g. after a test sets then restores an
			// explicit app theme), so a conditional raise could skip and leave a stale theme (test flakiness).
			RaiseSystemThemeChanged();
		}
	}

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
	/// Removes event handlers whose target belongs to a non-default ALC.
	/// Called during ALC teardown to release references to inner-app objects.
	/// </summary>
	internal static void ClearNonDefaultAlcHandlers()
	{
		var handler = _systemThemeChanged;
		if (handler is null)
		{
			return;
		}

		foreach (var d in handler.GetInvocationList())
		{
			// Check both the target instance and the method's declaring type.
			// Static handlers (Target == null) can still reference a non-default ALC
			// via Method.DeclaringType.
			var handlerAssembly = d.Target?.GetType().Assembly
				?? d.Method.DeclaringType?.Assembly;

			if (handlerAssembly is not null)
			{
				var alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(handlerAssembly);
				if (alc is not null && alc != System.Runtime.Loader.AssemblyLoadContext.Default)
				{
					_systemThemeChanged -= (EventHandler)d;
				}
			}
		}
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

		// Seed the OS-theme cache; an active SystemThemeOverride stays authoritative because the
		// SystemTheme getter checks it before the cache.
		_lastSystemTheme ??= GetSystemTheme();

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
		var previousTheme = SystemTheme;

		// Re-poll the real OS theme; while an override is active this refreshes the cache
		// silently (the effective theme stays pinned to the override).
		_lastSystemTheme = GetSystemTheme();

		if (SystemTheme != previousTheme)
		{
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
