#nullable enable
using System;
using Foundation;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Documents;
using AppKit;
using Uno.Extensions;

namespace Uno.UI
{
	internal static class NSStringAttributesHelper
	{
		private static readonly Func<
			(
				FontWeight fontWeight,
				FontStyle fontStyle,
				FontFamily fontFamily,
				Brush foreground,
				double fontSize,
				int characterSpacing,
				BaseLineAlignment baseLineAlignment,
				TextDecorations textDecorations
			),
			NSStringAttributes
		> _getAttributes;

		static NSStringAttributesHelper()
		{
			_getAttributes = InternalGetAttributes;
			_getAttributes = _getAttributes.AsMemoized();
		}

		internal static NSStringAttributes GetAttributes(
			FontWeight fontWeight,
			FontStyle fontStyle,
			FontFamily fontFamily,
			Brush foreground,
			double fontSize,
			int characterSpacing,
			BaseLineAlignment baseLineAlignment,
			TextDecorations textDecorations
		)
		{
			return _getAttributes((fontWeight, fontStyle, fontFamily, foreground, fontSize, characterSpacing, baseLineAlignment, textDecorations));
		}

		private static NSStringAttributes InternalGetAttributes((
			FontWeight fontWeight,
			FontStyle fontStyle,
			FontFamily fontFamily,
			Brush foreground,
			double fontSize,
			int characterSpacing,
			BaseLineAlignment baseLineAlignment,
			TextDecorations textDecorations) tuple
		)
		{
			float? GetBaselineOffset()
			{
				switch (tuple.baseLineAlignment)
				{
					case BaseLineAlignment.Superscript:
						// We assume that the fontsize will be set according to the 0.56 ratio from the non-superscript text.
						return (float)tuple.fontSize * 0.56f;

					case BaseLineAlignment.Baseline:
						return null;

					default:
						throw new NotSupportedException("The baseline alignment {0} is not supported".InvariantCultureFormat(tuple.baseLineAlignment));
				}
			}

			var font = NSFontHelper.TryGetFont((float)tuple.fontSize, tuple.fontWeight, tuple.fontStyle, tuple.fontFamily);
			var attributes = new NSStringAttributes()
			{
				ForegroundColor = Brush.GetColorWithOpacity(tuple.foreground),
				Font = font,
				BaselineOffset = GetBaselineOffset(),
				UnderlineStyle = (int)((tuple.textDecorations & TextDecorations.Underline) == TextDecorations.Underline
					? NSUnderlineStyle.Single
					: NSUnderlineStyle.None),
				StrikethroughStyle = (int)((tuple.textDecorations & TextDecorations.Strikethrough) == TextDecorations.Strikethrough
					? NSUnderlineStyle.Single
					: NSUnderlineStyle.None),
			};

			if (tuple.characterSpacing != 0f)
			{
				//CharacterSpacing is in 1/1000 of an em, iOS KerningAdjustment is in points. 1 em = 12 points
				attributes.KerningAdjustment = (tuple.characterSpacing / 1000f) * 12;
			}

			return attributes;
		}
	}
}
