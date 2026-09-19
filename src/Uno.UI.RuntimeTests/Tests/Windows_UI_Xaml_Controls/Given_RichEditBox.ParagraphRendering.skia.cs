#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_First_Baseline_Includes_Paragraph_Spacing(bool empty)
		{
			var editor = new RichEditBox { Width = 360, FontSize = 24 };
			try
			{
				editor.Document.SetText(TextSetOptions.None, empty ? "" : "first\rsecond");
				await UITestHelper.Load(editor);
				var block = GetDisplayBlock(editor);
				var before = block.ParsedText.FirstLineBaseline;

				editor.Document.GetRange(0, 0).ParagraphFormat.SpaceBefore = 12;
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(before + 16, block.ParsedText.FirstLineBaseline, 0.1f);
				Assert.AreEqual(block.ParsedText.GetBaselineForIndex(0), block.BaselineOffset, 0.1);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
		public async Task When_TextLineBounds_Changes_Then_Bounded_Layout_Is_Invalidated()
		{
			var editor = new RichEditBox { Width = 360, Height = 120, FontSize = 24 };
			try
			{
				editor.Document.SetText(TextSetOptions.FormatRtf, BuildAlternatingRunRtf(8200, 1));
				await UITestHelper.Load(editor);
				Assert.IsTrue(editor.UsesBoundedRichLayout);
				var block = GetDisplayBlock(editor);
				var before = block.ParsedText.GetVisualLine(0);
				var rebuilds = editor.BoundedRichLayoutParagraphRebuildCount;
				editor.Document.GetText(TextGetOptions.None, out var originalText);

				block.TextLineBounds = TextLineBounds.TrimToBaseline;
				await WindowHelper.WaitForIdle();

				Assert.IsGreaterThan(rebuilds, editor.BoundedRichLayoutParagraphRebuildCount);
				Assert.IsLessThan(before.Bounds.Height, block.ParsedText.GetVisualLine(0).Bounds.Height);
				Assert.AreEqual(block.ParsedText.GetVisualLine(0).Baseline, block.BaselineOffset, 0.1);
				editor.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual(originalText, text);

				block.TextLineBounds = TextLineBounds.Full;
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(before.Bounds.Height, block.ParsedText.GetVisualLine(0).Bounds.Height, 0.1);
				Assert.AreEqual(before.Baseline, block.BaselineOffset, 0.1);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
		public async Task When_Math_Layout_Provides_The_Shared_First_Baseline()
		{
			var editor = CreateMathEditor();
			try
			{
				editor.Document.SetMathMode(RichEditMathMode.MathOnly);
				editor.Document.SetMathML(
					"<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mfrac><mi>a</mi><mi>b</mi></mfrac></math>");
				await UITestHelper.Load(editor);

				var parsed = GetMathLayout(editor, out var block);
				Assert.IsGreaterThan(0, block.BaselineOffset);
				Assert.AreEqual(parsed.GetVisualLine(0).Baseline, block.BaselineOffset, 0.1);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
		public async Task When_Right_Tab_Excludes_Paragraph_Mark_Advance()
		{
			var editor = new RichEditBox { Width = 360, TextWrapping = TextWrapping.NoWrap };
			try
			{
				await UITestHelper.Load(editor);
				editor.Document.SetText(TextSetOptions.None, "R\t123\rnext");
				editor.Document.GetRange(0, 0).ParagraphFormat.AddTab(90, TabAlignment.Right, TabLeader.Spaces);
				editor.Document.GetRange(5, 6).CharacterFormat.Spacing = 12;
				await WindowHelper.WaitForIdle();

				var block = GetDisplayBlock(editor);
				Assert.AreEqual(120, block.ParsedText.GetRectForIndex(5).X, 0.5,
					"The paragraph marker is not part of the right-aligned tab field.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Custom_Tab_Stop_Projects_To_Layout_And_Caret_Geometry()
		{
			var editor = new RichEditBox { Width = 320, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "A\tB");
				editor.Document.GetRange(0, 0).ParagraphFormat.AddTab(72, TabAlignment.Left, TabLeader.Dots);
				await WindowHelper.WaitForIdle();

				var block = GetDisplayBlock(editor);
				var run = block.Inlines.OfType<Run>().First();
				Assert.IsNotNull(run.ParagraphLayout);
				Assert.HasCount(1, run.ParagraphLayout.Tabs);
				var tab = run.ParagraphLayout.Tabs[0];
				Assert.AreEqual(96f, tab.Position, 0.01f);
				Assert.AreEqual(TabAlignment.Left, tab.Alignment);
				Assert.AreEqual(TabLeader.Dots, tab.Leader);

				var afterTab = block.ParsedText.GetRectForIndex(2);
				Assert.AreEqual(96, afterTab.X, 2);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Right_And_Decimal_Tabs_Use_Bounded_Field_Alignment()
		{
			var editor = new RichEditBox { Width = 360, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "R\t123\rD\t12.5");
				editor.Document.GetRange(0, 0).ParagraphFormat.AddTab(90, TabAlignment.Right, TabLeader.Dashes);
				editor.Document.GetRange(6, 6).ParagraphFormat.AddTab(90, TabAlignment.Decimal, TabLeader.Equals);
				await WindowHelper.WaitForIdle();

				var block = GetDisplayBlock(editor);
				var rightFieldStart = block.ParsedText.GetRectForIndex(2).X;
				var rightFieldEnd = block.ParsedText.GetRectForIndex(5).X;
				var decimalFieldStart = block.ParsedText.GetRectForIndex(8).X;
				var decimalPoint = block.ParsedText.GetRectForIndex(10).X;

				Assert.AreEqual(120, rightFieldEnd, 5);
				Assert.IsTrue(rightFieldStart < rightFieldEnd);
				Assert.AreEqual(120, decimalPoint, 3);
				Assert.IsTrue(decimalFieldStart < decimalPoint);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Custom_Tabs_Change_At_Runtime_Without_Changing_Text()
		{
			var editor = new RichEditBox { Width = 320, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "A\tB");
				var format = editor.Document.GetRange(0, 0).ParagraphFormat;
				format.AddTab(48, TabAlignment.Left, TabLeader.Spaces);
				await WindowHelper.WaitForIdle();
				var first = GetDisplayBlock(editor).ParsedText.GetRectForIndex(2).X;

				format.ClearAllTabs();
				format.AddTab(96, TabAlignment.Left, TabLeader.Lines);
				await WindowHelper.WaitForIdle();
				var second = GetDisplayBlock(editor).ParsedText.GetRectForIndex(2).X;

				Assert.AreEqual(64, first, 2);
				Assert.AreEqual(128, second, 2);
				GetTextWithoutFinalEop(editor.Document, out var text);
				Assert.AreEqual("A\tB", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
