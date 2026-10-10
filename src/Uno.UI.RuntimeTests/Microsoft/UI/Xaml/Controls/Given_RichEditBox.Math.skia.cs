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

	private static MathParsedText GetMathLayout(RichEditBox richEditBox, out TextBlock block)
	{
		var content = richEditBox.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
		block = content?.Content as TextBlock
			?? throw new AssertFailedException("The RichEditBox DisplayBlock was not found.");
		return block.ParsedText as MathParsedText
			?? throw new AssertFailedException($"Expected {nameof(MathParsedText)}, got {block.ParsedText.GetType().Name}.");
	}
}
