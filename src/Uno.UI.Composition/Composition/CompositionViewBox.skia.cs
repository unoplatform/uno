#nullable enable
using System;
using System.Linq;
using SkiaSharp;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class CompositionViewBox
{
	internal SKRect GetSKRect()
		=> new(
			left: Offset.X,
			top: Offset.Y,
			right: Offset.X + Size.X,
			bottom: Offset.Y + Size.Y);

	internal Rect GetRect()
		=> new(
			x: Offset.X,
			y: Offset.Y,
			width: Size.X,
			height: Size.Y);
}
