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
using UIKit;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml;
using CoreText;
using ObjCRuntime;
using Uno.UI.Xaml.Media;

namespace Uno.UI.Xaml;

internal static class FontHelper
{
	private static Func<FontProperties, FontFamily, nfloat?, UIFont?> _tryGetFont;

	private const int DefaultUIFontPreferredBodyFontSize = 17;
	private static float? DefaultPreferredBodyFontSize = UIFont.PreferredBody.FontDescriptor.FontAttributes.Size;

	static FontHelper()
	{
		_tryGetFont = InternalTryGetFont;
		_tryGetFont = _tryGetFont.AsMemoized();
	}

	internal static UIFont? TryGetFont(
		FontProperties fontProperties,
		FontFamily requestedFamily,
		float? preferredBodyFontSize = null) =>
		_tryGetFont(fontProperties, requestedFamily, preferredBodyFontSize ?? DefaultPreferredBodyFontSize);

	/// <summary>
	/// Based on iOS settings of text size in General->Accessibility->LargerText
	/// </summary>
	/// <param name="size"></param>
	/// <returns>Scaled font size</returns>
	internal static nfloat GetScaledFontSize(nfloat size, float? preferredBodyFontSize = null) =>
		GetScaledFontSize(size, preferredBodyFontSize ?? DefaultPreferredBodyFontSize);

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
		//https://developer.xamarin.com/api/member/MonoTouch.UIKit.UIFont.GetPreferredFontForTextStyle/
		if (FeatureConfiguration.Font.IgnoreTextScaleFactor)
		{
			return size;
		}

