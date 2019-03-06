using Android.App;
using Android.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.UI;
using System.Linq;
using Android.OS;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Uno.Logging;
using Microsoft.Extensions.Logging;

namespace Windows.UI.Xaml
{
	internal class FontHelper
	{
		internal static readonly Func<FontFamily, FontWeight, TypefaceStyle, Typeface> _fontFamilyToTypeFace;
		private static bool _assetsListed;
		private static readonly string DefaultFontFamilyName = "sans-serif";

		static FontHelper()
		{
			_fontFamilyToTypeFace = Funcs
				.Create<FontFamily, FontWeight, TypefaceStyle, Typeface>(InternalFontFamilyToTypeFace)
				.AsLockedMemoized();
		}

		internal static Typeface FontFamilyToTypeFace(FontFamily fontFamily, FontWeight fontWeight, TypefaceStyle style = TypefaceStyle.Normal)
		{
			return _fontFamilyToTypeFace(fontFamily, fontWeight, style);
		}

		internal static Typeface InternalFontFamilyToTypeFace(FontFamily fontFamily, FontWeight fontWeight, TypefaceStyle style)
		{
			if (fontFamily?.Source == null || fontFamily.Equals(FontFamily.Default))
			{
				fontFamily = GetDefaultFontFamily(fontWeight);
			}

			Typeface typeface;

			try
			{
				if (typeof(FontHelper).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					typeof(FontHelper).Log().Debug($"Searching for font [{fontFamily.Source}]");
				}

				// If there's a ".", we assume there's an extension and that it's a font file path.
				if (fontFamily.Source.Contains("."))
				{
					var source = fontFamily.Source;

					// The lookup is always performed in the assets folder, even if its required to specify it
					// with UWP.
					source = source.TrimStart("ms-appx://", StringComparison.OrdinalIgnoreCase);
					source = source.TrimStart("/assets/", StringComparison.OrdinalIgnoreCase);
					source = FontFamilyHelper.RemoveHashFamilyName(source);

					if (typeof(FontHelper).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						typeof(FontHelper).Log().Debug($"Searching for font as asset [{source}]");
					}

					// We need to lookup assets manually, as assets are stored this way by android, but windows
					// is case insensitive.
					string actualAsset = AssetsHelper.FindAssetFile(source);
					if (actualAsset != null)
					{
						typeface = Android.Graphics.Typeface.CreateFromAsset(Android.App.Application.Context.Assets, actualAsset);

						if (style != typeface.Style)
						{
							typeface = Typeface.Create(typeface, style);
						}
					}
					else
					{
						throw new InvalidOperationException($"Unable to find [{fontFamily.Source}] from the application's assets.");
					}
				}
				else
				{
					typeface = Android.Graphics.Typeface.Create(fontFamily.Source, style);
				}

				return typeface;
			}
			catch (Exception e)
			{
				if (typeof(FontHelper).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					typeof(FontHelper).Log().Error("Unable to find font", e);

					if (typeof(FontHelper).Log().IsEnabled(LogLevel.Warning))
					{
						if (!_assetsListed)
						{
							_assetsListed = true;

							var allAssets = AssetsHelper.AllAssets.JoinBy("\r\n");
							typeof(FontHelper).Log().Warn($"List of available assets: {allAssets}");
						}
					}
				}

				return null;
			}
		}

		private static FontFamily GetDefaultFontFamily(FontWeight fontWeight)
		{
			string fontVariant = string.Empty;

			if (fontWeight == FontWeights.Light
				|| fontWeight == FontWeights.UltraLight
				|| fontWeight == FontWeights.ExtraLight)
			{
				fontVariant = "-light";
			}
			else if (fontWeight == FontWeights.Thin
					|| fontWeight == FontWeights.SemiLight)
			{
				fontVariant = "-thin";
			}
			else if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
			{
				if (fontWeight == FontWeights.Medium)
				{
					fontVariant = "-medium";
				}
				else if (fontWeight == FontWeights.Black
						|| fontWeight == FontWeights.UltraBlack
						|| fontWeight == FontWeights.ExtraBlack)
				{
					fontVariant = "-black";
				}
			}

			return new FontFamily(DefaultFontFamilyName + fontVariant);
		}

		/// <summary>
		/// Get the ratio dictated by the user-specified text size in Accessibility
		/// </summary>
		/// <returns></returns>
		public static double GetFontRatio()
		{
			return ViewHelper.FontScale / ViewHelper.Scale;	
		}
	}
}
