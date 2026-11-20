#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland;

// Snapshot of one wl_output, replaced as a whole so any thread can read it without locking.
internal sealed record WaylandOutputInfo(string Name, string Description, int PixelWidth, int PixelHeight, int PhysicalWidthMm, int PhysicalHeightMm, int Scale)
{
	internal static readonly WaylandOutputInfo Unknown = new(string.Empty, string.Empty, 0, 0, 0, 0, 1);

	internal double? DiagonalSizeInInches
		=> PhysicalWidthMm > 0 && PhysicalHeightMm > 0
			? Math.Sqrt((double)PhysicalWidthMm * PhysicalWidthMm + (double)PhysicalHeightMm * PhysicalHeightMm) / 25.4
			: null;
}

// Events arrive on the dispatch thread. wl_output v2+ groups them atomically until done; v1 applies each one.
internal sealed class WaylandOutput : IDisposable
{
	private const uint ModeCurrent = 1;

	private WaylandOutputInfo _pending = WaylandOutputInfo.Unknown;
	private volatile WaylandOutputInfo _info = WaylandOutputInfo.Unknown;

	internal WaylandOutput(uint globalName, WlOutput proxy)
	{
		GlobalName = globalName;
		Proxy = proxy;
		proxy.Geometry += (_, e) => Update(_pending with
		{
			PhysicalWidthMm = e.PhysicalWidth,
			PhysicalHeightMm = e.PhysicalHeight,
			// wl_output v4 sends a stable connector name; older versions only identify the monitor model.
			Name = proxy.Version >= 4 ? _pending.Name : e.Model,
		});
		proxy.Mode += (_, e) =>
		{
			if ((e.Flags & ModeCurrent) != 0)
			{
				Update(_pending with { PixelWidth = e.Width, PixelHeight = e.Height });
			}
		};
		proxy.Scale += (_, e) => Update(_pending with { Scale = Math.Max(e.Factor, 1) });
		proxy.Name += (_, e) => Update(_pending with { Name = e.Name });
		proxy.Description += (_, e) => Update(_pending with { Description = e.Description });
		proxy.Done += (_, _) => Publish();
	}

	internal uint GlobalName { get; }

	internal WlOutput Proxy { get; }

	internal WaylandOutputInfo Info => _info;

	// Raised on the dispatch thread after Info changed.
	internal event EventHandler? Changed;

	private void Update(WaylandOutputInfo info)
	{
		_pending = info;
		if (Proxy.Version < 2)
		{
			Publish();
		}
	}

	private void Publish()
	{
		if (_info != _pending)
		{
			_info = _pending;
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	// Integer buffer scale for surfaces without wl_surface.preferred_buffer_scale: the largest scale among the
	// entered outputs (sharp on all of them), keeping the previous scale while the surface is on no output.
	internal static int GetIntegerScale(IReadOnlyCollection<WaylandOutputInfo> entered, int previous)
	{
		if (entered.Count == 0)
		{
			return previous;
		}
		var scale = 1;
		foreach (var output in entered)
		{
			scale = Math.Max(scale, output.Scale);
		}
		return scale;
	}

	public void Dispose() => Proxy.Dispose();
}
