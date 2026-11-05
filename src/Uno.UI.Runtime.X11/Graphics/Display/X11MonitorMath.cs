#nullable enable

using System;
using System.Collections.Generic;
using Windows.Graphics;

namespace Uno.UI.Runtime.X11;

/// <summary>Picks a monitor and its work area out of what RandR and the window manager's root properties report.</summary>
internal static class X11MonitorMath
{
	/// <summary>The monitor holding <paramref name="point"/>, or else the nearest one.</summary>
	public static bool TryFindNearest(IReadOnlyList<RectInt32> monitors, PointInt32 point, out RectInt32 monitor)
	{
		monitor = default;
		var bestDistance = long.MaxValue;
		var found = false;

		foreach (var candidate in monitors)
		{
			if (candidate.Width <= 0 || candidate.Height <= 0)
			{
				continue;
			}

			var dx = (long)Math.Max(Math.Max(candidate.X - point.X, 0), point.X - (candidate.X + candidate.Width - 1));
			var dy = (long)Math.Max(Math.Max(candidate.Y - point.Y, 0), point.Y - (candidate.Y + candidate.Height - 1));
			var distance = dx * dx + dy * dy;
			if (distance < bestDistance)
			{
				bestDistance = distance;
				monitor = candidate;
				found = true;
			}
		}

		return found;
	}

	/// <summary>
	/// The work area of <paramref name="monitor"/>: the largest overlap with what the window manager reports. Some report one
	/// rectangle per monitor, others one for the whole screen. No overlap means it isn't known, so it's the whole monitor.
	/// </summary>
	public static RectInt32 GetWorkArea(RectInt32 monitor, IReadOnlyList<RectInt32> workAreas)
	{
		var best = monitor;
		var bestArea = 0L;

		foreach (var workArea in workAreas)
		{
			var left = Math.Max(monitor.X, workArea.X);
			var top = Math.Max(monitor.Y, workArea.Y);
			var right = Math.Min(monitor.X + monitor.Width, workArea.X + workArea.Width);
			var bottom = Math.Min(monitor.Y + monitor.Height, workArea.Y + workArea.Height);

			var area = (long)Math.Max(right - left, 0) * Math.Max(bottom - top, 0);
			if (area > bestArea)
			{
				bestArea = area;
				best = new RectInt32(left, top, right - left, bottom - top);
			}
		}

		return best;
	}
}
