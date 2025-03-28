using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollViewer : BaseTestClass
{
	public const string SimpleTextChange = " (changed)";

	public const string UserControl1TextBlockOriginalText = "Scroll Text";
	public const string UserControl1TextBlockChangedText = UserControl1TextBlockOriginalText + SimpleTextChange;

	[TestMethod]
	[RequiresFullWindow]
	public async Task Check_Change_ScrollViewer()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var frame = new Windows.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Scroll));

		var page = (HR_Frame_Pages_Scroll)frame.Content;

		var sut = page.ScrollViewer;

		await UnitTestsUIContentHelper.WaitForLoaded(page);
		await UnitTestsUIContentHelper.WaitForIdle();

		// Set the initial scroll position to be the end of the scrollable height
		sut.ScrollToVerticalOffset(sut.ScrollableHeight);
		await UnitTestsUIContentHelper.WaitForIdle();
		var scroll = sut.VerticalOffset;


		await frame.ValidateTextOnChildTextBlock(UserControl1TextBlockOriginalText);
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Scroll>(
			UserControl1TextBlockOriginalText,
			UserControl1TextBlockChangedText,
			async () =>
			{
				var updatedScroll = await (frame.Content as FrameworkElement)!.ScrollOffset();
				Assert.AreEqual(scroll, updatedScroll.VerticalOffset, "Updated scroll not correct");
			},
			ct);

		var finalScroll = await (frame.Content as FrameworkElement)!.ScrollOffset();
		Assert.AreEqual(scroll, finalScroll.VerticalOffset, "Final scroll not correct");
		await frame.ValidateTextOnChildTextBlock(UserControl1TextBlockOriginalText);
	}
}
