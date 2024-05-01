#nullable enable
using System;
using System.Linq;
using SkiaSharp;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class CompositionViewBox
{
	internal bool IsAncestorClip { get; set; }

	internal SKRect GetSKRect()
		=> new(
			left: Offset.X,
			top: Offset.Y,
			right: Offset.X + Size.X,
			bottom: Offset.Y + Size.Y);
}
