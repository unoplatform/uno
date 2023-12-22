#nullable enable
using System;
using System.Linq;
using SkiaSharp;

namespace Microsoft.UI.Composition;

public partial class CompositionViewBox
{
	internal SKRect GetRect()
		=> new(Offset.X, Offset.Y, Offset.X + Size.X, Offset.Y + Size.Y);
}
