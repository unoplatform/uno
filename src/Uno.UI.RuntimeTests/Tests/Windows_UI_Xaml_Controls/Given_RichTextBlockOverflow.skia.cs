using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlockOverflow
	{
		private const string LongText =
			"Line one of the content. Line two of the content. Line three of the content. " +
			"Line four of the content. Line five of the content. Line six of the content. " +
			"Line seven of the content. Line eight of the content. Line nine of the content.";

		[TestMethod]
		public async Task When_Content_Overflows_To_Target()
		{
			var master = new RichTextBlock { Width = 180, MaxLines = 2 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = LongText });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow { Width = 180 };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(master.HasOverflowContent, "Master should report overflow content (MaxLines=2 with long text)");
				Assert.AreEqual(master, overflow.ContentSource, "Overflow ContentSource should be the master");
				Assert.IsTrue(overflow.ActualHeight > 0, $"Overflow should render its content slice (height {overflow.ActualHeight})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Content_Fits_No_Overflow()
		{
			var master = new RichTextBlock { Width = 400 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Short text that fits." });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow { Width = 400 };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(master.HasOverflowContent, "Master should not report overflow when content fits");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
