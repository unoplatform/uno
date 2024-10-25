#nullable enable
using System;
using System.Linq;
using SkiaSharp;
using Windows.Foundation;

namespace Windows.UI.Composition;

public partial class CompositionViewBox
{
	/// <summary>
	/// Layout clipping is usually applied in the element's coordinate space.
	/// However, for Panels and ScrollViewer headers specifically, WinUI applies clipping in the parent's coordinate space.
	/// So, this flag will be set to true for Panels and ScrollViewer headers, indicating that clipping is in parent's coordinate space.
	/// </summary>
	internal bool IsAncestorClip { get; set; }

	internal SKRect GetSKRect()
		=> new(
			left: Offset.X,
			top: Offset.Y,
			right: Offset.X + Size.X,
			bottom: Offset.Y + Size.Y);
}
