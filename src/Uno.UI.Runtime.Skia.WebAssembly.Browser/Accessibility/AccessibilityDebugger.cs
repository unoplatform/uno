#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Provides debug overlay visualization for the accessibility semantic DOM tree.
/// When enabled, draws visible outlines around semantic elements to aid development.
/// Also provides performance metrics and subsystem state information.
/// </summary>
internal static partial class AccessibilityDebugger
{
	private static bool _isDebugModeEnabled;
	private static int _frameCount;
	private static double _totalFrameOverheadMs;

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

		if (enabled)
		{
			RefreshDebugOverlay();
		}
	}

	/// <summary>
	/// Toggles debug mode on/off.
	/// </summary>
	public static void ToggleDebugMode()
	{
		EnableDebugMode(!_isDebugModeEnabled);
	}

	/// <summary>
	/// Records a frame's accessibility overhead for performance tracking.
	/// Call this after each accessibility DOM update cycle.
	/// </summary>
	/// <param name="overheadMs">The time spent on accessibility updates in this frame, in milliseconds.</param>
	internal static void RecordFrameOverhead(double overheadMs)
	{
		_frameCount++;
		_totalFrameOverheadMs += overheadMs;
	}

	/// <summary>
	/// Gets the average accessibility overhead per frame in milliseconds.
	/// </summary>
	internal static double AverageFrameOverheadMs =>
		_frameCount > 0 ? _totalFrameOverheadMs / _frameCount : 0;

	/// <summary>
	/// Resets the frame overhead counters.
	/// </summary>
	internal static void ResetFrameCounters()
	{
		_frameCount = 0;
		_totalFrameOverheadMs = 0;
	}

	/// <summary>
	/// Refreshes the debug overlay with current subsystem state.
	/// Shows: semantic element count, virtualized region state, live region stats,
	/// focus sync state, and modal trap state.
	/// </summary>
	internal static void RefreshDebugOverlay()
	{
		if (!_isDebugModeEnabled)
		{
			return;
		}

		var instance = WebAssemblyAccessibility.Instance;
		if (!instance.IsAccessibilityEnabled)
		{
			return;
		}

		var modalState = instance.ActiveModalScope is not null
			? $"active (handle={instance.ActiveModalScope.ModalHandle})"
			: "none";

		NativeMethods.UpdateDebugOverlay(
			AverageFrameOverheadMs,
			_frameCount,
			modalState);
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

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateDebugOverlay")]
		internal static partial void UpdateDebugOverlay(double avgFrameOverheadMs, int totalFrames, string modalState);
	}
}
