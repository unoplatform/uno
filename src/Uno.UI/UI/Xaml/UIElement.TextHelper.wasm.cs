using System;
using System.Web;
using Windows.Foundation;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Uno.Collections;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;

using RadialGradientBrush = Microsoft/* UWP don't rename */.UI.Xaml.Media.RadialGradientBrush;
using Uno.UI.Helpers;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		internal void SetText(string text)
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

			this.SetProperty("textContent", text);
		}

		internal void SetFontStyle(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("font-style");
			}
			else
			{
				var value = (FontStyle)localValue;
				switch (value)
				{
					case FontStyle.Normal:
						this.SetStyle("font-style", "normal");
						break;
					case FontStyle.Italic:
						this.SetStyle("font-style", "italic");
						break;
					case FontStyle.Oblique:
						this.SetStyle("font-style", "oblique");
						break;
				}
			}
		}

		internal void SetFontWeight(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("font-weight");
			}
			else
			{
				this.SetStyle("font-weight", ((FontWeight)localValue).ToCssString());
			}
		}

		internal void SetFontFamily(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				ResetStyle("font-family");
			}
			else if (localValue is FontFamily font)
			{
				var actualFontFamily = font.Source;
				if (actualFontFamily == "XamlAutoFontFamily")
				{
					font = FontFamily.Default;
				}

				SetStyle("font-family", font.CssFontName);

				font.RegisterForInvalidateMeasureOnFontLoaded(this);
			}
		}

		internal void SetFontSize(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("font-size");
			}
			else
			{
				var value = (double)localValue;
				this.SetStyle("font-size", value.ToStringInvariant() + "px");
			}
		}

		internal void SetMaxLines(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("display", "-webkit-line-clamp", "webkit-box-orient");
			}
			else
			{
				var value = (int)localValue;
				this.SetStyle(("display", "-webkit-box"), ("-webkit-line-clamp", value.ToStringInvariant()), ("-webkit-box-orient", "vertical"));
			}
		}

		private void SetTextTrimming(object localValue)
		{
			switch (localValue)
			{
				case TextTrimming.CharacterEllipsis:
				case TextTrimming.WordEllipsis: // Word-level ellipsis not supported by HTML/CSS
					this.SetStyle("text-overflow", "ellipsis");
					break;

				case TextTrimming.Clip:
					this.SetStyle("text-overflow", "clip");
					break;

				case UnsetValue uv:
					this.ResetStyle("text-overflow");
					break;

				default:
					this.SetStyle("text-overflow", "");
					break;
			}
		}

		internal void SetForeground(object localValue)
		{
			switch (localValue)
			{
				case SolidColorBrush scb:
					WindowManagerInterop.SetElementColor(HtmlId, scb.ColorWithOpacity);
					break;
				case GradientBrush gradient:
					// background-size not supported for inlines (e.g, Run) because DesiredSize is always zero there.
					// In fact, inlines shouldn't have DesiredSize in the first place as they are TextElement → DependencyObject
					// They don't inherit from UIElement.
					this.SetStyle(
						("background", gradient.ToCssString(this.RenderSize)),
						("color", "transparent"),
						("background-clip", "text"),
						("background-size", this is TextBlock ? $"{this.DesiredSize.Width}px" : "auto")
					);
					break;

				case RadialGradientBrush radialGradient:
					this.SetStyle(
						("background", radialGradient.ToCssString(this.RenderSize)),
						("color", "transparent"),
						("background-clip", "text")
					);
					break;

				case ImageBrush imageBrush:
					if (imageBrush.ImageDataCache is not { } img)
					{
						return;
					}

					switch (img.Kind)
					{
						case ImageDataKind.Empty:
						case ImageDataKind.Error:
							this.ResetStyle(
								"background-color",
								"background-image",
								"background-size");
							this.SetStyle(
								("color", "transparent"),
								("background-clip", "text"));
							break;

						case ImageDataKind.DataUri:
						case ImageDataKind.Url:
						default:
							this.SetStyle(
								("color", "transparent"),
								("background-clip", "text"),
								("background-color", ""),
								("background-origin", "content-box"),
								("background-position", imageBrush.ToCssPosition()),
								("background-size", imageBrush.ToCssBackgroundSize()),
								("background-image", "url(" + img.Value + ")")
							);
							break;
					}
					break;
				case AcrylicBrush acrylic:
					acrylic.Apply(this);
					this.SetStyle("background-clip", "text");
					break;

				case UnsetValue uv:

				// TODO: support other foreground types
				default:
					this.ResetStyle("color", "background", "background-clip");
					AcrylicBrush.ResetStyle(this);
					break;
			}
		}

		internal void SetCharacterSpacing(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("letter-spacing");
			}
			else
			{
				var value = (int)localValue;
				this.SetStyle("letter-spacing", (value / 1000.0).ToStringInvariant() + "em");
			}
		}

		internal void SetLineHeight(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("line-height");
			}
			else
			{
				var value = (double)localValue;
				if (Math.Abs(value) < 0.0001)
				{
					this.ResetStyle("line-height");
				}
				else
				{
					this.SetStyle("line-height", value.ToStringInvariant() + "px");
				}
			}
		}

		internal void SetTextAlignment(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("text-align");
			}
			else
			{
				var value = (TextAlignment)localValue;
				switch (value)
				{
					case TextAlignment.Left:
						this.SetStyle("text-align", "left");
						break;
					case TextAlignment.Center:
						this.SetStyle("text-align", "center");
						break;
					case TextAlignment.Right:
						this.SetStyle("text-align", "right");
						break;
					case TextAlignment.Justify:
						this.SetStyle("text-align", "justify");
						break;
					case TextAlignment.DetectFromContent:
					default:
						this.ResetStyle("text-align");
						break;
				}
			}
		}

		internal void SetTextWrappingAndTrimming(object textWrapping, object textTrimming)
		{
			if (textWrapping == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("white-space", "word-break", "text-overflow");
			}
			else
			{
				var value = (TextWrapping)textWrapping;
				switch (value)
				{
					case TextWrapping.NoWrap:
						this.SetStyle(
							("white-space", "pre"),
							("word-break", ""));

						// Triming and wrapping are not yet supported by browsers. This spec would enable it:
						// https://drafts.csswg.org/css-overflow-3/#propdef-block-ellipsis
						//
						// For now, trimming isonly supported when wrapping is disabled.
						SetTextTrimming(textTrimming);
						break;
					case TextWrapping.Wrap:
						this.SetStyle(
							("white-space", "pre-wrap"),
							("word-break", "break-word"), // This is required to still wrap words that are longer than the ViewPort
							("text-overflow", ""));
						break;
					case TextWrapping.WrapWholeWords:
						this.SetStyle(
							("white-space", "pre-wrap"),
							("word-break", "keep-all"), // This is required to still wrap words that are longer than the ViewPort
							("text-overflow", ""));
						break;
				}
			}
		}

		internal void SetTextDecorations(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("text-decoration");
			}
			else
			{
				var value = (TextDecorations)localValue;
				switch (value)
				{
					case TextDecorations.None:
						this.SetStyle("text-decoration", "none");
						break;
					case TextDecorations.Underline:
						this.SetStyle("text-decoration", "underline");
						break;
					case TextDecorations.Strikethrough:
						this.SetStyle("text-decoration", "line-through");
						break;
					case TextDecorations.Underline | TextDecorations.Strikethrough:
						this.SetStyle("text-decoration", "underline line-through");
						break;
				}
			}
		}

		internal void SetTextPadding(object localValue)
		{
			if (localValue == DependencyProperty.UnsetValue)
			{
				this.ResetStyle("padding");
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

				this.SetStyle("padding", string.Concat(paddingStr));
			}
		}
	}
}
