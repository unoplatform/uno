using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

// Repro tests for https://github.com/unoplatform/uno/issues/2872

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_XBind_NavigatedTo
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/2872")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_XBind_Updated_After_DataContext_Set_In_OnNavigatedTo()
	{
		// Issue: x:Bind is evaluated too early when there's an await before navigation.
		// Changes made in OnNavigatedTo (like setting DataContext which triggers
		// DataContextChanged which updates a bound property) are ignored.
		// Expected: x:Bind should reflect values set in/after OnNavigatedTo.

		var frame = new Frame();
		WindowHelper.WindowContent = frame;
		try
		{
			await WindowHelper.WaitForIdle();

			// Simulate the "await before navigation" scenario from the issue:
			// an async continuation break on the UI thread before navigating.
			await Task.Yield();

			var navigated = false;
			frame.Navigated += (s, e) => navigated = true;
			frame.Navigate(typeof(XBind_NavigatedTo_Page));
			await WindowHelper.WaitFor(() => navigated, timeoutMS: 5000);
			await WindowHelper.WaitForIdle();

			var page = frame.Content as XBind_NavigatedTo_Page;
			Assert.IsNotNull(page, "Expected Frame.Content to be XBind_NavigatedTo_Page.");

			// After OnNavigatedTo sets DataContext, DataContextChanged fires and updates BoundText.
			// x:Bind (evaluated at Loading, after OnNavigatedTo) must reflect that in the rendered TextBlock,
			// without any manual Bindings.Update() papering over the timing.
			Assert.AreEqual("datacontext-changed", page.BoundText,
				$"Expected BoundText to be 'datacontext-changed' after DataContext set in OnNavigatedTo, " +
				$"but got '{page.BoundText}'.");

			var textBlock = page.FindName("TheTextBlock") as TextBlock;
			Assert.IsNotNull(textBlock, "Expected to find TextBlock named 'TheTextBlock' on the page.");

			await WindowHelper.WaitFor(
				() => textBlock.Text == "datacontext-changed",
				timeoutMS: 5000);

			Assert.AreEqual("datacontext-changed", textBlock.Text,
				$"Expected x:Bind to update TheTextBlock.Text to 'datacontext-changed' after DataContext set in OnNavigatedTo, " +
				$"but got '{textBlock.Text}'. " +
				$"This reproduces x:Bind being evaluated too early (before OnNavigatedTo DataContext changes).");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
