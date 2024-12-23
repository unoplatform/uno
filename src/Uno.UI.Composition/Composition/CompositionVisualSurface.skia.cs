#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.UI.Composition;
using Windows.Graphics.Display;

namespace Microsoft.UI.Composition
{
	public partial class CompositionVisualSurface : CompositionObject, ICompositionSurface, ISkiaSurface
	{
		private SKSurface? _surface;
		private DisplayInformation? _displayInformation;
		private float _scale = 1.0f;

		SKSurface? ISkiaSurface.Surface { get => _surface; }

		void ISkiaSurface.UpdateSurface(bool recreateSurface)
		{
			SKCanvas? canvas = null;
			if (_surface is null || recreateSurface)
			{
				_surface?.Dispose();

				Vector2 size = SourceSize switch
				{
					Vector2 { X: > 0.0f, Y: > 0.0f } => SourceSize,
					_ => SourceVisual switch
					{
						Visual { Size: { X: > 0.0f, Y: > 0.0f } } => SourceVisual.Size,
						_ => new Vector2(1000, 1000)
					}
				};

				if (_displayInformation is null)
				{
					if (SourceVisual?.CompositionTarget is ICompositionTarget target
						&& target.DisplayInformation is DisplayInformation displayInfo)
					{
						_displayInformation = displayInfo;
					}
					else
					{
						_displayInformation = DisplayInformation.GetForCurrentViewSafe();
					}

					_displayInformation.DpiChanged += DpiChanged;
					_scale = (float)_displayInformation.RawPixelsPerViewPixel;
				}

				size *= _scale;

				var info = new SKImageInfo((int)size.X, (int)size.Y, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
				_surface = SKSurface.Create(info);
				canvas = _surface.Canvas;
				canvas.Scale(_scale, _scale);
			}

			canvas ??= _surface.Canvas;

			if (SourceVisual is not null && _surface is not null)
			{
				canvas.Clear();

				var previousCompMode = Compositor.IsSoftwareRenderer;
				Compositor.IsSoftwareRenderer = true;
				SourceVisual.RenderRootVisual(_surface, SourceOffset, null);
				Compositor.IsSoftwareRenderer = previousCompMode;
			}
		}

		private void DpiChanged(DisplayInformation sender, object args)
		{
			_scale = (float)sender.RawPixelsPerViewPixel;

			if (_surface is not null)
			{
				((ISkiaSurface)this).UpdateSurface(true);
			}
		}

		void ISkiaSurface.UpdateSurface(in Visual.PaintingSession session)
		{
			if (SourceVisual is not null && session.Canvas is not null)
			{
				int save = session.Canvas.Save();
				if (SourceOffset != default)
				{
					// clip to the left of and above the origin (in local coordinates).
					// Note that this is applied before the SourceOffset translates the canvas' matrix, so
					// when the translation happens, the drawing will be clipped by the SourceOffset.
					session.Canvas.ClipRect(new SKRect(0, 0, int.MaxValue, int.MaxValue));
				}

				SourceVisual.RenderRootVisual(session.Surface, SourceOffset, null);
				session.Canvas.RestoreToCount(save);
			}
		}

		private protected override void DisposeInternal()
		{
			base.Dispose();

			_surface?.Dispose();

			if (_displayInformation is not null)
			{
				_displayInformation.DpiChanged -= DpiChanged;
			}
		}

		partial void OnSourceVisualChangedPartial(Visual? sourceVisual)
		{
			bool needsRecreation = false;

			if (sourceVisual?.CompositionTarget is ICompositionTarget target
				&& target.DisplayInformation is DisplayInformation displayInfo
				&& displayInfo != _displayInformation)
			{
				if (_displayInformation is not null)
				{
					_displayInformation.DpiChanged -= DpiChanged;
				}

				_displayInformation = displayInfo;
				_displayInformation.DpiChanged += DpiChanged;

				var scale = (float)_displayInformation.RawPixelsPerViewPixel;
				if (scale != _scale)
				{
					_scale = scale;
					needsRecreation = true;
				}
			}

			((ISkiaSurface)this).UpdateSurface(needsRecreation || (SourceSize == default && sourceVisual?.Size != default));
		}

		partial void OnSourceOffsetChangedPartial(Vector2 offset) => ((ISkiaSurface)this).UpdateSurface();
		partial void OnSourceSizeChangedPartial(Vector2 size) => ((ISkiaSurface)this).UpdateSurface(true);
	}
}
