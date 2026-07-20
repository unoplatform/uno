#nullable enable

using System;
using System.Collections.Generic;
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
		// Proves IFont is backend-neutral: an alternative implementation, backed by a hand-rolled managed
		// TrueType 'glyf' parser (no SkiaSharp in the outline path), builds glyph geometry purely through the
		// neutral IPathBuilder — and produces the same outline the Skia backend does for the same font+glyph.
		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		public void Alternative_Font_Backend_Matches_Skia_Outline()
		{
			const float size = 64f;

			// Skia is used only as a byte source (like reading a font file); it does no outline work here.
			if (!TryFindGlyfFont(size, out var typeface, out var managed))
			{
				// No TrueType 'glyf' font available in this environment — nothing to exercise.
				return;
			}

			Console.WriteLine($"[AltFontBackend] using glyf font '{typeface.FamilyName}'");

			using (typeface)
			{
				using var skFont = new SKFont(typeface, size) { Hinting = SKFontHinting.None };
				var skiaFont = new SkiaFont(skFont);

				foreach (var ch in "loHIThnx0")
				{
					var shaped = skFont.GetGlyphs(ch.ToString());
					if (shaped.Length != 1 || shaped[0] == 0 || !managed.IsSimpleNonEmpty(shaped[0]))
					{
						continue; // missing, composite, or empty in this font — try another glyph
					}

					var glyph = new ushort[] { shaped[0] };
					var position = new Vector2[] { Vector2.Zero };

					using var alternativeOutline = managed.BuildGlyphRunOutline(glyph, position, 0f);
					using var skiaOutline = skiaFont.BuildGlyphRunOutline(glyph, position, 0f);

					var a = alternativeOutline.Bounds;
					var s = skiaOutline.Bounds;

					Assert.IsTrue(a.Width > 0 && a.Height > 0, $"alternative backend produced an empty outline for '{ch}' ({a})");
					Assert.IsTrue(s.Width > 0 && s.Height > 0, $"skia backend produced an empty outline for '{ch}' ({s})");

					Console.WriteLine($"[AltFontBackend] glyph '{ch}' skia={s} alt={a}");

					// Two independent parsers of the same font, same size, same neutral geometry space:
					// the outlines must land in the same place (small tolerance for curve-bounds rounding).
					const double tol = 1.0;
					Assert.AreEqual(s.Left, a.Left, tol, $"Left mismatch for '{ch}': skia={s}, alt={a}");
					Assert.AreEqual(s.Top, a.Top, tol, $"Top mismatch for '{ch}': skia={s}, alt={a}");
					Assert.AreEqual(s.Right, a.Right, tol, $"Right mismatch for '{ch}': skia={s}, alt={a}");
					Assert.AreEqual(s.Bottom, a.Bottom, tol, $"Bottom mismatch for '{ch}': skia={s}, alt={a}");
					return; // validated one glyph end-to-end through the alternative backend
				}
			}
		}

		private static bool TryFindGlyfFont(float size, out SKTypeface typeface, out ManagedTrueTypeFont managed)
		{
			foreach (var candidate in EnumerateTypefaces())
			{
				if (candidate is null)
				{
					continue;
				}

				var data = ReadFontBytes(candidate, out var ttcIndex);
				if (data is not null && ManagedTrueTypeFont.TryCreate(data, ttcIndex, size, out managed))
				{
					typeface = candidate;
					return true;
				}

				candidate.Dispose();
			}

			typeface = null!;
			managed = null!;
			return false;
		}

		private static IEnumerable<SKTypeface> EnumerateTypefaces()
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

		/// <summary>
		/// A minimal, dependency-free TrueType 'glyf' outline reader that implements <see cref="IFont"/> without
		/// any SkiaSharp: glyph contours are read straight from the font tables and emitted through the neutral
		/// <see cref="IPathBuilder"/>. Simple (non-composite) glyphs only — enough to validate the abstraction.
		/// </summary>
		internal sealed class ManagedTrueTypeFont : IFont
		{
			private readonly byte[] _data;
			private readonly int _glyf;
			private readonly int _loca;
			private readonly int _unitsPerEm;
			private readonly int _numGlyphs;
			private readonly bool _longLoca;
			private readonly float _pixelSize;

			private ManagedTrueTypeFont(byte[] data, int glyf, int loca, int head, int maxp, float pixelSize)
			{
				_data = data;
				_glyf = glyf;
				_loca = loca;
				_pixelSize = pixelSize;
				_unitsPerEm = U16(data, head + 18);
				_longLoca = U16(data, head + 50) == 1;
				_numGlyphs = U16(data, maxp + 4);
			}

			public static bool TryCreate(byte[] data, int ttcIndex, float pixelSize, out ManagedTrueTypeFont font)
			{
				font = null!;
				var baseOffset = 0;
				if (data.Length >= 16 && U32(data, 0) == 0x74746366) // 'ttcf' — font collection
				{
					var count = (int)U32(data, 8);
					baseOffset = (int)U32(data, 12 + (ttcIndex >= 0 && ttcIndex < count ? ttcIndex : 0) * 4);
				}

				var numTables = U16(data, baseOffset + 4);
				int glyf = 0, loca = 0, head = 0, maxp = 0;
				var dir = baseOffset + 12;
				for (var i = 0; i < numTables; i++, dir += 16)
				{
					var offset = (int)U32(data, dir + 8);
					switch (U32(data, dir))
					{
						case 0x676C7966: glyf = offset; break; // 'glyf'
						case 0x6C6F6361: loca = offset; break; // 'loca'
						case 0x68656164: head = offset; break; // 'head'
						case 0x6D617870: maxp = offset; break; // 'maxp'
					}
				}

				if (glyf == 0 || loca == 0 || head == 0 || maxp == 0)
				{
					return false; // not a TrueType 'glyf' font (e.g. CFF/OpenType-PostScript)
				}

				font = new ManagedTrueTypeFont(data, glyf, loca, head, maxp, pixelSize);
				return true;
			}

			public bool HasColorGlyphs => false;

			public void AppendColorGlyphImages(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<PositionedGlyphImage> output)
			{
			}

			public bool IsSimpleNonEmpty(ushort glyph)
			{
				if (!TryGetGlyphSlice(glyph, out var start, out var end) || end <= start)
				{
					return false;
				}

				return S16(_data, _glyf + start) > 0; // numberOfContours; < 0 means composite
			}

			public IGeometry BuildGlyphRunOutline(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY)
			{
				var scale = _pixelSize / _unitsPerEm;
				var builder = DrawingBackend.Current.CreatePathBuilder();
				for (var i = 0; i < glyphs.Length; i++)
				{
					// Font units are Y-up with the origin at the baseline; screen space is Y-down.
					EmitGlyph(builder, glyphs[i], positions[i].X, positions[i].Y + baselineY, scale);
				}

				return builder.Build();
			}

			private bool TryGetGlyphSlice(ushort glyph, out int start, out int end)
			{
				start = end = 0;
				if (glyph >= _numGlyphs)
				{
					return false;
				}

				if (_longLoca)
				{
					start = (int)U32(_data, _loca + glyph * 4);
					end = (int)U32(_data, _loca + (glyph + 1) * 4);
				}
				else
				{
					start = U16(_data, _loca + glyph * 2) * 2;
					end = U16(_data, _loca + (glyph + 1) * 2) * 2;
				}

				return true;
			}

			private void EmitGlyph(IPathBuilder builder, ushort glyph, float originX, float originY, float scale)
			{
				if (!TryGetGlyphSlice(glyph, out var start, out var end) || end <= start)
				{
					return; // empty glyph (e.g. space)
				}

				var p = _glyf + start;
				var numContours = S16(_data, p);
				p += 2;
				p += 8; // skip xMin/yMin/xMax/yMax
				if (numContours <= 0)
				{
					return; // composite glyph — out of scope for this validation reader
				}

				var endPts = new int[numContours];
				for (var c = 0; c < numContours; c++, p += 2)
				{
					endPts[c] = U16(_data, p);
				}

				var numPoints = endPts[numContours - 1] + 1;
				var instructionLength = U16(_data, p);
				p += 2 + instructionLength;

				var flags = new byte[numPoints];
				for (var i = 0; i < numPoints;)
				{
					var flag = _data[p++];
					flags[i++] = flag;
					if ((flag & 0x08) != 0) // REPEAT_FLAG
					{
						var repeat = _data[p++];
						while (repeat-- > 0 && i < numPoints)
						{
							flags[i++] = flag;
						}
					}
				}

				var xs = new int[numPoints];
				var x = 0;
				for (var i = 0; i < numPoints; i++)
				{
					var flag = flags[i];
					if ((flag & 0x02) != 0) // X_SHORT_VECTOR
					{
						var dx = _data[p++];
						x += (flag & 0x10) != 0 ? dx : -dx;
					}
					else if ((flag & 0x10) == 0) // not X_IS_SAME
					{
						x += S16(_data, p);
						p += 2;
					}

					xs[i] = x;
				}

				var ys = new int[numPoints];
				var y = 0;
				for (var i = 0; i < numPoints; i++)
				{
					var flag = flags[i];
					if ((flag & 0x04) != 0) // Y_SHORT_VECTOR
					{
						var dy = _data[p++];
						y += (flag & 0x20) != 0 ? dy : -dy;
					}
					else if ((flag & 0x20) == 0) // not Y_IS_SAME
					{
						y += S16(_data, p);
						p += 2;
					}

					ys[i] = y;
				}

				var contourStart = 0;
				for (var c = 0; c < numContours; c++)
				{
					EmitContour(builder, flags, xs, ys, contourStart, endPts[c], originX, originY, scale);
					contourStart = endPts[c] + 1;
				}
			}

			private static void EmitContour(IPathBuilder builder, byte[] flags, int[] xs, int[] ys, int first, int last, float originX, float originY, float scale)
			{
				var n = last - first + 1;
				if (n <= 0)
				{
					return;
				}

				Vector2 Point(int k)
				{
					var idx = first + ((k % n) + n) % n;
					return new Vector2(originX + xs[idx] * scale, originY - ys[idx] * scale);
				}

				bool OnCurve(int k) => (flags[first + ((k % n) + n) % n] & 0x01) != 0;

				// TrueType allows an all-off-curve contour; then the start point is the implied midpoint.
				var startK = -1;
				for (var k = 0; k < n; k++)
				{
					if (OnCurve(k))
					{
						startK = k;
						break;
					}
				}

				if (startK == -1)
				{
					var implied = Vector2.Lerp(Point(n - 1), Point(0), 0.5f);
					builder.MoveTo(implied);
					var previousOff = Point(0);
					for (var k = 1; k <= n; k++)
					{
						var current = Point(k);
						builder.QuadraticTo(previousOff, Vector2.Lerp(previousOff, current, 0.5f));
						previousOff = current;
					}

					builder.Close();
					return;
				}

				var startPoint = Point(startK);
				builder.MoveTo(startPoint);

				var haveOff = false;
				var off = default(Vector2);
				for (var k = 1; k <= n; k++)
				{
					var index = startK + k;
					var current = Point(index);
					if (OnCurve(index))
					{
						if (haveOff)
						{
							builder.QuadraticTo(off, current);
							haveOff = false;
						}
						else
						{
							builder.LineTo(current);
						}
					}
					else
					{
						if (haveOff)
						{
							builder.QuadraticTo(off, Vector2.Lerp(off, current, 0.5f)); // implied on-curve midpoint
						}

						off = current;
						haveOff = true;
					}
				}

				builder.Close();
			}

			private static int U16(byte[] d, int o) => (d[o] << 8) | d[o + 1];
			private static short S16(byte[] d, int o) => (short)U16(d, o);
			private static uint U32(byte[] d, int o) => ((uint)d[o] << 24) | ((uint)d[o + 1] << 16) | ((uint)d[o + 2] << 8) | d[o + 3];
		}
#endif
	}
}
