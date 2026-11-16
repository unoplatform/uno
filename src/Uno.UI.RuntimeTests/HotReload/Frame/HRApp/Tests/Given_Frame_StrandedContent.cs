using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_Frame_StrandedContent : BaseTestClass
{
	/// <summary>
	/// A page navigated into a frame whose subtree never had a layout pass exists only
	/// as <c>Frame.Content</c> — the HR visual-tree walk cannot see it. The frame's
	/// element-update handler must re-create it (and keep the navigation history in
	/// sync) so the updated content is shown once the subtree lays out.
	/// </summary>
	[TestMethod]
	public async Task When_Page_Updated_While_Unmaterialized_Then_Content_Recreated()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var ct = cts.Token;

		var host = new Grid { Visibility = Visibility.Collapsed };
		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		host.Children.Add(frame);
		UnitTestsUIContentHelper.Content = host;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		var stalePage = frame.Content as Page;
		Assert.IsNotNull(stalePage, "Frame.Navigate must set Frame.Content even without a layout pass");

		// Precondition: live-but-unmaterialized, otherwise this test exercises the
		// regular visual-tree walk path instead of the stranded-content one.
		Assert.IsNull(VisualTreeHelper.GetParent(stalePage), "the page must not be a materialized visual child");

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			Given_Frame.FirstPageTextBlockOriginalText,
			Given_Frame.FirstPageTextBlockChangedText,
			async () =>
			{
				Assert.AreNotSame(stalePage, frame.Content, "the stranded page must be re-created by the hot reload");

				host.Visibility = Visibility.Visible;
				await frame.ValidateTextOnChildTextBlock(Given_Frame.FirstPageTextBlockChangedText);

				// Back-navigation must land on the re-created instance, not the stale one.
				frame.Navigate(typeof(HR_Frame_Pages_Page2));
				await frame.ValidateTextOnChildTextBlock(Given_Frame.SecondPageTextBlockOriginalText);
				frame.GoBack();
				await frame.ValidateTextOnChildTextBlock(Given_Frame.FirstPageTextBlockChangedText);
			},
			ct);
	}
}
