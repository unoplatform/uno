using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Composition
{
	internal class TextVisual : Visual
	{
		private SKPaint _paint;
		private static Func<string, SKTypeface> _getTypeFace = Funcs.CreateMemoized<string, SKTypeface>(FromFamilyName);

		private readonly TextBlock _owner;

		private static SKTypeface FromFamilyName(string name)
		{
			return SKTypeface.FromFamilyName(name);
		}

		public TextVisual(Compositor compositor, TextBlock owner) : base(compositor)
		{
			_owner = owner;

			_paint = new SKPaint();
			_paint.TextEncoding = SKTextEncoding.Utf16;
			_paint.IsStroke = false;
			_paint.IsAntialias = true;
			_paint.LcdRenderText = true;
			_paint.SubpixelText = true;
		}

		internal Size Measure(Size availableSize)
		{
			_paint.Typeface = _getTypeFace(_owner.FontFamily.Source);
			_paint.TextSize = (float)_owner.FontSize;

			var metrics = _paint.FontMetrics;
			var descent = metrics.Descent;
			var ascent = metrics.Ascent;

			var lineHeight = descent - ascent;

			UpdateForeground();

			var bounds = new SKRect(0, 0, (float)availableSize.Width, (float)availableSize.Height);
			_paint.MeasureText(string.IsNullOrEmpty(_owner.Text) ? " " : _owner.Text, ref bounds);

			var size = bounds.Size;

			size.Height = lineHeight;

			return new Size(size.Width, size.Height);
		}

		public void UpdateForeground()
		{
			switch (_owner.Foreground)
			{
				case SolidColorBrush scb:
					_paint.Color = new SKColor(red: scb.Color.R, green: scb.Color.G, blue: scb.Color.B, alpha: scb.Color.A);
					break;
			}
		}

		internal override void Render(SKSurface surface, SKImageInfo info)
		{
			if (!string.IsNullOrEmpty(_owner.Text))
			{
				var metrics = _paint.FontMetrics;
				var descent = metrics.Descent;
				var ascent = metrics.Ascent;

				var lineHeight = descent - ascent;

				surface.Canvas.DrawText(_owner.Text, 0, Size.Y-descent, _paint);
			}
		}
	}
}
