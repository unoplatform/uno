using System;
using System.Linq;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using Android.Text;
using Android.Text.Style;
using System.Collections.Generic;
using Uno.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Android.Graphics;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Documents
{
	internal static partial class InlineExtensions
	{
		internal static TextPaint GetPaint(this Inline inline)
		{
			var foreground = Brush
				.GetColorWithOpacity(inline.Foreground, Colors.Transparent)
				.Value;

			Shader shader = null;

			if (inline.Foreground is GradientBrush gb)
			{
				var textBlock = inline.FindFirstParent<TextBlock>();
				if (textBlock != null)
				{
					shader = gb.GetShader(textBlock.LayoutSlot.LogicalToPhysicalPixels());
				}
			}

			return Uno.UI.Controls.TextPaintPool.GetPaint(
				inline.FontWeight,
				inline.FontStyle,
				inline.FontFamily,
				inline.FontSize,
				inline.CharacterSpacing,
				foreground,
				shader,
				inline.BaseLineAlignment,
				inline.TextDecorations
			);
		}
	}
}
