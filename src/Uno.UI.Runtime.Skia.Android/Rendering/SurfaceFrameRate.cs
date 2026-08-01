#nullable enable

using System;
using Android.Views;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Asks the display for the panel's highest refresh rate.
/// </summary>
/// <remarks>
/// Left unsaid, Android assigns the surface a frame-rate category — 90Hz on a 120Hz panel — and a rate
/// that does not divide the panel's cannot be presented evenly: 120/90 leaves a repeating one, one, two
/// vsync cadence, which reads as judder in anything animating. Measured at 64% single and 35% double
/// intervals before this call.
/// </remarks>
internal static class SurfaceFrameRate
{
	public static void RequestHighest(View view, ISurfaceHolder? holder)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			return;
		}

		try
		{
			if (holder?.Surface is not { IsValid: true } surface || view.Display is not { } display)
			{
				return;
			}

			var highest = 0f;
			foreach (var mode in display.GetSupportedModes() ?? [])
			{
				highest = Math.Max(highest, mode.RefreshRate);
			}

			if (highest <= 0)
			{
				return;
			}

			if (OperatingSystem.IsAndroidVersionAtLeast(31))
			{
				surface.SetFrameRate(highest, (int)SurfaceFrameRateCompatibility.Default, (int)SurfaceChangeFrameRate.Always);
			}
			else
			{
				surface.SetFrameRate(highest, (int)SurfaceFrameRateCompatibility.Default);
			}

			if (typeof(SurfaceFrameRate).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(SurfaceFrameRate).Log().Debug($"Requested {highest}Hz for the render surface.");
			}
		}
		catch (Exception e)
		{
			// A refused rate request must never stop the surface coming up; the frames just stay uneven.
			if (typeof(SurfaceFrameRate).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(SurfaceFrameRate).Log().Warn("Could not request a display refresh rate.", e);
			}
		}
	}
}
