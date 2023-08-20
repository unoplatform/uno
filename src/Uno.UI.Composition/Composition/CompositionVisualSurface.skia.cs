#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.UI.Composition;

namespace Windows.UI.Composition
{
	public partial class CompositionVisualSurface : CompositionObject, ICompositionSurface
	{
		private SKSurface? _surface;
		private DrawingSession? _drawingSession;

		internal SKSurface? Surface { get => _surface; }

		internal void UpdateSurface(bool recreateSurface = false)
		{
			if (_surface is null || _drawingSession is null || recreateSurface)
			{
				_drawingSession?.Dispose();
				_surface?.Dispose();

				var size = SourceSize != default ? SourceSize : (SourceVisual is not null ? SourceVisual.Size : new Vector2(1000, 1000));
				var info = new SKImageInfo((int)size.X, (int)size.Y, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
				_surface = SKSurface.Create(info);
				_drawingSession = new DrawingSession(_surface, DrawingFilters.Default);
			}

			if (SourceVisual is not null)
			{
				_surface.Canvas.Clear();
				_surface.Canvas.Save();
				_surface.Canvas.Translate(-SourceOffset.X, -SourceOffset.Y);

				SourceVisual.Draw(_drawingSession.Value);
			}
		}

		public override void Dispose()
		{
			base.Dispose();

			_drawingSession?.Dispose();
			_surface?.Dispose();
		}

		partial void OnSourceVisualChangedPartial(Visual? sourceVisual) => UpdateSurface();
		partial void OnSourceOffsetChangedPartial(Vector2 offset) => UpdateSurface();
		partial void OnSourceSizeChangedPartial(Vector2 size) => UpdateSurface(true);
	}
}
