﻿using System;
using System.Linq;
using Uno.Extensions;
using Microsoft.UI.Xaml.Controls;
using Android.Text;
using Android.Text.Style;
using System.Collections.Generic;
using Uno.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Android.Graphics;
using Uno.UI.Extensions;
using Windows.Foundation;

using RadialGradientBrush = Microsoft.UI.Xaml.Media.RadialGradientBrush;

namespace Microsoft.UI.Xaml.Documents
{
	internal static partial class InlineExtensions
	{
		internal static TextPaint GetPaint(this Inline inline, Size size)
		{
			var foreground = Brush
				.GetColorWithOpacity(inline.Foreground, Colors.Transparent)
				.Value;

			Shader shader = inline.Foreground switch
			{
				GradientBrush gb => gb.GetShader(size.LogicalToPhysicalPixels()),
				RadialGradientBrush rgb => rgb.GetShader(size.LogicalToPhysicalPixels()),
				_ => null,
			};

			return Uno.UI.Controls.TextPaintPool.GetPaint(
				inline.FontWeight,
				inline.FontStyle,
				inline.FontStretch,
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
