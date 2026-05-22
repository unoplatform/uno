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
	internal static SystemTheme SystemTheme => _lastSystemTheme ??= EffectiveSystemTheme;

	/// <summary>
	/// The effective system theme: the override when one is set (for tests), otherwise the real
	/// OS theme reported by the platform.
	/// </summary>
	private static SystemTheme EffectiveSystemTheme => _systemThemeOverride ?? GetSystemTheme();

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

			// Setting the override is an explicit "force the system theme" action, so notify
			// observers UNCONDITIONALLY. We deliberately do NOT gate on _lastSystemTheme the way
			// RefreshSystemTheme() does (that path is for polling the real OS): _lastSystemTheme
			// tracks the last OS-notified theme and can legitimately desync from the application's
			// current theme — e.g. after a test sets an explicit app theme via
			// SetExplicitRequestedTheme and then restores it. A conditional raise would then find
			// cached == new and skip, leaving the application on a stale theme (the cause of
			// in-suite test flakiness). Re-asserting here makes the override reliable regardless of
			// prior state.
			_lastSystemTheme = EffectiveSystemTheme;
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
		var currentTheme = EffectiveSystemTheme;
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
