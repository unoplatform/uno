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

		[TestMethod]
		public async Task When_GetPositionFromPoint_Advances_With_X()
		{
			var SUT = BuildSut();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var y = SUT.ActualHeight / 2;
				var near = SUT.GetPositionFromPoint(new Point(2, y));
				var far = SUT.GetPositionFromPoint(new Point(SUT.ActualWidth - 2, y));

				Assert.IsNotNull(near, "left-edge hit should resolve");
				Assert.IsNotNull(far, "right-edge hit should resolve");

				// Hit-testing must not mirror the x coordinate for an LTR paragraph
				// (SkiaTextLine.AlignmentFollowsReadingOrder).
				Assert.IsTrue(far!.Offset > near!.Offset,
					$"Offset at the right edge ({far.Offset}) should exceed the offset at the left edge ({near.Offset})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_GetCharacterRect_Returns_Bounds()
		{
			var SUT = BuildSut();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var start = SUT.ContentStart;
				Assert.IsNotNull(start, "ContentStart should be non-null on populated content");

				// Exercises ParagraphNode.TextRangeToTextBounds -> SkiaTextLine.GetTextBounds.
				var rect = start!.GetCharacterRect(LogicalDirection.Forward);

				Assert.IsTrue(rect.Height > 0, $"Character rect should have a positive height (was {rect.Height})");
				Assert.IsTrue(rect.X >= 0 && rect.Y >= 0, $"Character rect should sit inside the control (was {rect.X},{rect.Y})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