		var originalScale = (basePreferredSize / DefaultUIFontPreferredBodyFontSize) ?? (float)1.0;
		if (FeatureConfiguration.Font.MaximumTextScaleFactor is float scaleFactor)
		{
			return size * (nfloat)Math.Min(originalScale, scaleFactor);
		}
		return size * originalScale;
	}

	private static UIFont? InternalTryGetFont(FontProperties fontProperties, FontFamily requestedFamily, nfloat? basePreferredSize)
	{
		if (typeof(FontHelper).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(FontHelper).Log().Trace(
				$"Getting font (size:{fontProperties.Size}, weight:{fontProperties.Weight}, stretch:{fontProperties.Stretch}, style:{fontProperties.Style}, family:{requestedFamily}, basePreferredSize:{basePreferredSize})");
		}

		UIFont? font = null;

		fontProperties = fontProperties with
		{
			Size = GetScaledFontSize(fontProperties.Size, basePreferredSize)
		};

		if (requestedFamily?.Source is not null)
		{
			if (XamlFilePathHelper.TryGetMsAppxAssetPath(requestedFamily.Source, out var path))
			{
				font = GetCustomFont(path, fontProperties);
			}
			else
			{
				var fontFamilyName = FontFamilyHelper.RemoveUri(requestedFamily.Source);

				// If there's a ".", we assume there's an extension and that it's a font file path.
				font = fontFamilyName.Contains(".") ? GetCustomFont(fontFamilyName, fontProperties) : GetSystemFont(fontProperties, fontFamilyName);
			}
		}

		return font ?? GetDefaultFont(fontProperties);
	}

	private static UIFont? GetDefaultFont(FontProperties properties)
	{
		if (!UIDevice.CurrentDevice.CheckSystemVersion(8, 2))
		{
			return GetSystemFont(properties, "HelveticaNeue");
		}

		return ApplyStyle(
			UIFont.SystemFontOfSize(properties.Size, properties.Weight.ToUIFontWeight()),
			properties);
	}

	#region Load Custom Font

	private static string TryAdjustFromManifest(NSUrl manifestUrl, FontProperties properties)
	{
		var fontData = NSData.FromUrl(manifestUrl);

		using var jsonStream = new MemoryStream(fontData.ToArray());
		var familyName = FontManifestHelpers.GetFamilyNameFromManifest(
			jsonStream,
			properties.Weight,
			properties.Style,
			properties.Stretch);

		familyName = familyName.TrimStart("ms-appx://", ignoreCase: true);

		return familyName;
	}

	private static UIFont? GetCustomFont(string fontPath, FontProperties fontProperties)
	{
		if (typeof(FontHelper).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(FontHelper).Log().Trace(
				$"Searching custom font (size:{fontProperties.Size}, weight:{fontPath}, style:{fontProperties.Style}, stretch:{fontProperties.Stretch}, fontWeight:{fontProperties.Weight})");
		}

		UIFont? font;
		//In Windows we define FontFamily with the path to the font file followed by the font family name, separated by a #
		var indexOfHash = fontPath.IndexOf('#');

		var actualFontPath = "";
		bool testFontFamilyName = false;
		bool skipAdjustments = false;
		if (indexOfHash > 0 && indexOfHash < fontPath.Length - 1)
		{
			actualFontPath = fontPath.Substring(0, indexOfHash);
		}
		else
		{
			actualFontPath = fontPath;
		}

		if (GetFontManifestUrl(actualFontPath) is { } manifestUrl &&
			TryAdjustFromManifest(manifestUrl, fontProperties) is { } adjustedPath)
		{
			fontPath = adjustedPath;
			skipAdjustments = true;
		}
		else
		{
			testFontFamilyName = true;
		}

		if (testFontFamilyName && indexOfHash > 0 && indexOfHash < fontPath.Length - 1)
		{
			font = GetFontFromFamilyName(fontProperties.Size, fontPath.Substring(indexOfHash + 1))
				?? GetFontFromFile(fontProperties.Size, fontPath.Substring(0, indexOfHash));
		}
		else
		{
			font = GetFontFromFile(fontProperties.Size, fontPath);
		}

		if (font == null)
		{
			font = GetDefaultFont(fontProperties);
		}

		if (font is not null && !skipAdjustments)
		{
			font = ApplyFontProperties(font, fontProperties);
		}

		return font;
	}

	private static NSUrl? GetFontManifestUrl(string actualFontPath)
	{
		var fileName = actualFontPath;
		var fileExtension = "manifest";

		var url = NSBundle
			.MainBundle
			.GetUrlForResource(
				name: Path.GetFileName(fileName),
				fileExtension: fileExtension,
				subdirectory: Path.GetDirectoryName(actualFontPath));

		return url;
	}

	private static UIFont ApplyFontProperties(UIFont font, FontProperties fontProperties)
	{
		font = ApplyWeight(font, fontProperties);
		font = ApplyStyle(font, fontProperties);
		return font;
	}

	private static UIFont ApplyWeight(UIFont font, FontProperties fontProperties)
	{
		if (fontProperties.Weight.Weight == FontWeights.Bold.Weight && !font.FontDescriptor.SymbolicTraits.HasFlag(UIFontDescriptorSymbolicTraits.Bold))
		{
			var descriptor = font.FontDescriptor.CreateWithTraits(font.FontDescriptor.SymbolicTraits | UIFontDescriptorSymbolicTraits.Bold);
			if (descriptor != null)
			{
				font = UIFont.FromDescriptor(descriptor, fontProperties.Size);
			}
			else
			{
				typeof(FontHelper).Log().Error($"Can't apply Bold on font \"{font.Name}\". Make sure the font supports it or use another FontFamily.");
			}
		}

		return font;
	}

	private static UIFont ApplyStyle(UIFont font, FontProperties fontProperties)
	{
		if (fontProperties.Style == FontStyle.Italic && !font.FontDescriptor.SymbolicTraits.HasFlag(UIFontDescriptorSymbolicTraits.Italic))
		{
			var descriptor = font.FontDescriptor.CreateWithTraits(font.FontDescriptor.SymbolicTraits | UIFontDescriptorSymbolicTraits.Italic);
			if (descriptor != null)
			{
				font = UIFont.FromDescriptor(descriptor, fontProperties.Size);
			}
			else
			{
				typeof(FontHelper).Log().Error($"Can't apply Italic on font \"{font.Name}\". Make sure the font supports it or use another FontFamily.");
			}
		}

		return font;
	}

	private static UIFont? GetFontFromFamilyName(nfloat size, string familyName)
	{
		//If only one font exists for this family name, use it. Otherwise we will need to inspect the file for the right font name
		var fontNames = UIFont.FontNamesForFamilyName(familyName);
		return fontNames.Length == 1 ? UIFont.FromName(fontNames[0], size) : null;
	}

	private static UIFont? GetFontFromFile(nfloat size, string file)
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
			if (typeof(FontHelper).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(FontHelper).Log().Debug($"Unable to find font in bundle using {file}");
			}

			return null;
		}

		var fontData = NSData.FromUrl(url);
		if (fontData is null)
		{
			if (typeof(FontHelper).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(FontHelper).Log().Debug($"Unable to load font in bundle using {url}");
			}

			return null;
		}

		//iOS loads UIFonts based on the PostScriptName of the font file
		using var fontProvider = new CGDataProvider(fontData);
		var font = CGFont.CreateFromProvider(fontProvider);

		if (font is not null)
		{
			var result = CoreText.CTFontManager.RegisterGraphicsFont(font, out var error);

			if (result
				|| error?.Code == (nint)CTFontManagerError.DuplicatedName
				|| error?.Code == (nint)CTFontManagerError.AlreadyRegistered
			)
			{
				// Use the font even if the registration failed if the error code
				// reports the fonts have already been registered.

				return UIFont.FromName(font.PostScriptName, size);
			}
			else
			{
				if (typeof(FontHelper).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(FontHelper).Log().Debug($"Unable to register font from {file} ({error})");
				}
			}
		}
		else
		{
			if (typeof(FontHelper).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(FontHelper).Log().Debug($"Unable to create font from {file}");
			}
		}

		return null;
	}
	#endregion

	#region Load System Font
	private static UIFont? GetSystemFont(FontProperties fontProperties, string fontFamilyName)
	{
		//based on Fonts available @ http://iosfonts.com/
		//for Windows parity feature, we will not support FontFamily="HelveticaNeue-Bold" (will ignore Bold and must be set by FontWeight property instead)
		var rootFontFamilyName = fontFamilyName.Split('-').FirstOrDefault();

		if (!rootFontFamilyName.IsNullOrEmpty())
		{
			var font = new StringBuilder(rootFontFamilyName);
			if (fontProperties.Weight != FontWeights.Normal || fontProperties.Style == FontStyle.Italic)
			{
				font.Append('-');
				font.Append(GetFontWeightString(fontProperties.Weight));
				font.Append(GetFontStyleString(fontProperties.Style));
			}

			var updatedFont = UIFont.FromName(font.ToString(), fontProperties.Size);
			if (updatedFont != null)
			{
				return updatedFont;
			}

			if (typeof(FontHelper).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(FontHelper).Log().Warn("Failed to get system font based on " + font);
			}

			return UIFont.FromName(rootFontFamilyName, fontProperties.Size);
		}

		return null;
	}

	private static string GetFontWeightString(FontWeight fontWeight)
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

	private static string GetFontStyleString(FontStyle fontStyle)
	{
		if (fontStyle == FontStyle.Italic)
		{
			return "Italic";
		}
		return string.Empty;
	}
	#endregion
}
