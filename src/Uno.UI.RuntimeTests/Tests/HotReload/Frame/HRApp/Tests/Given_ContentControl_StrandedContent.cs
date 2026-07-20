using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ContentControl_StrandedContent : BaseTestClass
{
	public const string ControlOriginalText = "Control 1";
	public const string ControlChangedText = ControlOriginalText + " (changed)";

	/// <summary>
	/// Same scenario as <see cref="Given_Frame_StrandedContent"/> but for a plain
	/// <see cref="ContentControl"/>: content assigned while the host's subtree never had
	/// a layout pass exists only as <c>ContentControl.Content</c> — the template is never
	/// applied, so the content is not a materialized visual child and the hot-reload
	/// visual-tree walk cannot see it. The ContentControl element-update handler must
	/// re-create it so the updated content is shown once the subtree lays out.
	/// </summary>
	[TestMethod]
	public async Task When_Content_Updated_While_Unmaterialized_Then_Content_Recreated()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var ct = cts.Token;

		var host = new Grid { Visibility = Visibility.Collapsed };
		var contentControl = new ContentControl
		{
			Content = new HR_Frame_Pages_UC1(),
		};
		host.Children.Add(contentControl);
		UnitTestsUIContentHelper.Content = host;

		var staleContent = contentControl.Content as FrameworkElement;
		Assert.IsNotNull(staleContent);

		// Precondition: the content must be live-but-unmaterialized. If it has a visual
		// parent, this test would exercise the regular visual-tree walk replacement
		// path instead of the stranded-content one.
		Assert.IsNull(VisualTreeHelper.GetParent(staleContent), "the content must not be a materialized visual child");

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_UC1>(
			ControlOriginalText,
			ControlChangedText,
			async () =>
			{
				// The stranded content must have been re-created by the ContentControl's
				// element-update handler (the visual-tree walk cannot reach it).
				Assert.AreNotEqual(staleContent, contentControl.Content, "the stranded content must be re-created by the hot reload");

				// Reveal the subtree and verify the updated content is what gets displayed.
				host.Visibility = Visibility.Visible;
				await contentControl.ValidateTextOnChildTextBlock(ControlChangedText);
			},
			ct);
	}
}
