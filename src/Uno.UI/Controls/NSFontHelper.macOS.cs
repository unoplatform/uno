#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml;
using CoreGraphics;
using Foundation;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Uno.Foundation.Logging;
using AppKit;
using ObjCRuntime;

namespace Windows.UI
{
	internal static class NSFontHelper
	{
		private static Func<nfloat, FontWeight, FontStyle, FontFamily, nfloat?, NSFont> _tryGetFont;

		private const int DefaultNSFontPreferredBodyFontSize = 13;
		private static readonly float DefaultPreferredBodyFontSize = (float)NSFont.SystemFontSize;

		static NSFontHelper()
		{
			_tryGetFont = InternalTryGetFont;
			_tryGetFont = _tryGetFont.AsMemoized();
		}

		internal static NSFont TryGetFont(nfloat size, FontWeight fontWeight, FontStyle fontStyle, FontFamily requestedFamily, float? preferredBodyFontSize = null)
		{
			return _tryGetFont(size, fontWeight, fontStyle, requestedFamily, preferredBodyFontSize ?? DefaultPreferredBodyFontSize);
		}

		/// <summary>
		/// Based on iOS settings of text size in General->Accessibility->LargerText
		/// </summary>
		/// <param name="size"></param>
		/// <returns>Scaled font size</returns>
		internal static nfloat GetScaledFontSize(nfloat size, float? preferredBodyFontSize = null)
		{
			return GetScaledFontSize(size, (nfloat)(preferredBodyFontSize ?? DefaultPreferredBodyFontSize));
		}

		/// <summary>
		/// Based on iOS settings of text size in General->Accessibility->LargerText.
		/// This overload was created to better work with memoized functions
		/// </summary>
		/// <param name="size"></param>
		/// <param name="basePreferredSize"></param>
		/// <returns></returns>
		private static nfloat GetScaledFontSize(nfloat size, nfloat? basePreferredSize)
		{
			//We need to scale the font size depending on the PreferredBody size. This is modified by accessibility settings.
			//https://developer.xamarin.com/api/member/MonoTouch.UIKit.NSFont.GetPreferredFontForTextStyle/
			return size * (basePreferredSize / DefaultNSFontPreferredBodyFontSize) ?? (float)1.0;
		}

		private static NSFont InternalTryGetFont(nfloat size, FontWeight fontWeight, FontStyle fontStyle, FontFamily requestedFamily, nfloat? basePreferredSize)
		{
			NSFont? font = null;

			size = GetScaledFontSize(size, basePreferredSize);

			if (requestedFamily?.Source != null)
			{
				var fontFamilyName = FontFamilyHelper.RemoveUri(requestedFamily.Source);

				// If there's a ".", we assume there's an extension and that it's a font file path.
				font = fontFamilyName.Contains(".")
					? GetCustomFont(size, fontFamilyName, fontWeight, fontStyle)
					: GetSystemFont(size, fontWeight, fontStyle, fontFamilyName);
			}

			return font ?? GetDefaultFont(size, fontWeight, fontStyle);
		}

		private static NSFont GetDefaultFont(nfloat size, FontWeight fontWeight, FontStyle fontStyle)
		{
			var font = NSFont.SystemFontOfSize(size, fontWeight.ToNSFontWeight());

			if (font is not null)
			{
				return ApplyStyle(font, size, fontStyle);
			}
			else
			{
				throw new InvalidOperationException($"Unable to find {font}");
			}
		}

		#region Load Custom Font
		private static NSFont GetCustomFont(nfloat size, string fontPath, FontWeight fontWeight, FontStyle fontStyle)
		{
			NSFont? font;
			//In Windows we define FontFamily with the path to the font file followed by the font family name, separated by a #
			var indexOfHash = fontPath.IndexOf('#');
			if (indexOfHash > 0 && indexOfHash < fontPath.Length - 1)
			{
				font = GetFontFromFamilyName(size, fontPath.Substring(indexOfHash + 1))
					?? GetFontFromFile(size, fontPath.Substring(0, indexOfHash));
			}
			else
			{
				font = GetFontFromFile(size, fontPath);
			}

			if (font == null)
			{
				font = GetDefaultFont(size, fontWeight, fontStyle);
			}

			font = ApplyWeightAndStyle(font, size, fontWeight, fontStyle);

			return font;
		}

		private static NSFont ApplyWeightAndStyle(NSFont font, nfloat size, FontWeight fontWeight, FontStyle fontStyle)
		{
			font = ApplyWeight(font, size, fontWeight);
			font = ApplyStyle(font, size, fontStyle);
			return font;
		}

