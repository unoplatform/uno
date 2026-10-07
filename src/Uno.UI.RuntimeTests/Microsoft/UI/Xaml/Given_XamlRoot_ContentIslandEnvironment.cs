#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_XamlRoot_ContentIslandEnvironment
{
	[TestCleanup]
	public void Cleanup() => TestServices.WindowHelper.CloseAllSecondaryWindows();

	[TestMethod]
	public async Task When_Hosted_In_Window_Then_AppWindowId_Matches()
	{
		AssertNotXamlIsland();

		// WaitForLoaded waits for a non-zero size.
		Border border = new() { Width = 10, Height = 10 };
		await UITestHelper.Load(border);

		var environment = border.XamlRoot!.ContentIslandEnvironment;

		Assert.IsNotNull(environment);
		Assert.AreEqual(TestServices.WindowHelper.CurrentTestWindow.AppWindow.Id, environment.AppWindowId);
	}

	[TestMethod]
	public async Task When_Read_Twice_Then_AppWindowId_Stable()
	{
		AssertNotXamlIsland();

		// WaitForLoaded waits for a non-zero size.
		Border border = new() { Width = 10, Height = 10 };
		await UITestHelper.Load(border);

		var first = border.XamlRoot!.ContentIslandEnvironment;
		var second = border.XamlRoot!.ContentIslandEnvironment;

		Assert.IsNotNull(first);
		Assert.IsNotNull(second);
		Assert.AreEqual(first.AppWindowId, second.AppWindowId);
	}

#if HAS_UNO_WINUI || WINAPPSDK
	[TestMethod]
	public async Task When_Hosted_In_Secondary_Window_Then_Its_AppWindowId()
	{
		AssertNotXamlIsland();
#if HAS_UNO
		if (!Uno.UI.Xaml.Controls.NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}
#endif

		Border border = new();
		Window window = new() { Content = border };
		var loaded = false;
		border.Loaded += (s, e) => loaded = true;

		try
		{
			window.Activate();
			await TestServices.WindowHelper.WaitFor(() => loaded);

			var environment = border.XamlRoot!.ContentIslandEnvironment;

			Assert.IsNotNull(environment);
			Assert.AreEqual(window.AppWindow.Id, environment.AppWindowId);
			Assert.AreNotEqual(TestServices.WindowHelper.CurrentTestWindow.AppWindow.Id, environment.AppWindowId);
		}
		finally
		{
			window.Close();
		}
	}
#endif

	private static void AssertNotXamlIsland()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive("The XAML island host is not an AppWindow.");
		}
	}
}
