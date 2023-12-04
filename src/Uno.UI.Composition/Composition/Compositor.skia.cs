#nullable enable

using System;
using SkiaSharp;
using Windows.ApplicationModel.Core;

namespace Microsoft.UI.Composition;

public partial class Compositor
{
	internal bool? IsSoftwareRenderer { get; set; }

	internal void RenderRootVisual(SKSurface surface, ContainerVisual rootVisual)
	{
		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		Console.WriteLine($"Rendering root visual {rootVisual.GetHashCode()}");
		rootVisual.RenderRootVisual(surface);
	}

	partial void InvalidateRenderPartial(Visual visual)
	{
		CoreApplication.QueueInvalidateRender(visual.CompositionTarget);
	}
}
