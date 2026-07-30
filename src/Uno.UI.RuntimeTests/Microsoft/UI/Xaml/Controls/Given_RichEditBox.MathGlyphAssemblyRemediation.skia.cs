#nullable enable

using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public void When_Math_Variants_Table_Is_Malformed_Parsing_Is_Bounded()
	{
		var valid = CreateMathVariantsTable();
		Assert.IsTrue(MathFontMetrics.TryReadVerticalConstructionForTesting(valid, 42, out var variants, out var parts));
		Assert.AreEqual(2, variants);
		Assert.AreEqual(3, parts);

		for (var length = 0; length < valid.Length; length++)
		{
			var truncated = new byte[length];
			Array.Copy(valid, truncated, length);
			Assert.IsFalse(MathFontMetrics.TryReadVerticalConstructionForTesting(truncated, 42, out _, out _));
		}

		var invalidCoverage = (byte[])valid.Clone();
		WriteUInt16(invalidCoverage, 14, ushort.MaxValue);
		Assert.IsFalse(MathFontMetrics.TryReadVerticalConstructionForTesting(invalidCoverage, 42, out _, out _));

		var excessivePartCount = (byte[])valid.Clone();
		WriteUInt16(excessivePartCount, 46, ushort.MaxValue);
		Assert.IsFalse(MathFontMetrics.TryReadVerticalConstructionForTesting(excessivePartCount, 42, out _, out _));
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Wasm)]
	public async Task When_Tall_Math_Table_Uses_Available_Math_Font_And_Preserves_Hit_Testing()
	{
		var editor = new RichEditBox
		{
			Width = 500,
			Height = 650,
			FontSize = 32,
			FontFamily = new FontFamily("Cambria Math"),
			TextWrapping = TextWrapping.NoWrap,
			BorderThickness = new Thickness(0),
			Padding = new Thickness(12),
		};
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetMathMode(RichEditMathMode.MathOnly);
			var rows = new StringBuilder();
			for (var index = 0; index < 10; index++)
			{
				rows.Append("<mtr><mtd><mi>x</mi></mtd><mtd><mn>")
					.Append(index)
					.Append("</mn></mtd></mtr>");
			}
			editor.Document.SetMathML(
				$"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mtable>{rows}</mtable></math>");
			await WindowHelper.WaitForIdle();

			var parsed = GetMathLayout(editor, out var block);
			var story = editor.Document.GetRange(0, int.MaxValue).Text;
			var bracket = parsed.GetRectForIndex(0);
			var firstCell = parsed.GetRectForIndex(story.IndexOf("\U0001D465", StringComparison.Ordinal));
			var lastCell = parsed.GetRectForIndex(story.LastIndexOf("\U0001D465", StringComparison.Ordinal));

			if (parsed.UsesOpenTypeMath)
			{
				Assert.IsGreaterThan(0, parsed.VerticalAssemblyGlyphCount);
			}
			else
			{
				Assert.AreEqual(0, parsed.VerticalAssemblyGlyphCount);
			}
			Assert.IsGreaterThanOrEqualTo(lastCell.Bottom - firstCell.Y, bracket.Height);

			var point = block.TransformToVisual(editor).TransformPoint(
				new Point(bracket.X + bracket.Width / 2, bracket.Y + bracket.Height / 2));
			Assert.AreEqual(0, editor.Document.GetRangeFromPoint(point, PointOptions.ClientCoordinates).StartPosition);
			Assert.IsTrue(editor.Document.AreRunIndexesValid());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Wasm)]
	public async Task When_Tall_Math_Table_Uses_Browser_Safe_Vertical_Glyphs()
	{
		var editor = new RichEditBox
		{
			Width = 500,
			Height = 650,
			FontSize = 32,
			FontFamily = new FontFamily("Cambria Math"),
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetMathMode(RichEditMathMode.MathOnly);
			var rows = new StringBuilder();
			for (var index = 0; index < 10; index++)
			{
				rows.Append("<mtr><mtd><mi>x</mi></mtd><mtd><mn>")
					.Append(index)
					.Append("</mn></mtd></mtr>");
			}
			editor.Document.SetMathML(
				$"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mtable>{rows}</mtable></math>");
			await WindowHelper.WaitForIdle();

			var parsed = GetMathLayout(editor, out _);
			var bracket = parsed.GetRectForIndex(0);
			if (parsed.UsesOpenTypeMath)
			{
				Assert.IsGreaterThan(0, parsed.VerticalAssemblyGlyphCount);
			}
			Assert.IsGreaterThan(0, bracket.Height);
			Assert.IsTrue(editor.Document.AreRunIndexesValid());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Fraction_Radical_Uses_Smallest_Math_Variant()
	{
		var editor = new RichEditBox
		{
			Width = 420,
			Height = 260,
			FontSize = 36,
			FontFamily = new FontFamily("Cambria Math"),
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetMathMode(RichEditMathMode.MathOnly);
			editor.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><msqrt><mfrac><mi>a</mi><mi>b</mi></mfrac></msqrt></math>");
			await WindowHelper.WaitForIdle();

			var parsed = GetMathLayout(editor, out _);
			var story = editor.Document.GetRange(0, int.MaxValue).Text;
			var radical = parsed.GetRectForIndex(story.IndexOf('\uFDD0'));
			var numerator = parsed.GetRectForIndex(story.IndexOf("\U0001D44E", StringComparison.Ordinal));
			var denominator = parsed.GetRectForIndex(story.IndexOf("\U0001D44F", StringComparison.Ordinal));

			if (parsed.UsesOpenTypeMath)
			{
				Assert.IsGreaterThan(0, parsed.VerticalVariantGlyphCount + parsed.VerticalAssemblyGlyphCount);
			}
			Assert.IsTrue(radical.Y <= numerator.Y);
			Assert.IsTrue(radical.Bottom >= denominator.Bottom);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static byte[] CreateMathVariantsTable()
	{
		var table = new byte[78];
		WriteUInt16(table, 8, 12);
		WriteUInt16(table, 12, 10);
		WriteUInt16(table, 14, 12);
		WriteUInt16(table, 18, 1);
		WriteUInt16(table, 22, 18);
		WriteUInt16(table, 24, 1);
		WriteUInt16(table, 26, 1);
		WriteUInt16(table, 28, 42);
		WriteUInt16(table, 30, 12);
		WriteUInt16(table, 32, 2);
		WriteUInt16(table, 34, 43);
		WriteUInt16(table, 36, 1000);
		WriteUInt16(table, 38, 44);
		WriteUInt16(table, 40, 2000);
		WriteUInt16(table, 46, 3);
		WritePart(table, 48, 45, 0, 30, 600, 0);
		WritePart(table, 58, 46, 30, 30, 500, 1);
		WritePart(table, 68, 47, 30, 0, 600, 0);
		return table;
	}

	private static void WritePart(
		byte[] table,
		int offset,
		ushort glyph,
		ushort startConnector,
		ushort endConnector,
		ushort advance,
		ushort flags)
	{
		WriteUInt16(table, offset, glyph);
		WriteUInt16(table, offset + 2, startConnector);
		WriteUInt16(table, offset + 4, endConnector);
		WriteUInt16(table, offset + 6, advance);
		WriteUInt16(table, offset + 8, flags);
	}

	private static void WriteUInt16(byte[] table, int offset, ushort value)
	{
		table[offset] = (byte)(value >> 8);
		table[offset + 1] = (byte)value;
	}
}
