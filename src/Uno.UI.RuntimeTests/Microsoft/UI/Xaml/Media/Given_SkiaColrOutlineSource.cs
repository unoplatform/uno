#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;

#if __SKIA__
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_SkiaColrOutlineSource
	{
#if __SKIA__
		// A COLR v1 table with a v0 base glyph record and a v1 base glyph paint record; unitsPerEm 1000. Glyph ids:
		// 1 a plain square (100,0)-(700,600), 2 a plain triangle in (100,0)-(500,400), 3 the v0 colour glyph and 4 the v1 colour glyph,
		// both with a monochrome fallback diamond in (100,0)-(700,700), 5 and 6 the colour layers.
		internal const string ColrFontUri = "ms-appx:///Uno.UI.RuntimeTests/Assets/Fonts/colr-mixed-glyphs.ttf";

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26670")]
		public async Task When_Colr_Font_Then_Color_Glyphs_Are_Its_Base_Glyphs()
		{
			using var data = SKData.CreateCopy(await AppPackageAssetHelper.ReadAllBytesAsync(ColrFontUri));
			using var typeface = SKTypeface.FromData(data, 0);
			var source = SkiaColrOutlineSource.Create(typeface);
			Assert.IsNotNull(source);
			using var copy = source.Typeface;

			var colorGlyphs = new List<int>();
			for (var glyph = 0; glyph <= ushort.MaxValue; glyph++)
			{
				if (source.IsColorGlyph((ushort)glyph))
				{
					colorGlyphs.Add(glyph);
				}
			}

			CollectionAssert.AreEqual(new[] { 3, 4 }, colorGlyphs);
		}

		// The copy is a plain font: it outlines the plain glyphs, and the colour glyphs as their monochrome fallbacks.
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26670")]
		[DataRow((ushort)1, 100f, 0f, 700f, 600f)]
		[DataRow((ushort)2, 100f, 0f, 500f, 400f)]
		[DataRow((ushort)3, 100f, 0f, 700f, 700f)]
		[DataRow((ushort)4, 100f, 0f, 700f, 700f)]
		public async Task When_Colr_Font_Then_Copy_Outlines_Glyph(ushort glyph, float left, float bottom, float right, float top)
		{
			using var data = SKData.CreateCopy(await AppPackageAssetHelper.ReadAllBytesAsync(ColrFontUri));
			using var typeface = SKTypeface.FromData(data, 0);
			var source = SkiaColrOutlineSource.Create(typeface);
			Assert.IsNotNull(source);
			using var copy = source.Typeface;

			// At unitsPerEm with no hinting, the outline is in font units (y down).
			using var font = new SKFont(copy, 1000) { Hinting = SKFontHinting.None };
			using var path = font.GetGlyphPath(glyph);
			Assert.IsNotNull(path, "expected the copy to outline the glyph");

			const float tolerance = 0.5f;
			var bounds = path.Bounds;
			Assert.AreEqual(left, bounds.Left, tolerance);
			Assert.AreEqual(-top, bounds.Top, tolerance);
			Assert.AreEqual(right, bounds.Right, tolerance);
			Assert.AreEqual(-bottom, bounds.Bottom, tolerance);
		}
#endif
	}
}
