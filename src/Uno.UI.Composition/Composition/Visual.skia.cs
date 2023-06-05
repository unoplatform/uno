#nullable enable

using System.Numerics;
using SkiaSharp;
using Uno.Extensions;
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

		private protected void PrepareSurfaceForDrawing(SKSurface surface)
		{
			if (ShadowState is { } shadow)
			{
				surface.Canvas.SaveLayer(shadow.Paint);
			}
			else
			{
				surface.Canvas.Save();
			}

			// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
			surface.Canvas.Translate(Offset.X + AnchorPoint.X, Offset.Y + AnchorPoint.Y);

			// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
			if (this.GetTransform() is { IsIdentity: false } transform)
			{
				var skTransform = transform.ToSKMatrix();
				surface.Canvas.Concat(ref skTransform);
			}

			// Apply the clipping defined on the element
			// (Only the Clip property, clipping applied by parent for layout constraints reason it's managed by the ShapeVisual through the ViewBox)
			// Note: The Clip is applied after the transformation matrix, so it's also transformed.
			Clip?.Apply(surface);

			using var filter = Compositor.PushFilter(Opacity);
		}

		private protected void CleanupSurfaceAfterDrawing(SKSurface surface)
		{
			surface.Canvas.Restore();
		}

		internal void RenderAtOrigin(SKSurface surface)
		{
			surface.Canvas.Save();
			surface.Canvas.Translate(-(Offset.X + AnchorPoint.X), -(Offset.Y + AnchorPoint.Y));
			Render(surface);
			surface.Canvas.Restore();
		}

		internal virtual void Render(SKSurface surface)
		{
			if (this is { Opacity: 0 } or { IsVisible: false })
			{
				return;
			}

			PrepareSurfaceForDrawing(surface);

			Draw(surface);

			CleanupSurfaceAfterDrawing(surface);
		}

		private protected virtual void Draw(SKSurface surface)
		{
		}
	}
}
