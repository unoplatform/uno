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
				_drawingSession = new DrawingSession(_surface, DrawingFilters.Default);
			}

			if (SourceVisual is not null && _surface is not null)
			{
				_surface.Canvas.Clear();
				if (SourceOffset != default)
				{
					_surface.Canvas.Translate(-SourceOffset.X, -SourceOffset.Y);
				}

				bool? previousCompMode = Compositor.IsSoftwareRenderer;
				Compositor.IsSoftwareRenderer = true;

				SourceVisual.Draw(_drawingSession.Value);

				Compositor.IsSoftwareRenderer = previousCompMode;
			}
		}

		void ISkiaSurface.UpdateSurface(in DrawingSession session)
		{
			if (SourceVisual is not null && session.Surface is not null)
			{
				int save = session.Surface.Canvas.Save();
				if (SourceOffset != default)
				{
					session.Surface.Canvas.Translate(-SourceOffset.X, -SourceOffset.Y);
					session.Surface.Canvas.ClipRect(new SKRect(SourceOffset.X, SourceOffset.Y, session.Surface.Canvas.DeviceClipBounds.Width, session.Surface.Canvas.DeviceClipBounds.Height));
				}

				SourceVisual.Draw(in session);
				session.Surface.Canvas.RestoreToCount(save);
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
