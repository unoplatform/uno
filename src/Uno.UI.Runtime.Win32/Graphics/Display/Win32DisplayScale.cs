#nullable enable

using System;
using System.Globalization;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Win32;

/// <summary>
/// The scale Uno draws at, given a window's DPI and the optional <c>UNO_DISPLAY_SCALE_OVERRIDE</c>
/// environment variable that the X11 and Linux framebuffer hosts also read.
/// </summary>
/// <remarks>
/// The override only changes Uno's own logical-to-physical ratio: the process stays per-monitor DPI
/// aware, so anything Windows measures for the window (frame, caption, icons) keeps the real DPI.
/// Kept free of HWND and P/Invoke dependencies so it can be linked into Uno.UI.UnitTests.
/// </remarks>
internal static class Win32DisplayScale
{
	internal const string OverrideEnvironmentVariable = "UNO_DISPLAY_SCALE_OVERRIDE";

	internal const float BaseDpi = 96;

	// Windows offers display scales from 100% to 500%. The range leaves room on both sides of that and keeps the DPI
	// the frame is measured at (24 or more) meaningful; it isn't a limit where rendering was found to stop working.
	internal const float MinOverride = 0.25f;
	internal const float MaxOverride = 8f;

	private static readonly Lazy<float?> _override = new(() => ReadOverride(Environment.GetEnvironmentVariable(OverrideEnvironmentVariable)));

	/// <summary>The scale set through <c>UNO_DISPLAY_SCALE_OVERRIDE</c>, or null when it isn't set or isn't usable.</summary>
	internal static float? Override => _override.Value;

	// Invariant culture as on X11, but without thousands separators: "1,5" must not read as 15.
	// A value that can't be a scale is ignored instead of breaking layout.
	internal static bool TryParseOverride(string? value, out float scale)
		=> float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out scale)
			&& float.IsFinite(scale)
			&& scale > 0;

	internal static float ClampOverride(float scale) => Math.Clamp(scale, MinOverride, MaxOverride);

	/// <summary>The DPI Uno lays out and draws at: the override's, or else the window's.</summary>
	internal static float GetLogicalDpi(uint windowDpi, float? scaleOverride)
		=> scaleOverride is { } scale ? scale * BaseDpi : windowDpi;

	/// <summary>The DPI a monitor at the override's scale would have, to measure the frame Windows would draw there.</summary>
	internal static uint GetOverrideDpi(float scaleOverride) => (uint)Math.Round(scaleOverride * BaseDpi);

	/// <summary>
	/// The percentage <c>DisplayInformation.ResolutionScale</c> reports. A window's DPI keeps the truncation it always
	/// had, exact for whole DPIs; an override is rounded, as its float DPI would otherwise lose a percent (1.15 gives 114).
	/// </summary>
	internal static int GetResolutionScalePercent(float? scaleOverride, double rawPixelsPerViewPixel)
		=> scaleOverride is { } scale ? (int)Math.Round(scale * 100) : (int)(rawPixelsPerViewPixel * 100);

	/// <summary>
	/// The window size that gives a new window the client area it would get on a monitor at the override's scale.
	/// </summary>
	/// <param name="launchSize">The window size requested in logical units, as the window was created.</param>
	/// <param name="scaleOverride">The override.</param>
	/// <param name="frameAtOverrideDpi">The frame (window size minus client size) Windows draws at <see cref="GetOverrideDpi"/>.</param>
	/// <param name="realFrame">The frame Windows draws at the window's real DPI.</param>
	/// <remarks>
	/// Without an override, Windows' own DPI scaling of the window gives <c>launchSize * scale</c> and a frame at that
	/// scale. Under the override the frame is still drawn at the real DPI, so it's swapped in for the expected one.
	/// </remarks>
	internal static (int Width, int Height) GetWindowSizeForOverride(
		(int Width, int Height) launchSize,
		float scaleOverride,
		(int Width, int Height) frameAtOverrideDpi,
		(int Width, int Height) realFrame)
		=> (
			(int)(launchSize.Width * scaleOverride) - frameAtOverrideDpi.Width + realFrame.Width,
			(int)(launchSize.Height * scaleOverride) - frameAtOverrideDpi.Height + realFrame.Height);

	private static float? ReadOverride(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		if (!TryParseOverride(value, out var scale))
		{
			typeof(Win32DisplayScale).LogWarn()?.Warn($"Ignoring {OverrideEnvironmentVariable}={value}: it must be a positive number, such as 1 or 1.5.");
			return null;
		}

		var clamped = ClampOverride(scale);
		if (clamped != scale)
		{
			typeof(Win32DisplayScale).LogWarn()?.Warn($"{OverrideEnvironmentVariable}={value} is outside {MinOverride.ToString(CultureInfo.InvariantCulture)} to {MaxOverride.ToString(CultureInfo.InvariantCulture)}: using {clamped.ToString(CultureInfo.InvariantCulture)}.");
			return clamped;
		}

		typeof(Win32DisplayScale).LogDebug()?.Debug($"Using {OverrideEnvironmentVariable}={scale.ToString(CultureInfo.InvariantCulture)} instead of the window's DPI.");
		return scale;
	}
}
