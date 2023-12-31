#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Uno.Extensions;

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
			// TODO: Adjust for multi window #8341 
			CoreApplication.QueueInvalidateRender();
		}
	}
}
