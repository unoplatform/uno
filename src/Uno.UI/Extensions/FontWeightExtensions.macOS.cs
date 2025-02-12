using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using AppKit;
using Windows.UI.Text;
using ObjCRuntime;

namespace Uno.UI.Extensions
{
	public static class FontWeightExtensions
	{
		public static nfloat ToNSFontWeight(this FontWeight fontWeight)
		{
			if (fontWeight == FontWeights.Black ||
				fontWeight == FontWeights.ExtraBlack)
			{
				return NSFontWeight.Black;
			}
			if (fontWeight == FontWeights.Bold)
			{
				return NSFontWeight.Bold;
			}
			if (fontWeight == FontWeights.Heavy ||
				//non corresponding FontWeight in iOS, fallback to FontWeight that makes sense
				fontWeight == FontWeights.ExtraBold ||
				fontWeight == FontWeights.UltraBlack ||
				fontWeight == FontWeights.UltraBlack ||
				fontWeight == FontWeights.UltraBold)
			{
				return NSFontWeight.Heavy;
			}
			if (fontWeight == FontWeights.Light)
			{
				return NSFontWeight.Light;
			}
			if (fontWeight == FontWeights.Medium)
			{
				return NSFontWeight.Medium;
			}
			if (fontWeight == FontWeights.SemiBold ||
				fontWeight == FontWeights.DemiBold)
			{
				return NSFontWeight.Semibold;
			}
			if (fontWeight == FontWeights.Thin
				|| fontWeight == FontWeights.SemiLight)
			{
				return NSFontWeight.Thin;
			}
			if (fontWeight == FontWeights.UltraLight || fontWeight == FontWeights.ExtraLight)
			{
				return NSFontWeight.UltraLight;
			}

			return NSFontWeight.Regular;
		}
	}
}
