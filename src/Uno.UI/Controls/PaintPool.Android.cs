using Android.Graphics;
using Android.Text;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Color = Windows.UI.Color;

namespace Uno.UI.Controls
{
	/// <summary>
	/// A native <see cref="TextPaint"/> pool to improve drawing performance.
	/// </summary>
	internal class TextPaintPool
	{
		private record Entry(
			FontWeight FontWeight,
			FontStyle FontStyle,
			FontFamily FontFamily,
			double FontSize,
			double CharacterSpacing,
			Windows.UI.Color Foreground,
			BaseLineAlignment BaseLineAlignment,
			TextDecorations TextDecorations)
		{
			public long Timestamp { get; set; }
		}

		private class EntryComparer : IEqualityComparer<Entry>
		{
			public bool Equals(Entry x, Entry y) =>
				x.FontWeight == y.FontWeight
				&& x.FontStyle == y.FontStyle
				&& x.FontFamily == y.FontFamily
				&& x.Foreground == y.Foreground
				&& x.FontSize == y.FontSize
				&& x.CharacterSpacing == y.CharacterSpacing
				&& x.BaseLineAlignment == y.BaseLineAlignment
				&& x.TextDecorations == y.TextDecorations;

			public int GetHashCode(Entry entry) =>
				entry.FontWeight.GetHashCode()
				^ entry.FontStyle.GetHashCode()
				^ entry.FontFamily?.GetHashCode() ?? 0
				^ entry.Foreground.GetHashCode()
				^ entry.FontSize.GetHashCode()
				^ entry.CharacterSpacing.GetHashCode()
				^ entry.BaseLineAlignment.GetHashCode()
				^ entry.TextDecorations.GetHashCode();
		}

		private static Dictionary<Entry, TextPaint> _entries = new(new EntryComparer());
		private static List<Entry> _entriesList = new();
		private static Stopwatch _entriesTime = Stopwatch.StartNew();
		private static long _minEntryTimestamp;
		private const int MaxEntries = 500;

		/// <summary>
		/// Builds a TextPaint configuration.
		/// </summary>
		/// <remarks>		
		/// This is required for some JNI related reason.
		/// At some point, the reference to a member field TextPaint gets collected if used 
		/// in some of the StaticLayout methods, so we create a local copy of the
		/// paint to be used in this context.
		/// One solution could be to use a <see cref="Android.Runtime.JNIEnv.NewGlobalRef"/>, but the release of the reference
		/// can be tricky to place properly.
		/// </remarks>
		/// <returns>A <see cref="TextPaint"/> instance.</returns>
		public static TextPaint GetPaint(FontWeight fontWeight, FontStyle fontStyle, FontFamily fontFamily, double fontSize, double characterSpacing, Windows.UI.Color foreground, Shader shader, BaseLineAlignment baselineAlignment, TextDecorations textDecorations)
		{
			if (shader != null)
			{
				// The "Shader" native object can't be use as a cache key
				return InnerBuildPaint(fontWeight, fontStyle, fontFamily, fontSize, characterSpacing, foreground, shader, baselineAlignment, textDecorations);
			}

			var key = new Entry(fontWeight, fontStyle, fontFamily, fontSize, characterSpacing, foreground, baselineAlignment, textDecorations);

			if (!_entries.TryGetValue(key, out var paint))
			{
				_entries.Add(key, paint = InnerBuildPaint(fontWeight, fontStyle, fontFamily, fontSize, characterSpacing, foreground, shader, baselineAlignment, textDecorations));
				_entriesList.Add(key);

				TryScavenge();
			}

			key.Timestamp = _entriesTime.ElapsedTicks;

			return paint;
		}

		private static void TryScavenge()
		{
			if (_entriesList.Count > MaxEntries)
			{
				var cutoff = ((_entriesTime.ElapsedTicks - _minEntryTimestamp) / 2) + _minEntryTimestamp;

				for (int i = 0; i < _entriesList.Count; i++)
				{
					var entry = _entriesList[i];

					if (entry.Timestamp < cutoff)
					{
						_entries.Remove(entry);
						_entriesList.RemoveAt(i--);
					}
				}

				_minEntryTimestamp = cutoff;

				if (typeof(TextPaintPool).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(TextPaintPool).Log().Debug($"Cleared pool ({_entries.Count} left)");
				}
			}
		}

		private static TextPaint InnerBuildPaint(FontWeight fontWeight, FontStyle fontStyle, FontFamily fontFamily, double fontSize, double characterSpacing, Color foreground, Shader shader, BaseLineAlignment baselineAlignment, TextDecorations textDecorations)
		{
			var paintSpecs = BuildPaintValueSpecs(fontSize, characterSpacing);

			var typefaceStyle = TypefaceStyleHelper.GetTypefaceStyle(fontStyle, fontWeight);

			return TextPaintPoolNative.BuildPaint(
				paintSpecs.density,
				paintSpecs.textSize,
				paintSpecs.letterSpacing,
				FontHelper.FontFamilyToTypeFace(fontFamily, fontWeight, typefaceStyle),
				(int)((Android.Graphics.Color)foreground),
				(textDecorations & TextDecorations.Underline) == TextDecorations.Underline,
				(textDecorations & TextDecorations.Strikethrough) == TextDecorations.Strikethrough,
				baselineAlignment == BaseLineAlignment.Superscript,
				shader
			);
		}

		internal static (float density, float textSize, float letterSpacing) BuildPaintValueSpecs(double fontSize, double characterSpacing)
		{
			double size = ViewHelper.LogicalToPhysicalPixels(fontSize);
			var letterSpacing = (float)characterSpacing / 1000f; // Android LetterSpacing is in em units
			var rawTextSize = (float)ViewHelper.ApplyDimension(Android.Util.ComplexUnitType.Px, size);

			var density = (float)ViewHelper.Scale;
			var textSize = rawTextSize * (float)FontHelper.GetFontRatio();

			return (density, textSize, letterSpacing);
		}
	}
}
