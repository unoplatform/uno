#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using Windows.ApplicationModel.Core;
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

		internal void RenderRootVisual(SKSurface surface, ContainerVisual rootVisual)
		{
			if (rootVisual is null)
			{
				throw new ArgumentNullException(nameof(rootVisual));
			}

			_isDirty = false;

			var children = rootVisual.GetChildrenInRenderOrder();
			for (var i = 0; i < children.Count; i++)
			{
				RenderVisual(surface, children[i]);
			}
		}

		internal void RenderVisual(SKSurface surface, Visual visual)
		{
			if (visual is { Opacity: 0 } or { IsVisible: false })
			{
				return;
			}

			if (visual.ShadowState is { } shadow)
			{
				surface.Canvas.SaveLayer(shadow.Paint);
			}
			else
			{
				surface.Canvas.Save();
			}

			// Set the position of the visual on the canvas (i.e. change coordinates system to the "XAML element" one)
			surface.Canvas.Translate(visual.Offset.X + visual.AnchorPoint.X, visual.Offset.Y + visual.AnchorPoint.Y);

			// Apply the clipping. This is relative to the visual's coordinate system, before any rendering transformation is applied.
			ApplyClip(surface, visual);

			// Applied rending transformation matrix (i.e. change coordinates system to the "rendering" one)
			var transform = GetTransform(visual);
			if (!transform.IsIdentity)
			{
				var skTransform = transform.ToSKMatrix();
				surface.Canvas.Concat(ref skTransform);
			}

			using var opacityDisposable = PushOpacity(visual.Opacity);

			visual.Render(surface);

			surface.Canvas.Restore();
		}

		private static void ApplyClip(SKSurface surface, Visual visual)
		{
			if (visual.Clip is InsetClip insetClip)
			{
				var clipRect = insetClip.SKRect;

				surface.Canvas.ClipRect(clipRect, SKClipOperation.Intersect, true);
			}
			else if (visual.Clip is RectangleClip rectangleClip)
			{
				surface.Canvas.ClipRoundRect(rectangleClip.SKRoundRect, SKClipOperation.Intersect, true);
			}
			else if (visual.Clip is CompositionGeometricClip geometricClip)
			{
				switch (geometricClip.Geometry)
				{
					case CompositionPathGeometry { Path.GeometrySource: SkiaGeometrySource2D geometrySource }:
						surface.Canvas.ClipPath(geometrySource.Geometry, antialias: true);
						break;
					case CompositionPathGeometry cpg:
						throw new InvalidOperationException($"Clipping with source {cpg.Path?.GeometrySource} is not supported");
					case null:
						// null is nop
						break;
					default:
						throw new InvalidOperationException($"Clipping with {geometricClip.Geometry} is not supported");
				}
			}
		}

		private static Matrix4x4 GetTransform(Visual visual)
		{
			var transform = visual.TransformMatrix;

			var scale = visual.Scale;
			if (scale != Vector3.One)
			{
				transform *= Matrix4x4.CreateScale(scale, visual.CenterPoint);
			}

			var orientation = visual.Orientation;
			if (orientation != Quaternion.Identity)
			{
				transform *= Matrix4x4.CreateFromQuaternion(orientation);
			}

			var rotation = visual.RotationAngle;
			if (rotation is not 0)
			{
				transform *= Matrix4x4.CreateFromAxisAngle(visual.RotationAxis, rotation);
			}

			return transform;
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
}
