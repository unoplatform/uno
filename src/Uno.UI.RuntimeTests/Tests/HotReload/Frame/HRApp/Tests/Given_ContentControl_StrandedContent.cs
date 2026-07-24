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
	/// Same scenario as <see cref="Given_Frame_StrandedContent"/> for a plain
	/// <see cref="ContentControl"/>: content assigned while the host's subtree never
	/// had a layout pass exists only as <c>ContentControl.Content</c>, invisible to
	/// the HR visual-tree walk; the ContentControl handler must re-create it.
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

		// Precondition: live-but-unmaterialized, otherwise this test exercises the
		// regular visual-tree walk path instead of the stranded-content one.
		Assert.IsNull(VisualTreeHelper.GetParent(staleContent), "the content must not be a materialized visual child");

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_UC1>(
			ControlOriginalText,
			ControlChangedText,
			async () =>
			{
				Assert.AreNotSame(staleContent, contentControl.Content, "the stranded content must be re-created by the hot reload");

				host.Visibility = Visibility.Visible;
				await contentControl.ValidateTextOnChildTextBlock(ControlChangedText);
			},
			ct);
	}

	/// <summary>
	/// The "do not replace" guards: stranded content of a non-updated type keeps its
	/// identity, and data-item content is never re-created.
	/// </summary>
	[TestMethod]
	public async Task When_Unrelated_Type_Updated_Then_Stranded_Content_Untouched()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var ct = cts.Token;

		var host = new Grid { Visibility = Visibility.Collapsed };
		var viewHost = new ContentControl { Content = new HR_Frame_Pages_UC1() };
		var dataHost = new ContentControl { Content = "data-item" };
		host.Children.Add(viewHost);
		host.Children.Add(dataHost);
		UnitTestsUIContentHelper.Content = host;

		var staleView = viewHost.Content;
		var staleData = dataHost.Content;

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			Given_Frame.FirstPageTextBlockOriginalText,
			Given_Frame.FirstPageTextBlockChangedText,
			() =>
			{
				Assert.AreSame(staleView, viewHost.Content, "stranded content of a non-updated type must keep its identity");
				Assert.AreSame(staleData, dataHost.Content, "data-item content must never be re-created");
				return Task.CompletedTask;
			},
			ct);
	}
}
