using System;
using System.Linq;
using System.Threading.Tasks;
using UITests.Windows_UI_Xaml_Controls.TextBlockControl;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_TextBlock
	{
		[TestMethod]
		[RunsOnUIThread]
		public void Check_ActualWidth_After_Measure()
		{
			var SUT = new TextBlock { Text = "Some text" };
			var size = new Size(1000, 1000);
			SUT.Measure(size);
			Assert.IsTrue(SUT.DesiredSize.Width > 0);
			Assert.IsTrue(SUT.DesiredSize.Height > 0);

			// For simplicity, currently we don't insist on a specific value here. The exact details of text measurement are highly
			// platform-specific, and additionally on UWP the ActualWidth and DesiredSize.Width are not exactly the same, a subtlety Uno
			// currently doesn't try to replicate.
			Assert.IsTrue(SUT.ActualWidth > 0);
			Assert.IsTrue(SUT.ActualHeight > 0);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void Check_ActualWidth_After_Measure_Collapsed()
		{
			var SUT = new TextBlock { Text = "Some text", Visibility = Visibility.Collapsed };
			var size = new Size(1000, 1000);
			SUT.Measure(size);
			Assert.AreEqual(0, SUT.DesiredSize.Width);
			Assert.AreEqual(0, SUT.DesiredSize.Height);

			Assert.AreEqual(0, SUT.ActualWidth);
			Assert.AreEqual(0, SUT.ActualHeight);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void Check_Text_When_Having_Inline_Text_In_Span()
		{
			var SUT = new InlineTextInSpan();
			var panel = (StackPanel)SUT.Content;
			var span = (Span)((TextBlock)panel.Children.Single()).Inlines.Single();
			var inlines = span.Inlines;
			Assert.AreEqual(3, inlines.Count);
			Assert.AreEqual("Where ", ((Run)inlines[0]).Text);
			Assert.AreEqual("did", ((Run)((Italic)inlines[1]).Inlines.Single()).Text);
			Assert.AreEqual(" my text go?", ((Run)inlines[2]).Text);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Null_FontFamily()
		{
			var SUT = new TextBlock { Text = "Some text", FontFamily = null };
			WindowHelper.WindowContent = SUT;
			SUT.Measure(new Size(1000, 1000));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_FontStretch()
		{
			var SUT = new TextBlock_FontStretch();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForLoaded(SUT.TextBlockCondensed);
			await WindowHelper.WaitForLoaded(SUT.TextBlockNormal);
			await WindowHelper.WaitForLoaded(SUT.TextBlockNormal2);
			await WindowHelper.WaitForLoaded(SUT.TextBlockExpanded);

			await WindowHelper.WaitForIdle();

			var renderer = new RenderTargetBitmap();
			await renderer.RenderAsync(SUT);
			var screenshot = new RawBitmap(renderer, SUT);
			screenshot.Populate();

			int lastBlueNormal = GetLastBluePixel(screenshot, SUT.TextBlockNormal);
			int lastBlueNormal2 = GetLastBluePixel(screenshot, SUT.TextBlockNormal2);

			Assert.AreEqual(lastBlueNormal, lastBlueNormal2);
#if !__SKIA__ // FontStretch doesn't work properly on Skia for custom fonts.
			int lastBlueCondensed = GetLastBluePixel(screenshot, SUT.TextBlockCondensed);
			Assert.IsTrue(lastBlueCondensed < lastBlueNormal);

			int lastBlueExpanded = GetLastBluePixel(screenshot, SUT.TextBlockExpanded);
			Assert.IsTrue(lastBlueExpanded > lastBlueNormal);
#endif

			int lastBlueCondensedArial = GetLastBluePixel(screenshot, SUT.TextBlockCondensedArial);
			int lastBlueNormalArial = GetLastBluePixel(screenshot, SUT.TextBlockNormalArial);
			Assert.IsTrue(lastBlueCondensedArial < lastBlueNormalArial);
		}

		private static int GetLastBluePixel(RawBitmap screenshot, TextBlock textBlock)
		{
			for (int x = (int)textBlock.ActualWidth - 1; x >= 0; x--)
			{
				var point = textBlock.TransformToVisual(screenshot.RenderedElement).TransformPoint(new Point(x, textBlock.ActualHeight / 2));
				var pixel = screenshot.GetPixel((int)point.X, (int)point.Y);
				if (ImageAssert.AreSameColor(Windows.UI.Colors.Blue, pixel, tolerance: 10, out _))
				{
					return x;
				}
			}

			return -1;
		}
	}
}
