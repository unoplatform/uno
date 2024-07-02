#nullable enable
using System;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using SkiaSharp;
using Uno;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Windows.ApplicationModel;
using Windows.UI.Text;
using Uno.UI;

namespace Windows.UI.Xaml.Documents.TextFormatting;

internal static class FontDetailsCache
{
	// TODO: Investigate best value to use here. SKShaper uses a constant 512 scale, Avalonia uses default font scale. Not 100% sure how much difference it
	// makes here but it affects subpixel rendering accuracy. Performance does not seem to be affected by changing this value.
	private const int FontScale = 512;

	private static readonly Func<string?, float, FontWeight, FontStyle, FontDetails> _getFont =
		Funcs.CreateMemoized<string?, float, FontWeight, FontStyle, FontDetails>(
			(nm, sz, wt, sl) => GetFontInternal(nm, sz, wt, sl));

	private static FontDetails GetFontInternal(
			string? name,
			float fontSize,
			FontWeight weight,
			FontStyle style)
	{
		var skWeight = weight.ToSkiaWeight();
		// TODO: FontStretch not supported by Uno yet
		// var skWidth = FontStretch.ToSkiaWidth();
		var skWidth = SKFontStyleWidth.Normal;
		var skSlant = style.ToSkiaSlant();

		SKTypeface? skTypeFace;

		SKTypeface GetDefaultTypeFace()
		{
			return SKTypeface.FromFamilyName(FeatureConfiguration.Font.DefaultTextFontFamily, skWeight, skWidth, skSlant)
				?? SKTypeface.FromFamilyName(null, skWeight, skWidth, skSlant)
				?? SKTypeface.FromFamilyName(null);
		}

		if (name == null || string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
		{
			skTypeFace = GetDefaultTypeFace();
		}
		else if (XamlFilePathHelper.TryGetMsAppxAssetPath(name, out var path))
		{
			var filePath = global::System.IO.Path.Combine(
				Package.Current.InstalledLocation.Path
				, path.Replace('/', global::System.IO.Path.DirectorySeparatorChar));

			// SKTypeface.FromFile may return null if the file is not found (SkiaSharp is not yet nullable attributed)
			skTypeFace = SKTypeface.FromFile(filePath);
		}
		else
		{
			// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
			skTypeFace = SKTypeface.FromFamilyName(name, skWeight, skWidth, skSlant);
		}

		if (skTypeFace == null)
		{
			if (typeof(Inline).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(Inline).Log().LogWarning($"The font {name} could not be found, using system default");
			}

			skTypeFace = GetDefaultTypeFace();
		}

		Blob? GetTable(Face face, Tag tag)
		{
			var size = skTypeFace.GetTableSize(tag);

			if (size == 0)
			{
				return null;
			}

			var data = Marshal.AllocHGlobal(size);

			var releaseDelegate = new ReleaseDelegate(() => Marshal.FreeHGlobal(data));

			var value = skTypeFace.TryGetTableData(tag, 0, size, data) ?
				new Blob(data, size, MemoryMode.Writeable, releaseDelegate) : null;

			return value;
		}

		var skFont = new SKFont(skTypeFace, fontSize);
		skFont.Edging = SKFontEdging.SubpixelAntialias;
		skFont.Subpixel = true;

		var hbFace = new Face(GetTable);
		hbFace.UnitsPerEm = skTypeFace.UnitsPerEm;

		var hbFont = new Font(hbFace);
		hbFont.SetScale(FontScale, FontScale);
		hbFont.SetFunctionsOpenType();

		return new(skFont, skFont.Size, skFont.ScaleX, skFont.Metrics, skTypeFace, hbFont, hbFace);
	}

	public static FontDetails GetFont(
			string? name,
			float fontSize,
			FontWeight weight,
			FontStyle style
		) => _getFont(name, fontSize, weight, style);
}
