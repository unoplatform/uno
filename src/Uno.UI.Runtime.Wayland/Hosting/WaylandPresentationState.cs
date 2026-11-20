#nullable enable

using System;

namespace Uno.UI.Runtime.Wayland;

internal sealed class WaylandPresentationState
{
	internal long Generation { get; private set; }
	internal WaylandDecorationLayout Layout { get; private set; }
	internal double Scale { get; private set; }

	internal bool Matches(WaylandDecorationLayout layout, double scale)
		=> Generation != 0 && Layout == layout && Scale == scale;

	internal bool Submit(long generation, WaylandDecorationLayout layout, double scale, Func<bool> submit)
	{
		if (!submit())
		{
			return false;
		}
		Layout = layout;
		Scale = scale;
		Generation = generation;
		return true;
	}
}
