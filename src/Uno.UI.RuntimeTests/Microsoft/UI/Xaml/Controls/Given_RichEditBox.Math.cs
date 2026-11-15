#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	private const string MathNamespace = "http://www.w3.org/1998/Math/MathML";

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_MathML_Projection_Uses_Native_Markers()
	{
		var editor = await CreateSharedMathEditor();
		try
		{
			var cases = new Dictionary<string, (string MathML, string Story)>
			{
				["token"] = ("<mi>x</mi>", "\U0001D465\r"),
				["fraction"] = ("<mfrac><mi>a</mi><mi>b</mi></mfrac>", "\uFDD0\U0001D44E\uFDEE\U0001D44F\uFDEF\r"),
				["sub"] = ("<msub><mi>x</mi><mi>i</mi></msub>", "\uFDD0\U0001D465\uFDEE\U0001D456\uFDEF\r"),
				["sup"] = ("<msup><mi>x</mi><mn>2</mn></msup>", "\uFDD0\U0001D465\uFDEE2\uFDEF\r"),
				["subsup"] = ("<msubsup><mi>x</mi><mi>i</mi><mn>2</mn></msubsup>", "\uFDD0\U0001D465\uFDEE\U0001D456\uFDEE2\uFDEF\r"),
				["sqrt"] = ("<msqrt><mi>x</mi></msqrt>", "\uFDD0\uFDEE\U0001D465\uFDEF\r"),
				["root"] = ("<mroot><mi>x</mi><mn>3</mn></mroot>", "\uFDD03\uFDEE\U0001D465\uFDEF\r"),
				["fenced"] = ("<mfenced open=\"[\" close=\"]\"><mi>x</mi><mi>y</mi></mfenced>", "\uFDD0\U0001D465,\U0001D466\uFDEF\r"),
				["matrix"] = ("<mtable><mtr><mtd><mi>a</mi></mtd><mtd><mi>b</mi></mtd></mtr><mtr><mtd><mi>c</mi></mtd><mtd><mi>d</mi></mtd></mtr></mtable>", "\uFDD0\U0001D44E\uFDEE\U0001D44F\uFDEE\U0001D450\uFDEE\U0001D451\uFDEF\r"),
				["mover"] = ("<mover><mi>x</mi><mo>¯</mo></mover>", "\uFDD0\U0001D465\uFDEF\r"),
				["munder"] = ("<munder><mi>x</mi><mo>_</mo></munder>", "\uFDD0\U0001D465\uFDEF\r"),
				["munderover"] = ("<munderover><mo>∑</mo><mi>i</mi><mi>n</mi></munderover>", "\uFDD0\U0001D456\uFDEE\U0001D45B\uFDEE\uFDEF\r"),
				["mmultiscripts"] = ("<mmultiscripts><mi>T</mi><mi>i</mi><mn>2</mn><mprescripts/><mi>j</mi><mn>3</mn></mmultiscripts>", "\uFDD0\U0001D457\uFDEE3\uFDEE\uFDD0\U0001D447\uFDEE\U0001D456\uFDEE2\uFDEF\uFDEF\r"),
			};

			foreach (var (name, item) in cases)
			{
				editor.Document.SetMathML(WrapMath(item.MathML));
				Assert.AreEqual(item.Story, editor.Document.GetRange(0, int.MaxValue).Text, name);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Advanced_MathML_Canonicalizes_Like_Native()
	{
		var editor = await CreateSharedMathEditor();
		try
		{
			AssertCanonical(
				editor,
				"<mover><mi>x</mi><mo>¯</mo></mover>",
				"<mover accent=\"true\"><mi>x</mi><mo>-</mo></mover>");
			AssertCanonical(
				editor,
				"<munder><mi>x</mi><mo>_</mo></munder>",
				"<munder accentunder=\"false\"><mi>x</mi><mo stretchy=\"true\">_</mo></munder>");
			AssertCanonical(
				editor,
				"<munderover><mo>∑</mo><mi>i</mi><mi>n</mi></munderover>",
				"<munderover><mo>∑</mo><mi>i</mi><mi>n</mi></munderover><mrow />");
			AssertCanonical(
				editor,
				"<mmultiscripts><mi>T</mi><mi>i</mi><mn>2</mn><mprescripts/><mi>j</mi><mn>3</mn></mmultiscripts>",
				"<mmultiscripts><mrow><msubsup><mi>T</mi><mi>i</mi><mn>2</mn></msubsup></mrow><mprescripts/><mi>j</mi><mn>3</mn></mmultiscripts>");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Structured_Math_Edits_Match_Native()
	{
		const string source = "<mfrac><mi>ab</mi><mi>cd</mi></mfrac>";
		const string canonicalSource = "<mfrac><mrow><mi>ab</mi></mrow><mrow><mi>cd</mi></mrow></mfrac>";
		var editor = await CreateSharedMathEditor();
		try
		{
			foreach (var edit in new[] { "replace-token", "insert-token", "delete-token", "replace-marker" })
			{
				editor.Document.SetMathML(WrapMath(source));
				editor.Document.ClearUndoRedoHistory();
				var story = editor.Document.GetRange(0, int.MaxValue).Text;
				var numerator = story.IndexOf('a');
				var separator = story.IndexOf('\uFDEE');
				var denominator = story.IndexOf('c');
				switch (edit)
				{
					case "replace-token":
						editor.Document.GetRange(numerator, numerator + 1).Text = "z";
						Assert.AreEqual("\uFDD0zb\uFDEEcd\uFDEF\r", editor.Document.GetRange(0, int.MaxValue).Text);
						AssertMath(editor, "<mfrac><mrow><mi>zb</mi></mrow><mrow><mi>cd</mi></mrow></mfrac>");
						break;
					case "insert-token":
						editor.Document.GetRange(numerator + 1, numerator + 1).Text = "z";
						Assert.AreEqual("\uFDD0azb\uFDEEcd\uFDEF\r", editor.Document.GetRange(0, int.MaxValue).Text);
						AssertMath(editor, "<mfrac><mrow><mi>azb</mi></mrow><mrow><mi>cd</mi></mrow></mfrac>");
						break;
					case "delete-token":
						editor.Document.GetRange(denominator, denominator + 1).Text = string.Empty;
						Assert.AreEqual("\uFDD0ab\uFDEEd\uFDEF\r", editor.Document.GetRange(0, int.MaxValue).Text);
						AssertMath(editor, "<mfrac><mrow><mi>ab</mi></mrow><mi mathvariant=\"normal\">d</mi></mfrac>");
						break;
					case "replace-marker":
						editor.Document.GetRange(separator, separator + 1).Text = "z";
						Assert.AreEqual("z\r", editor.Document.GetRange(0, int.MaxValue).Text);
						editor.Document.GetMathML(out var cleared);
						Assert.AreEqual(string.Empty, cleared);
						break;
				}

				Assert.IsTrue(editor.Document.CanUndo(), edit);
				editor.Document.Undo();
				AssertMath(editor, canonicalSource);
				Assert.IsTrue(editor.Document.CanRedo(), edit);
				editor.Document.Redo();
				Assert.IsFalse(editor.Document.CanRedo(), edit);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_UnicodeMath_Delimiter_Conversion_Matches_Native()
	{
		var editor = await CreateSharedMathEditor();
		try
		{
			var cases = new[]
			{
				(
					"x^2 ",
					"\uFDD0\U0001D465\uFDEE2\uFDEF\r",
					"<msup><mrow><mi mathcolor=\"#000000\">x</mi></mrow><mrow><mn mathcolor=\"#000000\">2</mn></mrow></msup>",
					"\U0001D465^2\r",
					"<mi mathcolor=\"#000000\">x</mi><mo mathcolor=\"#000000\">^</mo><mn mathcolor=\"#000000\">2</mn>"),
				(
					"a/b ",
					"\uFDD0\U0001D44E\uFDEE\U0001D44F\uFDEF\r",
					"<mfrac><mrow><mi mathcolor=\"#000000\">a</mi></mrow><mrow><mi mathcolor=\"#000000\">b</mi></mrow></mfrac>",
					"\U0001D44E/\U0001D44F\r",
					"<mi mathcolor=\"#000000\">a</mi><mo mathcolor=\"#000000\">/</mo><mi mathcolor=\"#000000\">b</mi>"),
				(
					"sqrt(x)",
					"\U0001D460\U0001D45E\U0001D45F\U0001D461(\U0001D465)\r",
					"<mi mathcolor=\"#000000\">s</mi><mi mathcolor=\"#000000\">q</mi><mi mathcolor=\"#000000\">r</mi><mi mathcolor=\"#000000\">t</mi><mo mathcolor=\"#000000\" fence=\"false\">(</mo><mi mathcolor=\"#000000\">x</mi><mo mathcolor=\"#000000\" fence=\"false\">)</mo>",
					null,
					null),
				(
					"sqrt(x) ",
					"\U0001D460\U0001D45E\U0001D45F\U0001D461\uFDD0\U0001D465\uFDEF\r",
					"<mi mathcolor=\"#000000\">s</mi><mi mathcolor=\"#000000\">q</mi><mi mathcolor=\"#000000\">r</mi><mi mathcolor=\"#000000\">t</mi><mfenced><mrow><mi mathcolor=\"#000000\">x</mi></mrow></mfenced>",
					"\U0001D460\U0001D45E\U0001D45F\U0001D461(\U0001D465)\r",
					"<mi mathcolor=\"#000000\">s</mi><mi mathcolor=\"#000000\">q</mi><mi mathcolor=\"#000000\">r</mi><mi mathcolor=\"#000000\">t</mi><mo mathcolor=\"#000000\" fence=\"false\">(</mo><mi mathcolor=\"#000000\">x</mi><mo mathcolor=\"#000000\" fence=\"false\">)</mo>"),
			};

			foreach (var (input, expectedStory, expectedBody, undoStory, undoBody) in cases)
			{
				editor.Document.SetMathMode(RichEditMathMode.NoMath);
				editor.Document.SetMathMode(RichEditMathMode.MathOnly);
				editor.Document.Selection.TypeText(input);
				Assert.AreEqual(expectedStory, editor.Document.GetRange(0, int.MaxValue).Text, input);
				AssertMath(editor, expectedBody);
				if (undoStory is not null)
				{
					Assert.IsTrue(editor.Document.CanUndo(), input);
					editor.Document.Undo();
					Assert.AreEqual(undoStory, editor.Document.GetRange(0, int.MaxValue).Text, input);
					AssertMath(editor, undoBody!);
					Assert.IsTrue(editor.Document.CanRedo(), input);
					editor.Document.Redo();
					Assert.AreEqual(expectedStory, editor.Document.GetRange(0, int.MaxValue).Text, input);
					AssertMath(editor, expectedBody);
				}
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static async Task<RichEditBox> CreateSharedMathEditor()
	{
		var editor = new RichEditBox();
		WindowHelper.WindowContent = editor;
		await WindowHelper.WaitForLoaded(editor);
		editor.Document.SetMathMode(RichEditMathMode.MathOnly);
		return editor;
	}

	private static void AssertCanonical(RichEditBox editor, string sourceBody, string expectedBody)
	{
		editor.Document.SetMathML(WrapMath(sourceBody));
		AssertMath(editor, expectedBody);
	}

	private static void AssertMath(RichEditBox editor, string expectedBody)
	{
		editor.Document.GetMathML(out var actual);
		Assert.AreEqual(
			NormalizeMath(XDocument.Parse(WrapMath(expectedBody)).Root!),
			NormalizeMath(XDocument.Parse(actual).Root!),
			$"Expected: {WrapMath(expectedBody)}{Environment.NewLine}Actual: {actual}");
	}

	private static string WrapMath(string body)
		=> $"<math xmlns=\"{MathNamespace}\" display=\"block\">{body}</math>";

	private static string NormalizeMath(XElement element)
	{
		var attributes = string.Concat(
			element.Attributes()
				.Where(attribute => !attribute.IsNamespaceDeclaration)
				.OrderBy(attribute => attribute.Name.LocalName, StringComparer.Ordinal)
				.Select(attribute => $" {attribute.Name.LocalName}=\"{attribute.Value}\""));
		var content = string.Concat(
			element.Nodes().Select(node => node switch
			{
				XElement child => NormalizeMath(child),
				XText text => text.Value,
				_ => string.Empty,
			}));
		return $"<{element.Name.LocalName}{attributes}>{content}</{element.Name.LocalName}>";
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[RunsOnUIThread]
	public void When_MathML_Rejects_Malformed_Or_Overly_Complex_Structures()
	{
		var richEditBox = new RichEditBox();
		richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);
		const string valid = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mi>seed</mi></math>";
		var deep = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\">"
			+ string.Concat(Enumerable.Repeat("<mrow>", 65))
			+ "<mi>x</mi>"
			+ string.Concat(Enumerable.Repeat("</mrow>", 65))
			+ "</math>";
		var tooManyNodes = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\">"
			+ string.Concat(Enumerable.Repeat("<mi>x</mi>", 4097))
			+ "</math>";
		var tooManyRows = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mtable>"
			+ string.Concat(Enumerable.Repeat("<mtr><mtd><mi>x</mi></mtd></mtr>", 65))
			+ "</mtable></math>";
		var tooManyColumns = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mtable><mtr>"
			+ string.Concat(Enumerable.Repeat("<mtd><mi>x</mi></mtd>", 65))
			+ "</mtr></mtable></math>";

		foreach (var invalid in new[]
		{
			"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mfrac><mi>x</mi></mfrac></math>",
			deep,
			tooManyNodes,
			tooManyRows,
			tooManyColumns,
		})
		{
			richEditBox.Document.SetMathML(valid);
			richEditBox.Document.GetMathML(out var beforeFailure);
			richEditBox.Document.Selection.SetRange(0, 4);
			richEditBox.Document.ClearUndoRedoHistory();
			Assert.ThrowsExactly<ArgumentException>(() => richEditBox.Document.SetMathML(invalid));
			richEditBox.Document.GetMathML(out var afterFailure);
			Assert.AreEqual(beforeFailure, afterFailure);
			Assert.AreEqual(0, richEditBox.Document.Selection.StartPosition);
			Assert.AreEqual(4, richEditBox.Document.Selection.EndPosition);
			Assert.IsFalse(richEditBox.Document.CanUndo());
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_UnicodeMath_Character_Typing_Uses_A_Conversion_Boundary()
	{
		var richEditBox = new RichEditBox();
		richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);
		foreach (var character in "x^2 ")
		{
			richEditBox.Document.Selection.TypeText(character.ToString());
		}

		richEditBox.Document.GetMathML(out var converted);
		Assert.IsTrue(XDocument.Parse(converted).Descendants().Any(element => element.Name.LocalName == "msup"));
		richEditBox.Document.Undo();
		Assert.AreEqual("\U0001D465^2\r", richEditBox.Document.GetRange(0, int.MaxValue).Text);
		richEditBox.Document.GetMathML(out var linear);
		Assert.IsFalse(XDocument.Parse(linear).Descendants().Any(element => element.Name.LocalName == "msup"));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Fraction_Layout_Renders_A_Bar_With_Ink_Above_And_Below()
	{
		var richEditBox = CreateMathEditor();
		richEditBox.Width = 320;
		richEditBox.Height = 200;
		richEditBox.FontSize = 48;
		try
		{
			WindowHelper.WindowContent = richEditBox;
			await WindowHelper.WaitForLoaded(richEditBox);
			richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);
			richEditBox.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mfrac><mi>abc</mi><mi>xyz</mi></mfrac></math>");
			await WindowHelper.WaitForIdle();

			var bitmap = await UITestHelper.ScreenShot(richEditBox);
			var longestRun = 0;
			var barRow = 0;
			for (var y = 0; y < bitmap.Height; y++)
			{
				var current = 0;
				for (var x = 0; x < bitmap.Width; x++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if (pixel is { A: > 200, R: < 90, G: < 90, B: < 90 })
					{
						current++;
						if (current > longestRun)
						{
							longestRun = current;
							barRow = y;
						}
					}
					else
					{
						current = 0;
					}
				}
			}

			Assert.IsGreaterThan(20, longestRun);
			Assert.IsGreaterThan(20, CountMathDarkPixels(bitmap, 0, Math.Max(0, barRow - 2)));
			Assert.IsGreaterThan(20, CountMathDarkPixels(bitmap, Math.Min(bitmap.Height, barRow + 3), bitmap.Height));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static RichEditBox CreateMathEditor()
		=> new()
		{
			Width = 500,
			Height = 220,
			FontSize = 36,
			TextWrapping = TextWrapping.NoWrap,
			BorderThickness = new Thickness(0),
			Padding = new Thickness(12),
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black),
		};

	private static void AssertBounded(Rect rect)
	{
		Assert.IsFalse(double.IsNaN(rect.X) || double.IsInfinity(rect.X));
		Assert.IsFalse(double.IsNaN(rect.Y) || double.IsInfinity(rect.Y));
		Assert.IsTrue(rect.Width is >= 0 and < 5_000);
		Assert.IsTrue(rect.Height is >= 0 and < 5_000);
	}

	private static int CountMathDarkPixels(RawBitmap bitmap, int startY, int endY)
	{
		var count = 0;
		for (var y = startY; y < endY; y++)
		{
			for (var x = 0; x < bitmap.Width; x++)
			{
				var pixel = bitmap.GetPixel(x, y);
				if (pixel is { A: > 200, R: < 90, G: < 90, B: < 90 })
				{
					count++;
				}
			}
		}

		return count;
	}
}
