#nullable enable

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
using Uno.Foundation.Logging;


namespace Windows.UI.Xaml
{
	internal partial class FontHelper
	{
		private static bool _assetsListed;
		private static readonly string DefaultFontFamilyName = "sans-serif";

		internal static Typeface? FontFamilyToTypeFace(FontFamily? fontFamily, FontWeight fontWeight, TypefaceStyle style = TypefaceStyle.Normal)
		{
			var entry = new FontFamilyToTypeFaceDictionary.Entry(fontFamily?.Source, fontWeight, style);

			if (!_fontFamilyToTypeFaceDictionary.TryGetValue(entry, out var typeFace))
			{
				typeFace = InternalFontFamilyToTypeFace(fontFamily, fontWeight, style);
				_fontFamilyToTypeFaceDictionary.Add(entry, typeFace);
			}

			return typeFace;
		}

		internal static Typeface? InternalFontFamilyToTypeFace(FontFamily? fontFamily, FontWeight fontWeight, TypefaceStyle style)
		{
			if (fontFamily?.Source == null || fontFamily.Equals(FontFamily.Default))
			{
				fontFamily = GetDefaultFontFamily(fontWeight);
			}

			Typeface? typeface;

			try
			{
				if (typeof(FontHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					typeof(FontHelper).Log().Debug($"Searching for font [{fontFamily.Source}]");
				}

				// If there's a ".", we assume there's an extension and that it's a font file path.
				if (fontFamily.Source.Contains("."))
				{
					var source = fontFamily.Source;

					source = source.TrimStart("ms-appx://", ignoreCase: true);

					if (!TryLoadFromPath(style, source, out typeface))
					{
						// The lookup used to be performed without the assets folder, even if its required to specify it
						// with UWP. Keep this behavior for backward compatibility.
						var legacySource = source.TrimStart("/assets/", ignoreCase: true);

						// The path for AndroidAssets is not encoded, unlike assets processed by the RetargetAssets tool.
						if (!TryLoadFromPath(style, legacySource, out typeface, encodePath: false))
						{
							throw new InvalidOperationException($"Unable to find [{fontFamily.Source}] from the application's assets.");
						}
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
				if (typeof(FontHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
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

		private static bool TryLoadFromPath(TypefaceStyle style, string source, out Typeface? typeface, bool encodePath = true)
		{
			source = FontFamilyHelper.RemoveHashFamilyName(source);

			if (typeof(FontHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(FontHelper).Log().Debug($"Searching for font as asset [{source}]");
			}

			var encodedSource = encodePath
				? AndroidResourceNameEncoder.EncodeFileSystemPath(source, prefix: "")
				: source;

			// We need to lookup assets manually, as assets are stored this way by android, but windows
			// is case insensitive.
			string actualAsset = AssetsHelper.FindAssetFile(encodedSource);
			if (actualAsset != null)
			{
				typeface = Android.Graphics.Typeface.CreateFromAsset(Android.App.Application.Context.Assets, actualAsset);

				if (typeface is not null)
				{
					if (style != typeface.Style)
					{
						typeface = Typeface.Create(typeface, style);
					}

					return true;
				}
				else
				{
					if (typeof(FontHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						typeof(FontHelper).Log().Debug($"Font [{source}] could not be created from asset [{actualAsset}]");
					}
				}
			}
			else
			{
				if (typeof(FontHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					typeof(FontHelper).Log().Debug($"Font [{source}] could not be found in app assets using [{encodedSource}]");
				}
			}

			typeface = null;
			return false;
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
