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

namespace Windows.UI.Composition
{
	public partial class CompositionVisualSurface : CompositionObject, ICompositionSurface, ISkiaSurface
	{
		private SKSurface? _surface;
		//private SKSurface? _scaleSurface;
		private DrawingSession? _drawingSession;

		SKSurface? ISkiaSurface.Surface { get => _surface; }

		void ISkiaSurface.UpdateSurface(bool recreateSurface)
		{
			//var dpi = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

			if (_surface is null || _drawingSession is null || recreateSurface)
			{
				_drawingSession?.Dispose();
				_surface?.Dispose();

				var size = SourceSize != default ? SourceSize : (SourceVisual is not null && SourceVisual.Size != default ? SourceVisual.Size : new Vector2(1000, 1000));
				//var info = new SKImageInfo((int)(size.X * dpi), (int)(size.Y * dpi), SKImageInfo.PlatformColorType, SKAlphaType.Premul);
				var info = new SKImageInfo((int)size.X, (int)size.Y, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
				_surface = SKSurface.Create(info);
				//_scaleSurface = SKSurface.Create(info);
				//_surface.Canvas.Scale((float)dpi);
				//_scaleSurface.Canvas.Scale(1.0f / (float)dpi);
				_drawingSession = new DrawingSession(_surface, DrawingFilters.Default);
			}

			if (SourceVisual is not null && _surface is not null)
			{
				_surface.Canvas.Clear();
				//_scaleSurface?.Canvas.Clear();
				if (SourceOffset != default) _surface.Canvas.Translate(-SourceOffset.X, -SourceOffset.Y);

				SourceVisual.Draw(_drawingSession.Value);
				//_scaleSurface?.Canvas.DrawSurface(_surface, 0.0f, 0.0f);
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

		public override void Dispose()
		{
			base.Dispose();

			_drawingSession?.Dispose();
			_surface?.Dispose();
			//_scaleSurface?.Dispose();
		}

		partial void OnSourceVisualChangedPartial(Visual? sourceVisual) => ((ISkiaSurface)this).UpdateSurface(SourceSize == default && sourceVisual?.Size != default);
		partial void OnSourceOffsetChangedPartial(Vector2 offset) => ((ISkiaSurface)this).UpdateSurface();
		partial void OnSourceSizeChangedPartial(Vector2 size) => ((ISkiaSurface)this).UpdateSurface(true);
	}
}
