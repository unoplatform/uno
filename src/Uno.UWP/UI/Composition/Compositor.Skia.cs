#nullable enable

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Windows.Services.Maps;
using Windows.UI.Core;

namespace Windows.UI.Composition
{
	public partial class Compositor
	{
		private readonly Stack<float> _opacityStack = new Stack<float>();
		private float _currentOpacity = 1.0f;
		private bool _isDirty = false;

		private OpacityDisposable PushOpacity(float opacity)
		{
			_opacityStack.Push(_currentOpacity);
			_currentOpacity *= opacity;

			return new OpacityDisposable(this);
		}

		private struct OpacityDisposable : IDisposable
		{
			private readonly Compositor Compositor;

			public OpacityDisposable(Compositor compositor)
			{
				Compositor = compositor;
			}

			public void Dispose()
			{
				Compositor._currentOpacity = Compositor._opacityStack.Pop();
			}
		}

		internal ContainerVisual? RootVisual { get; set; }

		internal float CurrentOpacity => _currentOpacity;

		internal void Render(SKSurface surface, SKImageInfo info)
		{
			_isDirty = false;

			if (RootVisual != null)
			{
				foreach (var visual in RootVisual.Children)
				{
					RenderVisual(surface, info, visual);
				}
			}
		}

		private void RenderVisual(SKSurface surface, SKImageInfo info, Visual visual)
		{
			if (visual.Opacity != 0 && visual.IsVisible)
			{
				surface.Canvas.Save();

				var visualMatrix = surface.Canvas.TotalMatrix;

				visualMatrix = visualMatrix.PreConcat(SKMatrix.CreateTranslation(visual.Offset.X, visual.Offset.Y));
				visualMatrix = visualMatrix.PreConcat(SKMatrix.CreateTranslation(visual.AnchorPoint.X, visual.AnchorPoint.Y));

				if (visual.RotationAngleInDegrees != 0)
				{
					visualMatrix = visualMatrix.PreConcat(SKMatrix.CreateRotationDegrees(visual.RotationAngleInDegrees, visual.CenterPoint.X, visual.CenterPoint.Y));
				}

				if (visual.TransformMatrix != Matrix4x4.Identity)
				{
					visualMatrix = visualMatrix.PreConcat(visual.TransformMatrix.ToSKMatrix44().Matrix);
				}

				surface.Canvas.SetMatrix(visualMatrix);

				ApplyClip(surface, visual);

				using var opacityDisposable = PushOpacity(visual.Opacity);

				visual.Render(surface, info);

				switch (visual)
				{
					case SpriteVisual spriteVisual:
						foreach (var inner in spriteVisual.Children)
						{
							RenderVisual(surface, info, inner);
						}
						break;

					case ContainerVisual containerVisual:
						foreach (var inner in containerVisual.Children)
						{
							RenderVisual(surface, info, inner);
						}
						break;
				}

				surface.Canvas.Restore();
			}
		}

		private static void ApplyClip(SKSurface surface, Visual visual)
		{
			if (visual.Clip is InsetClip insetClip)
			{
				var clipRect = new SKRect
				{
					Top = insetClip.TopInset - 1,
					Bottom = insetClip.BottomInset + 1,
					Left = insetClip.LeftInset - 1,
					Right = insetClip.RightInset + 1
				};

				surface.Canvas.ClipRect(clipRect, SKClipOperation.Intersect, true);
			}
			else if (visual.Clip is CompositionGeometricClip geometricClip)
			{
				if (geometricClip.Geometry is CompositionPathGeometry cpg)
				{
					if (cpg.Path?.GeometrySource is SkiaGeometrySource2D geometrySource)
					{
						surface.Canvas.ClipPath(geometrySource.Geometry, antialias: true);
					}
					else
					{
						throw new InvalidOperationException($"Clipping with source {cpg.Path?.GeometrySource} is not supported");
					}
				}
				else if(geometricClip.Geometry is null)
				{
					// null is nop
				}
				else
				{
					throw new InvalidOperationException($"Clipping with {geometricClip.Geometry} is not supported");
				}
			}
		}

		partial void InvalidateRenderPartial()
		{
			if (!_isDirty)
			{
				_isDirty = true;
				CoreWindow.QueueInvalidateRender();
			}
		}
	}
}
