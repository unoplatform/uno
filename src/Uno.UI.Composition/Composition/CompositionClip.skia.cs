#nullable enable
using System;
using System.Linq;
using SkiaSharp;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class CompositionClip
{
	/// <summary>
	/// Returns the bounds of the clip. The clip itself could be non-rectangular, e.g, rounded rectangle or path.
	/// </summary>
	internal virtual Rect? GetBounds(Visual visual)
		=> null;

	internal virtual void Apply(SKCanvas canvas, Visual visual)
	{
	}
}
