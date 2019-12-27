using System;
using System.Web;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Uno.Extensions;

namespace Uno.UI.UI.Xaml.Documents
{
	internal static class UIElementTextHelper
	{
		internal static void SetText(this UIElement element, string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				// UWP will return a height on empty textblocks, so we need
				// to make sure HTML will behave the same way. To achieve
				// that, we use a zero-width white space special character.
				// http://www.fileformat.info/info/unicode/char/200b/index.htm

				if (string.IsNullOrEmpty(text))
				{
					text = "\u200b";
				}
				else
				{
					text = "\u200b" + text;
				}
			}

			element.SetProperty("textContent", text);
		}

		internal static void SetFontStyle(this UIElement element, object localValue)
		{
			if (localValue is UnsetValue)
			{
				element.ResetStyle("font-style");
			}
			else
			{
				var value = (FontStyle) localValue;
				switch (value)
				{
					case FontStyle.Normal:
						element.SetStyle("font-style", "normal");
						break;
					case FontStyle.Italic:
						element.SetStyle("font-style", "italic");
						break;
					case FontStyle.Oblique:
						element.SetStyle("font-style", "oblique");
						break;
				}
			}
		}

		internal static void SetFontWeight(this UIElement element, object localValue)
		{
			if (localValue is UnsetValue)
			{
				element.ResetStyle("font-weight");
			}
			else
			{
				element.SetStyle("font-weight", ((FontWeight)localValue).ToCssString());
			}
		}

		internal static void SetFontFamily(this UIElement element, object localValue)
		{
			if (localValue is UnsetValue)
			{
				element.ResetStyle("font-family");
			}
			else
			{
				var value = (FontFamily) localValue;
				if (value != null)
				{
					// TODO
					var actualFontFamily = value.Source;
					if (actualFontFamily == "XamlAutoFontFamily")
					{
						value = FontFamily.Default;
					}

					element.SetStyle("font-family", value.Source);
				}
			}
		}

		internal static void SetFontSize(this UIElement element, object localValue)
		{
			if (localValue is UnsetValue)
			{
				element.ResetStyle("font-size");
			}
			else
			{
				var value = (double)localValue;
				element.SetStyle("font-size", value.ToStringInvariant() + "px");
			}
		}

		internal static void SetMaxLines(this UIElement element, object localValue)
		{
			// Not available yet
		}

		private static void SetTextTrimming(this UIElement element, object localValue)
		{
			switch (localValue)
			{
				case TextTrimming.CharacterEllipsis:
				case TextTrimming.WordEllipsis: // Word-level ellipsis not supported by HTML/CSS
					element.SetStyle("text-overflow", "ellipsis");
					break;

				case TextTrimming.Clip:
					element.SetStyle("text-overflow", "clip");
					break;

				case UnsetValue uv:
					element.ResetStyle("text-overflow");
					break;

				default:
					element.SetStyle("text-overflow", "");
					break;
			}
		}

		internal static void SetForeground(this UIElement element, object localValue)
		{
			switch (localValue)
			{
				case SolidColorBrush scb:
					element.SetStyle("color", scb.ColorWithOpacity.ToCssString());
					break;

				case UnsetValue uv:

				// TODO: support other foreground types
				default:
					element.ResetStyle("color");
					break;
			}
		}

		internal static void SetCharacterSpacing(this UIElement element, object localValue)
		{
			if (localValue is UnsetValue)
			{
				element.ResetStyle("letter-spacing");
			}
			else
			{
				var value = (int) localValue;
				element.SetStyle("letter-spacing", (value / 1000.0).ToStringInvariant() + "em");
			}
		}

		internal static void SetLineHeight(this UIElement element, object localValue)
		{
			if (localValue is UnsetValue)
			{
				element.ResetStyle("line-height");
			}
			else
			{
				var value = (double) localValue;
				if (Math.Abs(value) < 0.0001)
				{
					element.ResetStyle("line-height");
				}
				else
				{
					element.SetStyle("line-height", value.ToStringInvariant() + "px");
				}
			}
		}

		internal static void SetTextAlignment(this UIElement element, object localValue)
		{
			if (localValue is UnsetValue)
			{
				element.ResetStyle("text-align");
			}
			else
			{
				var value = (TextAlignment) localValue;
				switch (value)
				{
					case TextAlignment.Left:
						element.SetStyle("text-align", "left");
						break;
					case TextAlignment.Center:
						element.SetStyle("text-align", "center");
						break;
					case TextAlignment.Right:
						element.SetStyle("text-align", "right");
						break;
					case TextAlignment.Justify:
						element.SetStyle("text-align", "justify");
						break;
					case TextAlignment.DetectFromContent:
					default:
						element.ResetStyle("text-align");
						break;
				}
			}
		}

		internal static void SetTextWrappingAndTrimming(this UIElement element, object textWrapping, object textTrimming)
		{
			if (textWrapping is UnsetValue)
			{
				element.ResetStyle("white-space", "word-break", "text-overflow");
			}
			else
			{
				var value = (TextWrapping) textWrapping;
				switch (value)
				{
					case TextWrapping.NoWrap:
						element.SetAttribute("wrap", "off");
						element.SetStyle(
							("white-space", "pre"),
							("word-break", ""));

						// Triming and wrapping are not yet supported by browsers. This spec would enable it:
						// https://drafts.csswg.org/css-overflow-3/#propdef-block-ellipsis
						//
						// For now, trimming isonly supported when wrapping is disabled.
						SetTextTrimming(element, textTrimming);
						break;
					case TextWrapping.Wrap:
						element.SetAttribute("wrap", "soft");
						element.SetStyle(
							("white-space", ""),
							("word-break", "break-word"), // This is required to still wrap words that are longer than the ViewPort
							("text-overflow", ""));
						break;
					case TextWrapping.WrapWholeWords:
						element.SetAttribute("wrap", "soft");
						element.SetStyle(
							("white-space", ""),
							("word-break", "keep-all"), // This is required to still wrap words that are longer than the ViewPort
							("text-overflow", ""));
						break;
				}
			}
		}

		internal static void SetTextDecorations(this UIElement element, object localValue)
		{
			if (localValue is UnsetValue)
			{
				element.ResetStyle("text-decoration");
			}
			else
			{
				var value = (TextDecorations)localValue;
				switch (value)
				{
					case TextDecorations.None:
						element.SetStyle("text-decoration", "none");
						break;
					case TextDecorations.Underline:
						element.SetStyle("text-decoration", "underline");
						break;
					case TextDecorations.Strikethrough:
						element.SetStyle("text-decoration", "line-through");
						break;
					case TextDecorations.Underline | TextDecorations.Strikethrough:
						element.SetStyle("text-decoration", "underline line-through");
						break;
				}
			}
		}

		internal static void SetTextPadding(this UIElement element, object localValue)
		{
			if (localValue is UnsetValue)
			{
				element.ResetStyle("padding");
			}
			else
			{
				var padding = (Thickness)localValue;
				var paddingStr = new[]
				{
					padding.Top.ToStringInvariant(),
					"px ",
					padding.Right.ToStringInvariant(),
					"px ",
					padding.Bottom.ToStringInvariant(),
					"px ",
					padding.Left.ToStringInvariant(),
					"px"
				};

				element.SetStyle("padding", string.Concat(paddingStr));
			}
		}
	}
}
