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