		private static NSFont ApplyWeight(NSFont font, nfloat size, FontWeight fontWeight)
		{
			// TODO

			//if (fontWeight.Weight == FontWeights.Bold.Weight && !font.FontDescriptor.SymbolicTraits.HasFlag(NSFontDescriptorSymbolicTraits.Bold))
			//{
			//	var descriptor = font.FontDescriptor.CreateWithTraits(font.FontDescriptor.SymbolicTraits | NSFontDescriptorSymbolicTraits.Bold);
			//	if (descriptor != null)
			//	{
			//		font = NSFont.FromDescriptor(descriptor, size);
			//	}
			//	else
			//	{
			//		typeof(NSFontHelper).Log().Error($"Can't apply Bold on font \"{font.Name}\". Make sure the font supports it or use another FontFamily.");
			//	}
			//}
			//else if (
			//	fontWeight.Weight != FontWeights.SemiBold.Weight && // For some reason, when we load a Semibold font, we must keep the native Bold flag.
			//	fontWeight.Weight < FontWeights.Bold.Weight &&
			//	font.FontDescriptor.SymbolicTraits.HasFlag(NSFontDescriptorSymbolicTraits.Bold))
			//{
			//	var descriptor = font.FontDescriptor.CreateWithTraits(font.FontDescriptor.SymbolicTraits & ~NSFontDescriptorSymbolicTraits.Bold);
			//	if (descriptor != null)
			//	{
			//		font = NSFont.FromDescriptor(descriptor, size);
			//	}
			//	else
			//	{
			//		typeof(NSFontHelper).Log().Error($"Can't remove Bold from font \"{font.Name}\". Make sure the font supports it or use another FontFamily.");
			//	}
			//}

			return font;
		}

		private static NSFont ApplyStyle(NSFont font, nfloat size, FontStyle fontStyle)
		{
			// TODO
			//if (fontStyle == FontStyle.Italic && !font.FontDescriptor.SymbolicTraits.HasFlag(NSFontDescriptorSymbolicTraits.Italic))
			//{
			//	var descriptor = font.FontDescriptor.CreateWithTraits(font.FontDescriptor.SymbolicTraits | NSFontDescriptorSymbolicTraits.Italic);
			//	if (descriptor != null)
			//	{
			//		font = NSFont.FromDescriptor(descriptor, size);
			//	}
			//	else
			//	{
			//		typeof(NSFontHelper).Log().Error($"Can't apply Italic on font \"{font.Name}\". Make sure the font supports it or use another FontFamily.");
			//	}
			//}
			//else if (fontStyle == FontStyle.Normal && font.FontDescriptor.SymbolicTraits.HasFlag(NSFontDescriptorSymbolicTraits.Italic))
			//{
			//	var descriptor = font.FontDescriptor.CreateWithTraits(font.FontDescriptor.SymbolicTraits & ~NSFontDescriptorSymbolicTraits.Italic);
			//	if (descriptor != null)
			//	{
			//		font = NSFont.FromDescriptor(descriptor, size);
			//	}
			//	else
			//	{
			//		typeof(NSFontHelper).Log().Error($"Can't remove Italic from font \"{font.Name}\". Make sure the font supports it or use another FontFamily.");
			//	}
			//}

			return font;
		}

		private static NSFont? GetFontFromFamilyName(nfloat size, string familyName)
		{
			// TODO
			////If only one font exists for this family name, use it. Otherwise we will need to inspect the file for the right font name
			//var fontNames = NSFont.FontNamesForFamilyName(familyName);
			//return fontNames.Count() == 1 ? NSFont.FromName(fontNames[0], size) : null;
			return null;
		}

