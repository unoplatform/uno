#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	private const int CambriaMathUnitsPerEm = 2048;

	// MathConstants records of Cambria Math, as reported by HarfBuzz's hb_ot_math_get_constant.
	private static readonly int[] _cambriaMathConstants =
	{
		73, 60, 3000, 2500, 300, 585, 976, 1250, 418, 760, 320, 750, 615, 239, 460, 300, 765, 85, 133, 500,
		133, 1240, 940, 1550, 770, 1370, 400, 660, 1600, 1180, 133, 133, 1200, 1550, 1030, 1370, 133, 260,
		133, 133, 260, 800, 133, 345, 133, 133, 345, 133, 133, 166, 345, 133, 133, 133, -640, 65,
	};

	[TestMethod]
	public void When_Math_Constants_Are_Read_From_The_Font_Table()
	{
		var font = new MathTableFont(CreateMathConstantsTable(_cambriaMathConstants), CambriaMathUnitsPerEm);

		// A font size equal to the em size makes every scaled value equal its design-unit value.
		var metrics = MathFontMetrics.Create(FontDetails.Create(font, CambriaMathUnitsPerEm));

		Assert.IsTrue(metrics.UsesOpenTypeMath);
		Assert.AreEqual(0.73f, metrics.ScriptScale, 0.0001f);
		Assert.AreEqual(0.60f, metrics.ScriptScriptScale, 0.0001f);
		Assert.AreEqual(585f, metrics.AxisHeight);
		Assert.AreEqual(1200f, metrics.FractionNumeratorShift);
		Assert.AreEqual(1030f, metrics.FractionDenominatorShift);
		Assert.AreEqual(133f, metrics.FractionNumeratorGap);
		Assert.AreEqual(133f, metrics.FractionDenominatorGap);
		Assert.AreEqual(133f, metrics.FractionRuleThickness);
		Assert.AreEqual(750f, metrics.SuperscriptShift);
		Assert.AreEqual(239f, metrics.SuperscriptBottom);
		Assert.AreEqual(418f, metrics.SubscriptShift);
		Assert.AreEqual(760f, metrics.SubscriptTop);
		Assert.AreEqual(300f, metrics.SubSuperscriptGap);
		Assert.AreEqual(85f, metrics.SpaceAfterScript);
		Assert.AreEqual(166f, metrics.RadicalGap);
		Assert.AreEqual(133f, metrics.RadicalRuleThickness);
		Assert.AreEqual(133f, metrics.RadicalExtraAscender);
		Assert.AreEqual(133f, metrics.RadicalKernBeforeDegree);
		Assert.AreEqual(-640f, metrics.RadicalKernAfterDegree);
		Assert.AreEqual(65f, metrics.RadicalDegreeRaisePercent);
	}

	[TestMethod]
	public void When_Math_Constants_Are_Scaled_To_The_Font_Size()
	{
		var font = new MathTableFont(CreateMathConstantsTable(_cambriaMathConstants), CambriaMathUnitsPerEm);

		var metrics = MathFontMetrics.Create(FontDetails.Create(font, CambriaMathUnitsPerEm / 2f));

		Assert.AreEqual(585f / 2, metrics.AxisHeight);
		Assert.AreEqual(-640f / 2, metrics.RadicalKernAfterDegree);
		Assert.AreEqual(0.73f, metrics.ScriptScale, 0.0001f);
		Assert.AreEqual(65f, metrics.RadicalDegreeRaisePercent);
	}

	[TestMethod]
	public void When_Font_Has_No_Math_Table_Constants_Fall_Back()
	{
		var font = new MathTableFont(mathTable: null, CambriaMathUnitsPerEm);

		var metrics = MathFontMetrics.Create(FontDetails.Create(font, 100));

		Assert.IsFalse(metrics.UsesOpenTypeMath);
		Assert.AreEqual(0.7f, metrics.ScriptScale);
		Assert.AreEqual(25f, metrics.AxisHeight);
		Assert.AreEqual(60f, metrics.RadicalDegreeRaisePercent);
	}

	// MATH header (version, MathConstants, MathGlyphInfo, MathVariants offsets) followed by the MathConstants subtable.
	private static byte[] CreateMathConstantsTable(int[] constants)
	{
		const int ConstantsOffset = 10;
		var table = new byte[ConstantsOffset + 214];
		WriteUInt16(table, 0, 1);
		WriteUInt16(table, 4, ConstantsOffset);

		for (var index = 0; index < 4; index++)
		{
			WriteUInt16(table, ConstantsOffset + index * 2, unchecked((ushort)constants[index]));
		}

		// MathValueRecords: int16 value + Offset16 device table (left null).
		for (var index = 4; index <= 54; index++)
		{
			WriteUInt16(table, ConstantsOffset + 8 + (index - 4) * 4, unchecked((ushort)constants[index]));
		}

		WriteUInt16(table, ConstantsOffset + 212, (ushort)constants[55]);
		return table;
	}

	private sealed class MathTableFont(byte[]? mathTable, ushort unitsPerEm) : IFont
	{
		private const uint MathTag = 0x4D415448;
		private const uint HeadTag = 0x68656164;

		public bool TryGetTable(uint tag, [NotNullWhen(true)] out byte[]? data)
		{
			data = tag switch
			{
				MathTag => mathTable,
				HeadTag => CreateHead(),
				_ => null,
			};
			return data is not null;
		}

		private byte[] CreateHead()
		{
			var head = new byte[54];
			WriteUInt16(head, 18, unitsPerEm);
			return head;
		}

		public GlyphRun Shape(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures = true)
			=> new(Array.Empty<ushort>(), Array.Empty<Vector2>(), Array.Empty<float>(), Array.Empty<int>());

		public void BuildGlyphRun(IGeometryFactory geometry, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<GlyphRunElement> elements)
		{
		}

		public float Ascent => -0.8f * unitsPerEm;

		public float Descent => 0.2f * unitsPerEm;

		public float CapHeight => 0.7f * unitsPerEm;

		public float? UnderlinePosition => null;

		public float? UnderlineThickness => null;

		public float? StrikeoutPosition => null;

		public float? StrikeoutThickness => null;

		public ushort GetGlyphIndex(int codepoint) => 0;

		public bool ContainsGlyph(int codepoint) => false;

		public float GetGlyphAdvance(ushort glyph) => 0;

		public string FamilyName => "Math Table Test Font";
	}
}
