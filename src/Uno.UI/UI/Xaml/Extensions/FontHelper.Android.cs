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
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Uno.Foundation.Logging;
using Windows.Storage;
using Uno.UI.Xaml.Media;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Microsoft.UI.Xaml
{
	internal partial class FontHelper
	{
		private static bool _assetsListed;
		private static readonly string DefaultFontFamilyName = "sans-serif";

		internal static Typeface? FontFamilyToTypeFace(FontFamily? fontFamily, FontWeight fontWeight, FontStyle fontStyle, FontStretch fontStretch)
		{
			var italic = fontStyle is FontStyle.Italic or FontStyle.Oblique;
			var entry = new FontFamilyToTypeFaceDictionary.Entry(fontFamily?.Source, fontWeight, italic, fontStretch);

			if (!_fontFamilyToTypeFaceDictionary.TryGetValue(entry, out var typeFace))
			{
				typeFace = InternalFontFamilyToTypeFace(fontFamily, fontWeight, italic, fontStretch);
				_fontFamilyToTypeFaceDictionary.Add(entry, typeFace);
			}

			return typeFace;
		}

		internal static Typeface? InternalFontFamilyToTypeFace(FontFamily? fontFamily, FontWeight fontWeight, bool italic, FontStretch fontStretch)
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

					if (!TryLoadFromPath(fontWeight.Weight, fontStretch, italic, source, out typeface))
					{
						// The lookup used to be performed without the assets folder, even if its required to specify it
						// with UWP. Keep this behavior for backward compatibility.
						var legacySource = source.TrimStart("/assets/", ignoreCase: true);

						// The path for AndroidAssets is not encoded, unlike assets processed by the RetargetAssets tool.
						if (!TryLoadFromPath(fontWeight.Weight, fontStretch, italic, legacySource, out typeface, encodePath: false))
						{
							throw new InvalidOperationException($"Unable to find [{fontFamily.Source}] from the application's assets.");
						}
					}
				}
				else
				{
					var style = TypefaceStyleHelper.GetTypefaceStyle(italic, fontWeight);
					typeface = ATypeface.Create(fontFamily.Source, style);
					if (typeface is not null && typeface.Weight != fontWeight.Weight)
					{
						typeface = ATypeface.Create(typeface, fontWeight.Weight, italic);
					}
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

		private static string FontStretchToPercentage(FontStretch fontStretch)
		{
			return fontStretch switch
			{
				FontStretch.UltraCondensed => "50",
				FontStretch.ExtraCondensed => "62.5",
				FontStretch.Condensed => "75",
				FontStretch.SemiCondensed => "87.5",
				FontStretch.Normal => "100",
				FontStretch.SemiExpanded => "112.5",
				FontStretch.Expanded => "125",
				FontStretch.ExtraExpanded => "150",
				FontStretch.UltraExpanded => "200",
				_ => "100",
			};
		}

		private static bool TryLoadFromPath(int weight, FontStretch stretch, bool italic, string source, out Typeface? typeface, bool encodePath = true)
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
				var builder = new ATypeface.Builder(AApplication.Context.Assets!, actualAsset);
				// NOTE: We are unable to use 'ital' axis here. If that axis doesn't exist in the
				// font file, italic will break badly. However, if it exists, we will render "reasonable" (but not ideal) italic text.
				builder.SetFontVariationSettings($"'wght' {weight}, 'wdth' {FontStretchToPercentage(stretch)}");
				typeface = builder.Build();

				if (typeface is not null)
				{
					if (typeface.Weight != weight || typeface.IsItalic != italic)
					{
						typeface = Typeface.Create(typeface, weight, italic);
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
