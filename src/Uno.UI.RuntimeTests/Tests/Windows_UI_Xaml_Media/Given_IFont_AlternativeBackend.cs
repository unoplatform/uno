#nullable enable

using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if __SKIA__
using SkiaSharp;
using Uno.UI.Composition.Drawing;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_IFont_AlternativeBackend
	{
#if __SKIA__
		// Proves IFont is backend-neutral: ManagedFont — a SkiaSharp-free implementation that reads glyph outlines
		// straight from the sfnt tables and emits them through the neutral IPathBuilder — produces the same outline
		// the Skia backend does for the same font+glyph, for both simple and composite (accented) glyphs.
		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		public void Alternative_Font_Backend_Matches_Skia_Outline()
		{
			const float size = 64f;

			// Skia is used only as a byte source (like reading a font file); it does no outline work here.
			if (!TryFindGlyfFont(size, out var typeface, out var managed))
			{
				return; // no TrueType 'glyf' font available in this environment
			}

			using (typeface)
			{
				using var skFont = new SKFont(typeface, size) { Hinting = SKFontHinting.None };
				var skiaFont = new SkiaFont(skFont);

				var matched = 0;
				var compositeMatched = 0;
				// Latin letters + accented letters (the accented ones are composite glyphs in most TrueType fonts).
				foreach (var ch in "loHITnx0éñüàçÅ")
				{
					var shaped = skFont.GetGlyphs(ch.ToString());
					if (shaped.Length != 1 || shaped[0] == 0)
					{
						continue; // not present in this font
					}

					var glyph = new ushort[] { shaped[0] };
					var position = new Vector2[] { Vector2.Zero };

					using var alternativeOutline = managed.BuildGlyphRunOutline(glyph, position, 0f);
					using var skiaOutline = skiaFont.BuildGlyphRunOutline(glyph, position, 0f);

					var a = alternativeOutline.Bounds;
					var s = skiaOutline.Bounds;
					if (s.Width <= 0 || s.Height <= 0)
					{
						continue; // no ink (e.g. a space) — nothing to compare
					}

					var composite = managed.IsCompositeGlyph(shaped[0]);
					Console.WriteLine($"[AltFontBackend] '{ch}' composite={composite} skia={s} alt={a}");

					// Two independent readers of the same font, same size, same neutral geometry space: the outlines
					// must land in the same place (small tolerance for curve-bounds rounding).
					const double tol = 1.0;
					Assert.AreEqual(s.Left, a.Left, tol, $"Left mismatch for '{ch}': skia={s}, alt={a}");
					Assert.AreEqual(s.Top, a.Top, tol, $"Top mismatch for '{ch}': skia={s}, alt={a}");
					Assert.AreEqual(s.Right, a.Right, tol, $"Right mismatch for '{ch}': skia={s}, alt={a}");
					Assert.AreEqual(s.Bottom, a.Bottom, tol, $"Bottom mismatch for '{ch}': skia={s}, alt={a}");

					matched++;
					if (composite)
					{
						compositeMatched++;
					}
				}

				Assert.IsTrue(matched >= 2, $"expected to validate several glyphs, only matched {matched}");
				Assert.IsTrue(compositeMatched >= 1, "expected to validate at least one composite (accented) glyph");
			}
		}

		private static bool TryFindGlyfFont(float size, out SKTypeface typeface, out ManagedFont managed)
		{
			foreach (var candidate in EnumerateTypefaces())
			{
				if (candidate is null)
				{
					continue;
				}

				var data = ReadFontBytes(candidate, out var ttcIndex);
				if (data is not null && ManagedFont.TryCreate(data, ttcIndex, size, out managed) && !managed.IsCffFont())
				{
					typeface = candidate;
					Console.WriteLine($"[AltFontBackend] using font '{candidate.FamilyName}'");
					return true;
				}

				candidate.Dispose();
			}

			typeface = null!;
			managed = null!;
			return false;
		}

		private static System.Collections.Generic.IEnumerable<SKTypeface> EnumerateTypefaces()
		{
			yield return SKTypeface.Default;
			yield return SKFontManager.Default.MatchCharacter('H');

			var fontManager = SKFontManager.Default;
			for (var i = 0; i < fontManager.FontFamilyCount; i++)
			{
				yield return fontManager.MatchFamily(fontManager.GetFamilyName(i));
			}
		}

		private static byte[]? ReadFontBytes(SKTypeface typeface, out int ttcIndex)
		{
			using var asset = typeface.OpenStream(out ttcIndex);
			if (asset is null)
			{
				return null;
			}

			var bytes = new byte[asset.Length];
			return asset.Read(bytes, bytes.Length) == bytes.Length ? bytes : null;
		}
#endif
	}
}
