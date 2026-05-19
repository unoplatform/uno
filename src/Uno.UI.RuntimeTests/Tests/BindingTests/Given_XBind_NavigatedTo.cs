using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
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
		await WindowHelper.WaitForIdle();

		// Simulate the "await before navigation" scenario from the issue
		await Task.Delay(200);

		var navigated = false;
		frame.Navigated += (s, e) => navigated = true;
		frame.Navigate(typeof(XBind_NavigatedTo_Page));
		await WindowHelper.WaitFor(() => navigated, timeoutMS: 5000);
		await WindowHelper.WaitForIdle();

		var page = frame.Content as XBind_NavigatedTo_Page;
		Assert.IsNotNull(page, "Expected Frame.Content to be XBind_NavigatedTo_Page.");

		// After OnNavigatedTo sets DataContext, DataContextChanged fires and updates BoundText.
		// Bindings.Update() is called inside DataContextChanged handler.
		// The BoundText property should be "datacontext-changed" and x:Bind should reflect it.
		Assert.AreEqual("datacontext-changed", page.BoundText,
			$"Expected BoundText to be 'datacontext-changed' after DataContext set in OnNavigatedTo, " +
			$"but got '{page.BoundText}'. " +
			$"This reproduces x:Bind being evaluated too early (before OnNavigatedTo DataContext changes).");
	}
}
