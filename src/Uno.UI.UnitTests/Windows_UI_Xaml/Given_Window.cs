using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Activation;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_Window
{
	[TestInitialize]
	public void Init()
	{
		UnitTestsApp.App.EnsureApplication();
	}

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/17399 — relied on the mock-only Window.CleanupCurrentForTestsOnly helper that resets the static Window.Current. The Skia window lifecycle does not expose an equivalent reset hook.")]
	public void New_Window_Becomes_Current()
	{
		var window = new Microsoft.UI.Xaml.Window();
		window.Activate();
		Assert.AreEqual(window, Microsoft.UI.Xaml.Window.Current);
	}
}
