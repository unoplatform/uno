using Android.Graphics;
using Android.Text;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
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
		private static readonly Action LogCharacterSpacingNotSupported =
			Actions.CreateOnce(() => typeof(TextPaintPool).Log().Warn("CharacterSpacing is only supported on Android API Level 21+"));

		private class Entry
		{
			public readonly FontWeight FontWeight;
			public readonly FontStyle FontStyle;
			public readonly FontFamily FontFamily;
			public readonly double FontSize;
			public readonly double CharacterSpacing;
			public readonly Windows.UI.Color Foreground;
			public readonly BaseLineAlignment BaseLineAlignment;
			public readonly TextDecorations TextDecorations;

			public Entry(FontWeight fontWeight, FontStyle fontStyle, FontFamily fontFamily, double fontSize, double characterSpacing, Windows.UI.Color foreground, BaseLineAlignment baselineAlignment, TextDecorations textDecorations)
			{
				FontWeight = fontWeight;
				FontStyle = fontStyle;
				FontFamily = fontFamily;
				FontSize = fontSize;
				CharacterSpacing = characterSpacing;
				Foreground = foreground;
				BaseLineAlignment = baselineAlignment;
				TextDecorations = textDecorations;
			}
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

		private static Dictionary<Entry, TextPaint> _entries = new Dictionary<Entry, TextPaint>(new EntryComparer());

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
				return InnerBuildPaint(fontWeight, fontStyle, fontFamily, fontSize, characterSpacing, foreground,  shader, baselineAlignment, textDecorations);
			}

			var key = new Entry(fontWeight, fontStyle, fontFamily, fontSize, characterSpacing, foreground, baselineAlignment, textDecorations);

			if (!_entries.TryGetValue(key, out var paint))
			{
				_entries.Add(key, paint = InnerBuildPaint(fontWeight, fontStyle, fontFamily, fontSize, characterSpacing, foreground, shader, baselineAlignment, textDecorations));
			}

			return paint;
		}

		private static TextPaint InnerBuildPaint(FontWeight fontWeight, FontStyle fontStyle, FontFamily fontFamily,  double fontSize, double characterSpacing, Color foreground, Shader shader,  BaseLineAlignment baselineAlignment, TextDecorations textDecorations)
		{
			var paint = new TextPaint(PaintFlags.AntiAlias);

			var paintSpecs = BuildPaintValueSpecs(fontSize, characterSpacing);

			paint.Density = paintSpecs.density;
			paint.TextSize = paintSpecs.textSize;
			paint.UnderlineText = (textDecorations & TextDecorations.Underline) == TextDecorations.Underline;
			paint.StrikeThruText = (textDecorations & TextDecorations.Strikethrough) == TextDecorations.Strikethrough;
			if (shader != null)
			{
				paint.SetShader(shader);
			}

			if (baselineAlignment == BaseLineAlignment.Superscript)
			{
				paint.BaselineShift += (int)(paint.Ascent() / 2);
			}

			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
			{
				paint.LetterSpacing = paintSpecs.letterSpacing;
			}
			else
			{
				LogCharacterSpacingNotSupported();
			}

			var typefaceStyle = TypefaceStyleHelper.GetTypefaceStyle(fontStyle, fontWeight);
			var typeface = FontHelper.FontFamilyToTypeFace(fontFamily, fontWeight, typefaceStyle);
			paint.SetTypeface(typeface);
			paint.Color = foreground;

			return paint;
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
