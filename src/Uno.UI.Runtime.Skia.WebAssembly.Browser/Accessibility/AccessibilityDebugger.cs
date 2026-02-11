#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Provides debug overlay visualization for the accessibility semantic DOM tree.
/// When enabled, draws visible outlines around semantic elements to aid development.
/// </summary>
internal static partial class AccessibilityDebugger
{
	private static bool _isDebugModeEnabled;

	/// <summary>
	/// Gets whether debug mode is currently enabled.
	/// </summary>
	public static bool IsDebugModeEnabled => _isDebugModeEnabled;

	/// <summary>
	/// Enables or disables debug mode visualization.
	/// When enabled, semantic elements are rendered with visible outlines.
	/// </summary>
	/// <param name="enabled">True to enable debug mode, false to disable.</param>
	public static void EnableDebugMode(bool enabled)
	{
		if (_isDebugModeEnabled == enabled)
		{
			return;
		}

		_isDebugModeEnabled = enabled;

		if (typeof(AccessibilityDebugger).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(AccessibilityDebugger).Log().Debug($"Accessibility debug mode {(enabled ? "enabled" : "disabled")}");
		}

		// Call TypeScript to toggle debug outlines
		NativeMethods.EnableDebugMode(enabled);
	}

	/// <summary>
	/// Toggles debug mode on/off.
	/// </summary>
	public static void ToggleDebugMode()
	{
		EnableDebugMode(!_isDebugModeEnabled);
	}

	/// <summary>
	/// Renders debug information for a specific element.
	/// Used during development to show ARIA attributes and handle IDs.
	/// </summary>
	/// <param name="handle">The element handle.</param>
	/// <param name="showLabels">Whether to show aria-label values as visible text.</param>
	/// <param name="showHandles">Whether to show handle IDs for debugging.</param>
	public static void RenderDebugInfo(IntPtr handle, bool showLabels = true, bool showHandles = true)
	{
		// Stub - implementation in T096
	}

	/// <summary>
	/// Configuration for debug overlay styles.
	/// </summary>
	public static class DebugStyles
	{
		/// <summary>
		/// Default outline style for focusable elements.
		/// </summary>
		public static string FocusableOutline { get; set; } = "2px solid green";

		/// <summary>
		/// Default outline style for non-focusable elements.
		/// </summary>
		public static string NonFocusableOutline { get; set; } = "1px dashed gray";

		/// <summary>
		/// Background color for interactive elements.
		/// </summary>
		public static string InteractiveBackground { get; set; } = "rgba(0, 255, 0, 0.1)";

		/// <summary>
		/// Background color for static elements.
		/// </summary>
		public static string StaticBackground { get; set; } = "rgba(128, 128, 128, 0.05)";
	}

	/// <summary>
	/// Native methods for accessibility debug mode.
	/// </summary>
	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.enableDebugMode")]
		internal static partial void EnableDebugMode(bool enabled);
	}
}
