#nullable enable

using System;
using SkiaSharp;
using Windows.ApplicationModel.Core;

namespace Microsoft.UI.Composition;

public partial class Compositor
{
	private bool _isDirty;

	internal bool? IsSoftwareRenderer { get; set; }

	internal void RenderRootVisual(SKSurface surface, ContainerVisual rootVisual)
	{
		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		_isDirty = false;

		rootVisual.RenderRootVisual(surface);
	}

	partial void InvalidateRenderPartial()
	{
		if (!_isDirty)
		{
			_isDirty = true;
			// TODO: Adjust for multi window #8341, make dirty flag XamlRoot specific
			CoreApplication.QueueInvalidateRender();
		}
	}
}
