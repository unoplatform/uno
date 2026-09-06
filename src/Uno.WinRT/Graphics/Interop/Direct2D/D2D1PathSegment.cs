using System;

namespace Windows.Graphics.Interop.Direct2D;

[Flags]
internal enum D2D1PathSegment
{
	None = 0,
	ForceUnstroked = 1,
	ForceRoundLineJoin = 2
}
