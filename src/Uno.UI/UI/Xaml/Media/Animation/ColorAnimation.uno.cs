#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ColorAnimation
{
	// CColor: packed ARGB.
	internal static uint ToArgb(Color color) => ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;

	internal static Color FromArgb(uint argb) => Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

	// XcpRound: rounds half away from zero.
	private static float XcpRound(float value) => MathF.Round(value, MidpointRounding.AwayFromZero);

	private static byte ByteSaturate(int value) => (byte)Math.Clamp(value, 0, 255);

	// CUIElement::GetForegroundBrush
	private static void GetForegroundBrush(UIElement element, out bool hasForegroundProperty, out Brush? foregroundBrush)
	{
		switch (element)
		{
			case TextBlock textBlock:
				hasForegroundProperty = true;
				foregroundBrush = textBlock.Foreground;
				break;

			case RichTextBlock richTextBlock:
				hasForegroundProperty = true;
				foregroundBrush = richTextBlock.Foreground;
				break;

			case Control control:
				hasForegroundProperty = true;
				foregroundBrush = control.Foreground;
				break;

			case ContentPresenter contentPresenter:
				hasForegroundProperty = true;
				foregroundBrush = contentPresenter.Foreground;
				break;

			default:
				hasForegroundProperty = false;
				foregroundBrush = null;
				break;
		}
	}

	private static IEnumerable<UIElement> GetChildren(UIElement element)
	{
		var count = VisualTreeHelper.GetChildrenCount(element);
		for (var i = 0; i < count; i++)
		{
			if (VisualTreeHelper.GetChild(element, i) is UIElement child)
			{
				yield return child;
			}
		}
	}
}
