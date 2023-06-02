#nullable enable

using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition.Composition;

namespace Windows.UI.Composition
{
	public partial class Visual : global::Windows.UI.Composition.CompositionObject
	{
		private CompositionClip? _clip;
		private Vector2 _anchorPoint = Vector2.Zero; // Backing for scroll offsets
		private int _zIndex;

		public CompositionClip? Clip
		{
			get => _clip; 
			set => SetProperty(ref _clip, value);
		}

		public Vector2 AnchorPoint
		{
			get => _anchorPoint;
			set
			{
				SetProperty(ref _anchorPoint, value);
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
					SetProperty(ref _zIndex, value);
					if (Parent is ContainerVisual containerVisual)
					{
						containerVisual.IsChildrenRenderOrderDirty = true;
					}
				}
			}
		}

		internal ShadowState? ShadowState { get; set; }

		internal virtual void Render(SKSurface surface)
		{
		}
	}
}
