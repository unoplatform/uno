using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using UIKit;
using Windows.UI.Text;

namespace Uno.UI.Extensions
{
	public static class FontWeightExtensions
	{
		public static UIFontWeight ToUIFontWeight(this FontWeight fontWeight)
		{
			if (fontWeight == FontWeights.Black ||
				fontWeight == FontWeights.ExtraBlack)
			{
				return UIFontWeight.Black;
			}
			if (fontWeight == FontWeights.Bold)
			{
				return UIFontWeight.Bold;
			}
			if (fontWeight == FontWeights.Heavy ||
				//non corresponding FontWeight in iOS, fallback to FontWeight that makes sense
				fontWeight == FontWeights.ExtraBold ||
				fontWeight == FontWeights.UltraBlack ||
				fontWeight == FontWeights.UltraBlack ||
				fontWeight == FontWeights.UltraBold)
			{
				return UIFontWeight.Heavy;
			}
			if (fontWeight == FontWeights.Light)
			{
				return UIFontWeight.Light;
			}
			if (fontWeight == FontWeights.Medium)
			{
				return UIFontWeight.Medium;
			}
			if (fontWeight == FontWeights.SemiBold ||
				fontWeight == FontWeights.DemiBold)
			{
				return UIFontWeight.Semibold;
			}
			if (fontWeight == FontWeights.Thin
				|| fontWeight == FontWeights.SemiLight)
			{
				return UIFontWeight.Thin;
			}
			if (fontWeight == FontWeights.UltraLight || fontWeight == FontWeights.ExtraLight)
			{
				return UIFontWeight.UltraLight;
			}

			return UIFontWeight.Regular;
		}
	}
}
