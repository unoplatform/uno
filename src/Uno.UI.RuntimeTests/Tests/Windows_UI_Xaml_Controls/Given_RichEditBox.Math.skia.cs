#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
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
	[RunsOnUIThread]
	public void When_MathML_AST_Canonicalizes_And_Maps_Atoms()
	{
		const string source = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mrow>"
			+ "<mfrac><mi> a </mi><mn> 2 </mn></mfrac><mo> - </mo>"
			+ "<mroot><mi>x</mi><mn>3</mn></mroot>"
			+ "<msubsup><mi>y</mi><mi>i</mi><mn>2</mn></msubsup>"
			+ "<mfenced open=\"[\" close=\"]\" separators=\" ; \"><mi>p</mi><mi>q</mi></mfenced>"
			+ "<mtable><mtr><mtd><mi>a</mi></mtd><mtd><mi>b</mi></mtd></mtr>"
			+ "<mtr><mtd><mi>c</mi></mtd><mtd><mi>d</mi></mtd></mtr></mtable>"
			+ "<unsupported><mi>z</mi></unsupported>"
			+ "</mrow></math>";
		var richEditBox = new RichEditBox();
		richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);
		richEditBox.Document.SetMathML(source);

		Assert.IsTrue(richEditBox.Document.MathProjection!.Contains('\uFDD0'));
		Assert.IsTrue(richEditBox.Document.MathProjection.Contains('\uFDEE'));
		Assert.IsTrue(richEditBox.Document.MathProjection.Contains('\uFDEF'));
		Assert.IsTrue(richEditBox.Document.MathProjection.Contains("\U0001D44E", StringComparison.Ordinal));
		Assert.IsNotNull(richEditBox.Document.StructuredMath);
		foreach (var atom in richEditBox.Document.MathAtoms)
		{
			Assert.AreEqual(
				atom.Atom.ProjectionText,
				richEditBox.Document.MathProjection.Substring(atom.Span.Start, atom.Span.Length));
		}

		richEditBox.Document.GetMathML(out var canonicalText);
		var canonical = XDocument.Parse(canonicalText);
		Assert.AreEqual("block", canonical.Root?.Attribute("display")?.Value);
		Assert.IsFalse(canonical.Descendants().Any(element => element.Name.LocalName == "unsupported"));
		Assert.IsTrue(canonical.Descendants().Any(element => element.Name.LocalName == "mo" && element.Value == "−"));

		richEditBox.Document.SetMathML(
			"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mo>(</mo><mfrac><mi>x</mi><mi>y</mi></mfrac><mo>)</mo></math>");
		richEditBox.Document.GetMathML(out var explicitFenceCanonical);
		Assert.IsTrue(
			XDocument.Parse(explicitFenceCanonical).Descendants().Any(element => element.Name.LocalName == "mfenced"),
			"Matching explicit fence operators should canonicalize to a scalable fenced node.");
	}

	[TestMethod]
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
	public void When_Large_Math_Fragment_Allocates_Format_State_Per_Run()
	{
		const int textLength = 262_000;
		var richEditBox = new RichEditBox();
		richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);
		var math = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mtext>"
			+ new string('x', textLength)
			+ "</mtext></math>";

		var (_, clones) = TrackFormattingClones(() =>
		{
			richEditBox.Document.SetMathML(math);
			return true;
		});

		Assert.AreEqual(textLength, richEditBox.Document.TextLength);
		Assert.AreEqual(1, richEditBox.Document.CharacterRunCount);
		Assert.AreEqual(1, richEditBox.Document.ParagraphRunCount);
		Assert.IsLessThan(32, clones.Character);
		Assert.IsLessThan(32, clones.Paragraph);
		Assert.IsTrue(richEditBox.Document.AreRunIndexesValid());
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Structured_Token_And_Marker_Edits_Preserve_Or_Clear_The_AST()
	{
		const string source = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mfrac><mi>ab</mi><mi>cd</mi></mfrac></math>";
		var richEditBox = new RichEditBox();
		richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);
		richEditBox.Document.SetMathML(source);
		var story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
		var numerator = story.IndexOf('a');
		richEditBox.Document.GetRange(numerator, numerator + 1).Text = "z";
		Assert.IsNotNull(richEditBox.Document.StructuredMath);
		Assert.IsTrue(richEditBox.Document.AreRunIndexesValid());

		richEditBox.Document.SetMathML(source);
		richEditBox.Document.ClearUndoRedoHistory();
		story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
		var separator = story.IndexOf('\uFDEE');
		richEditBox.Document.GetRange(separator, separator + 1).Text = "z";
		Assert.IsNull(richEditBox.Document.StructuredMath);
		richEditBox.Document.GetMathML(out var cleared);
		Assert.AreEqual(string.Empty, cleared);
		richEditBox.Document.Undo();
		Assert.IsNotNull(richEditBox.Document.StructuredMath);
		Assert.IsTrue(richEditBox.Document.AreRunIndexesValid());
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Advanced_Math_Token_Edits_Stay_Structured()
	{
		var richEditBox = new RichEditBox();
		richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);
		var cases = new[]
		{
			("<mover><mi>x</mi><mo>¯</mo></mover>", "\U0001D465", "y", "mover", "y"),
			("<munderover><mo>∑</mo><mi>i</mi><mi>n</mi></munderover>", "\U0001D456", "k", "munderover", "k"),
			("<mmultiscripts><mi>T</mi><mi>i</mi><mn>2</mn><mprescripts/><mi>j</mi><mn>3</mn></mmultiscripts>", "\U0001D457", "k", "mmultiscripts", "k"),
		};

		foreach (var (body, projectedToken, replacement, structure, expectedToken) in cases)
		{
			richEditBox.Document.SetMathML(
				$"<math xmlns=\"http://www.w3.org/1998/Math/MathML\">{body}</math>");
			var story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
			var tokenIndex = story.IndexOf(projectedToken, StringComparison.Ordinal);
			richEditBox.Document.GetRange(tokenIndex, tokenIndex + projectedToken.Length).Text = replacement;
			Assert.IsNotNull(richEditBox.Document.StructuredMath);
			richEditBox.Document.GetMathML(out var mathML);
			var document = XDocument.Parse(mathML);
			Assert.IsTrue(document.Descendants().Any(element => element.Name.LocalName == structure));
			Assert.IsTrue(document.Descendants().Any(element => element.Value == expectedToken));
			Assert.IsTrue(richEditBox.Document.AreRunIndexesValid());
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
	public async Task When_Structured_Math_Layout_Maps_Core_Markers()
	{
		var richEditBox = CreateMathEditor();
		try
		{
			WindowHelper.WindowContent = richEditBox;
			await WindowHelper.WaitForLoaded(richEditBox);
			richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);

			richEditBox.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mfrac><mi>a</mi><mi>b</mi></mfrac></math>");
			await WindowHelper.WaitForIdle();
			var parsed = GetMathLayout(richEditBox, out var block);
			var story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
			var numeratorIndex = story.IndexOf("\U0001D44E", StringComparison.Ordinal);
			var separatorIndex = story.IndexOf('\uFDEE');
			var denominatorIndex = story.IndexOf("\U0001D44F", StringComparison.Ordinal);
			var numerator = parsed.GetRectForIndex(numeratorIndex);
			var fractionBar = parsed.GetRectForIndex(separatorIndex);
			var denominator = parsed.GetRectForIndex(denominatorIndex);
			Assert.IsTrue(numerator.Y < denominator.Y);
			Assert.IsGreaterThan(10, fractionBar.Width);
			Assert.IsGreaterThan(0, fractionBar.Height);

			var numeratorPoint = block.TransformToVisual(richEditBox).TransformPoint(
				new Point(numerator.X + numerator.Width / 4, numerator.Y + numerator.Height / 2));
			var denominatorPoint = block.TransformToVisual(richEditBox).TransformPoint(
				new Point(denominator.X + denominator.Width / 4, denominator.Y + denominator.Height / 2));
			Assert.AreEqual(
				numeratorIndex,
				richEditBox.Document.GetRangeFromPoint(numeratorPoint, PointOptions.ClientCoordinates).StartPosition);
			Assert.AreEqual(
				denominatorIndex,
				richEditBox.Document.GetRangeFromPoint(denominatorPoint, PointOptions.ClientCoordinates).StartPosition);

			richEditBox.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mroot><mi>x</mi><mn>3</mn></mroot></math>");
			await WindowHelper.WaitForIdle();
			parsed = GetMathLayout(richEditBox, out _);
			story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
			var radical = parsed.GetRectForIndex(story.IndexOf('\uFDD0'));
			var degree = parsed.GetRectForIndex(story.IndexOf('3'));
			var radicand = parsed.GetRectForIndex(story.IndexOf("\U0001D465", StringComparison.Ordinal));
			Assert.IsTrue(degree.Y < radicand.Y);
			Assert.IsTrue(radical.Height >= radicand.Height);

			richEditBox.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><msubsup><mi>x</mi><mi>i</mi><mn>2</mn></msubsup></math>");
			await WindowHelper.WaitForIdle();
			parsed = GetMathLayout(richEditBox, out _);
			story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
			var @base = parsed.GetRectForIndex(story.IndexOf("\U0001D465", StringComparison.Ordinal));
			var subscript = parsed.GetRectForIndex(story.IndexOf("\U0001D456", StringComparison.Ordinal));
			var superscript = parsed.GetRectForIndex(story.IndexOf('2'));
			Assert.IsTrue(superscript.Y < @base.Y);
			Assert.IsTrue(subscript.Y > @base.Y);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Advanced_Math_Layout_Is_Bounded_And_Maps_Scripts()
	{
		var richEditBox = CreateMathEditor();
		try
		{
			WindowHelper.WindowContent = richEditBox;
			await WindowHelper.WaitForLoaded(richEditBox);
			richEditBox.Document.SetMathMode(RichEditMathMode.MathOnly);

			richEditBox.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mover><mi>x</mi><mo>¯</mo></mover></math>");
			await WindowHelper.WaitForIdle();
			var parsed = GetMathLayout(richEditBox, out _);
			var story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
			var moverBounds = parsed.GetRectForIndex(0);
			var moverBase = parsed.GetRectForIndex(story.IndexOf("\U0001D465", StringComparison.Ordinal));
			AssertBounded(moverBounds);
			Assert.IsTrue(moverBounds.Y < moverBase.Y);

			richEditBox.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><munder><mi>x</mi><mo>_</mo></munder></math>");
			await WindowHelper.WaitForIdle();
			parsed = GetMathLayout(richEditBox, out _);
			story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
			var munderBounds = parsed.GetRectForIndex(0);
			var munderBase = parsed.GetRectForIndex(story.IndexOf("\U0001D465", StringComparison.Ordinal));
			AssertBounded(munderBounds);
			Assert.IsTrue(munderBounds.Bottom > munderBase.Bottom);

			richEditBox.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><munderover><mo>∑</mo><mi>i</mi><mi>n</mi></munderover></math>");
			await WindowHelper.WaitForIdle();
			parsed = GetMathLayout(richEditBox, out _);
			story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
			var lower = parsed.GetRectForIndex(story.IndexOf("\U0001D456", StringComparison.Ordinal));
			var upper = parsed.GetRectForIndex(story.IndexOf("\U0001D45B", StringComparison.Ordinal));
			AssertBounded(lower);
			AssertBounded(upper);
			Assert.IsTrue(upper.Y < lower.Y);

			richEditBox.Document.SetMathML(
				"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mmultiscripts><mi>T</mi><mi>i</mi><mn>2</mn><mprescripts/><mi>j</mi><mn>3</mn></mmultiscripts></math>");
			await WindowHelper.WaitForIdle();
			parsed = GetMathLayout(richEditBox, out _);
			story = richEditBox.Document.GetRange(0, int.MaxValue).Text;
			var baseRect = parsed.GetRectForIndex(story.IndexOf("\U0001D447", StringComparison.Ordinal));
			var preSub = parsed.GetRectForIndex(story.IndexOf("\U0001D457", StringComparison.Ordinal));
			var preSup = parsed.GetRectForIndex(story.IndexOf('3'));
			var postSub = parsed.GetRectForIndex(story.IndexOf("\U0001D456", StringComparison.Ordinal));
			var postSup = parsed.GetRectForIndex(story.IndexOf('2'));
			foreach (var rect in new[] { baseRect, preSub, preSup, postSub, postSup })
			{
				AssertBounded(rect);
			}
			Assert.IsTrue(preSup.Y < baseRect.Y);
			Assert.IsTrue(preSub.Y > baseRect.Y);
			Assert.IsTrue(postSup.Y < baseRect.Y);
			Assert.IsTrue(postSub.Y > baseRect.Y);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
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

	private static MathParsedText GetMathLayout(RichEditBox richEditBox, out TextBlock block)
	{
		var content = richEditBox.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
		block = content?.Content as TextBlock
			?? throw new AssertFailedException("The RichEditBox DisplayBlock was not found.");
		return block.ParsedText as MathParsedText
			?? throw new AssertFailedException($"Expected {nameof(MathParsedText)}, got {block.ParsedText.GetType().Name}.");
	}

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
