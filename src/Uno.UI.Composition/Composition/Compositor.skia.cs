#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using Windows.UI.Core;

namespace Windows.UI.Composition
{
	public partial class Compositor
	{
		private readonly Stack<float> _opacityStack = new Stack<float>();
		private float _currentOpacity = 1.0f;
		private bool _isDirty;
		private SKColorFilter? _currentOpacityColorFilter;

		private OpacityDisposable PushOpacity(float opacity)
		{
			_opacityStack.Push(_currentOpacity);
			_currentOpacity *= opacity;
			_currentOpacityColorFilter = null;

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
				Compositor._currentOpacityColorFilter?.Dispose();
				Compositor._currentOpacityColorFilter = null;
			}
		}

		internal ContainerVisual? RootVisual { get; set; }

		internal float CurrentOpacity => _currentOpacity;
		internal SKColorFilter? CurrentOpacityColorFilter
		{
			get
			{
				if (_currentOpacity != 1.0f)
				{
					if (_currentOpacityColorFilter is null)
					{
						var opacity = 255 * _currentOpacity;
						_currentOpacityColorFilter = SKColorFilter.CreateBlendMode(new SKColor(0xFF, 0xFF, 0xFF, (byte)opacity), SKBlendMode.Modulate);
					}

					return _currentOpacityColorFilter;
				}
				else
				{
					return null;
				}
			}
		}

		internal void Render(SKSurface surface)
		{
			_isDirty = false;

			if (RootVisual != null)
			{
				var children = RootVisual.GetChildrenInRenderOrder();
				for (var i = 0; i < children.Count; i++)
				{
					RenderVisual(surface, children[i]);
				}
			}
		}

		internal void RenderVisual(SKSurface surface, Visual visual)
		{
			if (visual.Opacity != 0 && visual.IsVisible)
			{
				if (visual.ShadowState is { } shadow)
				{
					surface.Canvas.SaveLayer(shadow.Paint);
				}
				else
				{
					surface.Canvas.Save();
				}

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

				visual.Render(surface);


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
			else if (visual.Clip is RectangleClip rectangleClip)
			{
				surface.Canvas.ClipRoundRect(rectangleClip.SKRoundRect, SKClipOperation.Intersect, true);
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
				else if (geometricClip.Geometry is null)
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

		private void ComputeDropShadowValues(float offsetZ, out float dx, out float dy, out float sigmaX, out float sigmaY, out SKColor shadowColor)
		{
			// Following math magic seems to follow UWP ThemeShadow quite nicely.
			const float SHADOW_OFFSET_MAX = 150;
			const byte SHADOW_ALPHA_FALLBACK = 150;
			const float SHADOW_ALPHA_MODIFIER = 1f / 650f;
			const float SHADOW_SIGMA_X_MODIFIER = 1f / 5f;
			const float SHADOW_SIGMA_Y_MODIFIER = 1f / 3.5f;

			byte alpha;
			if (offsetZ <= SHADOW_OFFSET_MAX)
			{
				// Alpha should slightly decrease as the offset increases
				alpha = (byte)((1.0f - (offsetZ * SHADOW_ALPHA_MODIFIER)) * 255);
			}
			else
			{
				alpha = SHADOW_ALPHA_FALLBACK;
				offsetZ = SHADOW_OFFSET_MAX;
			}

			dx = 0;
			dy = offsetZ / 2 - offsetZ * SHADOW_SIGMA_Y_MODIFIER;
			sigmaX = offsetZ * SHADOW_SIGMA_X_MODIFIER;
			sigmaY = offsetZ * SHADOW_SIGMA_Y_MODIFIER;
			shadowColor = SKColor.Parse("ACACAC").WithAlpha(alpha);
		}
	}
}
