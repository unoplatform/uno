#nullable enable

using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Uno;
using Uno.Extensions;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Composition
{
	internal class TextVisual : Visual
	{
		private readonly SKPaint _paint;
		private static readonly Func<string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant, SKTypeface> _getTypeFace =
			Funcs.CreateMemoized<string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant, SKTypeface>(
				(nm, wt, wh, sl) => FromFamilyName(nm, wt, wh, sl));

		private readonly TextBlock _owner;

		private string? _previousRenderText;
		private string[]? _textLines;

		private static SKTypeface FromFamilyName(
			string name,
			SKFontStyleWeight weight,
			SKFontStyleWidth width,
			SKFontStyleSlant slant)
		{
			if (name.StartsWith(XamlFilePathHelper.AppXIdentifier))
			{
				var path = new Uri(name).PathAndQuery;

				var filePath = global::System.IO.Path.Combine(Windows.Application­Model.Package.Current.Installed­Location.Path, path.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar));

				var font = SKTypeface.FromFile(filePath);

				return font;
			}
			else
			{
				if (string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
				{
					return SKTypeface.FromFamilyName(null, weight, width, slant);
				}

				var typeFace = SKTypeface.FromFamilyName(name, weight, width, slant);

				// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
				if (typeFace == null)
				{
					if (typeof(TextVisual).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(TextVisual).Log().LogWarning($"The font {name} could not be found, using system default");
					}

					typeFace = SKTypeface.FromFamilyName(null, weight, width, slant);
				}

				return typeFace;
			}
		}

		public TextVisual(Compositor compositor, TextBlock owner) : base(compositor)
		{
			_owner = owner;

			_paint = new SKPaint
			{
				TextEncoding = SKTextEncoding.Utf16,
				IsStroke = false,
				IsAntialias = true,
				LcdRenderText = true,
				SubpixelText = true
			};
		}

		internal Size Measure(Size availableSize)
		{
			if (_owner.FontFamily?.Source != null)
			{
				var weight = _owner.FontWeight.ToSkiaWeight();
				//var width = _owner.FontStretch.ToSkiaWidth(); -- FontStretch not supported by Uno yet
				var width = SKFontStyleWidth.Normal;
				var slant = _owner.FontStyle.ToSkiaSlant();
				var font = _getTypeFace(_owner.FontFamily.Source, weight, width, slant);
				_paint.Typeface = font;
			}

			_paint.TextSize = (float)_owner.FontSize;

			var metrics = _paint.FontMetrics;
			var descent = metrics.Descent;
			var ascent = metrics.Ascent;

			var lineHeight = descent - ascent;

			if (_textLines == null || _previousRenderText != _owner.Text)
			{
				_textLines = _owner.Text.Split(
					new[] { "\r\n", "\r", "\n" },
					StringSplitOptions.None
				);
				_previousRenderText = _owner.Text;
			}

			var bounds = new SKRect(0, 0, (float)availableSize.Width, (float)availableSize.Height);
			_paint.MeasureText(string.IsNullOrEmpty(_owner.Text) ? " " : _owner.Text, ref bounds);

			var size = bounds.Size;

			size.Height = lineHeight * _textLines.Length;

			return new Size(size.Width, size.Height);
		}

		public void UpdateForeground()
		{
			switch (_owner.Foreground)
			{
				case SolidColorBrush scb:
					_paint.Color = new SKColor(
						red: scb.Color.R,
						green: scb.Color.G,
						blue: scb.Color.B,
						alpha: (byte)(scb.Color.A * Compositor.CurrentOpacity));
					break;
			}
		}

		internal override void Render(SKSurface surface, SKImageInfo info)
		{
			if (!string.IsNullOrEmpty(_owner.Text))
			{
				UpdateForeground();

				var metrics = _paint.FontMetrics;
				var descent = metrics.Descent;
				var ascent = metrics.Ascent;

				var lineHeight = descent - ascent;

				_textLines ??= new[] {_owner.Text};

				var y = -ascent;

				foreach (var line in _textLines)
				{
					surface.Canvas.DrawText(line, 0, y, _paint);
					y += lineHeight;
				}
			}
		}
	}
}
