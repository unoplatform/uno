using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_TextPointer
	{
		private static RichTextBlock BuildSut()
		{
			var sut = new RichTextBlock { Width = 300 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Hello world from RichTextBlock" });
			sut.Blocks.Add(paragraph);
			return sut;
		}

		[TestMethod]
		public async Task When_ContentStart_ContentEnd()
		{
			var SUT = BuildSut();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var start = SUT.ContentStart;
				var end = SUT.ContentEnd;

				Assert.IsNotNull(start, "ContentStart should be non-null on populated content");
				Assert.IsNotNull(end, "ContentEnd should be non-null on populated content");
				Assert.IsTrue(end.Offset > start.Offset, $"ContentEnd ({end.Offset}) should be past ContentStart ({start.Offset})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_GetPositionFromPoint()
		{
			var SUT = BuildSut();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				// A point well inside the laid-out content should resolve to a valid pointer.
				var pointer = SUT.GetPositionFromPoint(new Point(5, 5));
				Assert.IsNotNull(pointer, "GetPositionFromPoint should return a pointer for an in-content point");
				Assert.IsTrue(pointer!.Offset >= SUT.ContentStart!.Offset, "Resolved offset should be within content");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
