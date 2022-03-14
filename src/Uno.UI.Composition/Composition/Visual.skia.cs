#nullable enable

#if !__IOS__
using System.Numerics;
using System;
using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class Visual : global::Windows.UI.Composition.CompositionObject
	{
		internal virtual void Render(SKSurface surface)
		{

		}

		public CompositionClip? Clip
		{
			get;
			set;
		}

		// Backing for scroll offsets
		private Vector2 _anchorPoint = Vector2.Zero;
		private int _zIndex;

		public Vector2 AnchorPoint
		{
			get => _anchorPoint;
			set
			{
				_anchorPoint = value;
				Compositor.InvalidateRender();
			}
		}

		internal int ZIndex
		{
			get => _zIndex;
			set
			{
				if (_zIndex != value)
				{
					_zIndex = value;
					if (Parent is ContainerVisual containerVisual)
					{
						containerVisual.IsChildrenRenderOrderDirty = true;
					}
				}
			}
		}
	}
}
#endif
