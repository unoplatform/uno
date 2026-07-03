using System;

namespace Microsoft.Graphics.Canvas.Geometry;

[Flags]
public enum CanvasFigureSegmentOptions
{
	None = 0,
	ForceUnstroked = 1,
	ForceRoundLineJoin = 2
}