		private static NSFont? GetFontFromFile(nfloat size, string file)
		{
			var fileName = Path.GetFileNameWithoutExtension(file);
			var fileExtension = Path.GetExtension(file)!.Substring(1);

			var url = file.Contains("/")

				// Search the file using the appropriate subdirectory
				? NSBundle
					.MainBundle
					.GetUrlForResource(
						name: fileName,
						fileExtension: fileExtension,
						subdirectory: Path.GetDirectoryName(file))

				// Legacy behavior when fonts were located in the fonts folder.
				: NSBundle
					.MainBundle
					.GetUrlForResource(
						name: fileName,
						fileExtension: fileExtension,
						subdirectory: "Fonts");

			if (url is null)
			{
				if (typeof(NSFontHelper).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(NSFontHelper).Log().Debug($"Unable to find font in bundle using {file}");
				}

				return null;
			}

			var fontData = NSData.FromUrl(url);
			if (fontData is null)
			{
				if (typeof(NSFontHelper).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(NSFontHelper).Log().Debug($"Unable to load font in bundle using {url}");
				}

				return null;
			}

			//iOS loads NSFonts based on the PostScriptName of the font file
			using var fontProvider = new CGDataProvider(fontData);
			var font = CGFont.CreateFromProvider(fontProvider);

			if (font is not null)
			{
				var result = CoreText.CTFontManager.RegisterGraphicsFont(font, out var error);

				// Remove the (int) conversion when removing xamarin and net6.0 support (net7+ has implicit support for enum conversion to nint).
				if (result
					|| error?.Code == (nint)(int)CoreText.CTFontManagerError.DuplicatedName
					|| error?.Code == (nint)(int)CoreText.CTFontManagerError.AlreadyRegistered
				)
				{
					// Use the font even if the registration failed if the error code
					// reports the fonts have already been registered.
					if (font.PostScriptName is not null)
					{
						return NSFont.FromFontName(font.PostScriptName, size);
					}
					else
					{
						if (typeof(NSFontHelper).Log().IsEnabled(LogLevel.Debug))
						{
							typeof(NSFontHelper).Log().Debug($"Unable to register font from {file} ({error})");
						}

						return null;
					}
				}
				else
				{
					if (typeof(NSFontHelper).Log().IsEnabled(LogLevel.Debug))
					{
						typeof(NSFontHelper).Log().Debug($"Unable to register font from {file} ({error})");
					}
				}
			}
			else
			{
				if (typeof(NSFontHelper).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(NSFontHelper).Log().Debug($"Unable to register font from {file}");
				}

			}

			return null;
		}
		#endregion

		#region Load System Font
		private static NSFont? GetSystemFont(nfloat size, FontWeight fontWeight, FontStyle fontStyle, string fontFamilyName)
		{
			//based on Fonts available @ http://iosfonts.com/
			//for Windows parity feature, we will not support FontFamily="HelveticaNeue-Bold" (will ignore Bold and must be set by FontWeight property instead)
			var rootFontFamilyName = fontFamilyName.Split('-').FirstOrDefault();

			if (!rootFontFamilyName.IsNullOrEmpty())
			{
				var font = new StringBuilder(rootFontFamilyName);
				if (fontWeight != FontWeights.Normal || fontStyle == FontStyle.Italic)
				{
					font.Append('-');
					font.Append(GetFontWeight(fontWeight));
					font.Append(GetFontStyle(fontStyle));
				}

				var updatedFont = NSFont.FromFontName(font.ToString(), size);
				if (updatedFont != null)
				{
					return updatedFont;
				}

				font.Log().Warn("Failed to apply Font " + font);

				return NSFont.FromFontName(rootFontFamilyName, size);
			}

			return null;
		}

		private static string GetFontWeight(FontWeight fontWeight)
		{
			if (fontWeight == FontWeights.Normal)
			{
				return string.Empty;
			}
			if (fontWeight == FontWeights.Black)
			{
				return "Black";
			}
			if (fontWeight == FontWeights.Bold)
			{
				return "Bold";
			}
			if (fontWeight == FontWeights.DemiBold)
			{
				return "DemiBold";
			}
			if (fontWeight == FontWeights.ExtraBlack)
			{
				return "ExtraBlack";
			}
			if (fontWeight == FontWeights.Heavy ||
				//non corresponding FontWeight in iOS, fallback to FontWeight that makes sense
				fontWeight == FontWeights.ExtraBold ||
				fontWeight == FontWeights.UltraBlack ||
				fontWeight == FontWeights.UltraBlack ||
				fontWeight == FontWeights.UltraBold)
			{
				return "Heavy";
			}
			if (fontWeight == FontWeights.Light)
			{
				return "Light";
			}
			if (fontWeight == FontWeights.Medium)
			{
				return "Medium";
			}
			if (fontWeight == FontWeights.Regular)
			{
				return "Regular";
			}
			if (fontWeight == FontWeights.SemiBold)
			{
				return "SemiBold";
			}
			if (fontWeight == FontWeights.Thin
				|| fontWeight == FontWeights.SemiLight)
			{
				return "Thin";
			}
			if (fontWeight == FontWeights.UltraLight || fontWeight == FontWeights.ExtraLight)
			{
				return "UltraLight";
			}
			return string.Empty;
		}

		private static string GetFontStyle(FontStyle fontStyle)
		{
			if (fontStyle == FontStyle.Italic)
			{
				return "Italic";
			}
			return string.Empty;
		}
		#endregion
	}
}
