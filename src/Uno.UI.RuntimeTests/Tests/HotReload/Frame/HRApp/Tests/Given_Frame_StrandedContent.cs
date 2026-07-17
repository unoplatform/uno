using System.Reflection.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_Frame_StrandedContent : BaseTestClass
{
	/// <summary>
	/// A page navigated into a frame whose subtree never had a layout pass (e.g. a
	/// collapsed ancestor) exists only as <c>Frame.Content</c> — the frame's template is
	/// never applied, so the page is not a materialized visual child and the hot-reload
	/// visual-tree walk cannot see it. The frame's element-update handler must re-create
	/// the stranded page so the updated content is shown once the subtree finally lays
	/// out; without that, the stale pre-update page is displayed.
	/// </summary>
	[TestMethod]
	public async Task When_Page_Updated_While_Unmaterialized_Then_Content_Recreated()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var host = new Grid { Visibility = Visibility.Collapsed };
		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		host.Children.Add(frame);
		UnitTestsUIContentHelper.Content = host;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		var stalePage = frame.Content as Page;
		Assert.IsNotNull(stalePage, "Frame.Navigate must set Frame.Content even without a layout pass");

		// Precondition: the page must be live-but-unmaterialized. If it has a visual
		// parent, this test would exercise the regular visual-tree walk replacement
		// path instead of the stranded-content one.
		Assert.IsNull(VisualTreeHelper.GetParent(stalePage), "the page must not be a materialized visual child");

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			Given_Frame.FirstPageTextBlockOriginalText,
			Given_Frame.FirstPageTextBlockChangedText,
			async () =>
			{
				// The stranded page must have been re-created by the frame's
				// element-update handler (the visual-tree walk cannot reach it).
				Assert.AreNotEqual(stalePage, frame.Content, "the stranded page must be re-created by the hot reload");

				// Reveal the subtree — the moment a real app's host view becomes
				// visible — and verify the updated content is what gets displayed.
				host.Visibility = Visibility.Visible;
				await frame.ValidateTextOnChildTextBlock(Given_Frame.FirstPageTextBlockChangedText);
			},
			ct);
	}
}
