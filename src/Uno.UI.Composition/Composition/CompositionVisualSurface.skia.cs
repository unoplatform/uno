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
		private DrawingSession? _drawingSession;

		SKSurface? ISkiaSurface.Surface { get => _surface; }

		void ISkiaSurface.UpdateSurface(bool recreateSurface)
		{
			SKCanvas? canvas = null;
			if (_surface is null || _drawingSession is null || recreateSurface)
			{
				_drawingSession?.Dispose();
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

				var info = new SKImageInfo((int)size.X, (int)size.Y, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
				_surface = SKSurface.Create(info);
				canvas = _surface.Canvas;
				_drawingSession = new DrawingSession(_surface, canvas, DrawingFilters.Default);
			}

			canvas ??= _surface.Canvas;

			if (SourceVisual is not null && _surface is not null)
			{
				canvas.Clear();
				if (SourceOffset != default)
				{
					canvas.Translate(-SourceOffset.X, -SourceOffset.Y);
				}

				bool? previousCompMode = Compositor.IsSoftwareRenderer;
				Compositor.IsSoftwareRenderer = true;

				SourceVisual.Draw(_drawingSession.Value);

				Compositor.IsSoftwareRenderer = previousCompMode;
			}
		}

		void ISkiaSurface.UpdateSurface(in DrawingSession session)
		{
			if (SourceVisual is not null && session.Canvas is not null)
			{
				int save = session.Canvas.Save();
				if (SourceOffset != default)
				{
					session.Canvas.Translate(-SourceOffset.X, -SourceOffset.Y);
					session.Canvas.ClipRect(new SKRect(SourceOffset.X, SourceOffset.Y, session.Canvas.DeviceClipBounds.Width, session.Canvas.DeviceClipBounds.Height));
				}

				SourceVisual.Draw(in session);
				session.Canvas.RestoreToCount(save);
			}
		}

		private protected override void DisposeInternal()
		{
			base.Dispose();

			_drawingSession?.Dispose();
			_surface?.Dispose();
		}

		partial void OnSourceVisualChangedPartial(Visual? sourceVisual) => ((ISkiaSurface)this).UpdateSurface(SourceSize == default && sourceVisual?.Size != default);
		partial void OnSourceOffsetChangedPartial(Vector2 offset) => ((ISkiaSurface)this).UpdateSurface();
		partial void OnSourceSizeChangedPartial(Vector2 size) => ((ISkiaSurface)this).UpdateSurface(true);
	}
}
