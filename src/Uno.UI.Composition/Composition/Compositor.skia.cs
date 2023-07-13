#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Uno.Extensions;

namespace Windows.UI.Composition;

public partial class Compositor
{
	private bool _isDirty;

	internal void RenderRootVisual(SKSurface surface, ContainerVisual rootVisual)
	{
		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		_isDirty = false;

		// TODO: Why are we enumerating children manually instead of just let the ContainerVisual do its job?
		var children = rootVisual.GetChildrenInRenderOrder();
		for (var i = 0; i < children.Count; i++)
		{
			children[i].Render(surface);
		}
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
