using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Activation;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml;

// These tests will become invalid when multi-window support is added #8341
[TestClass]
public class Given_Window
{
	[TestMethod]
	public void New_Window_Becomes_Current()
	{
		var window = new Microsoft.UI.Xaml.Window(true);
		window.Activate();
		Assert.AreEqual(window, Microsoft.UI.Xaml.Window.Current);
	}

	[TestMethod]
	public void New_Window_Does_Not_Override_Current()
	{
		var existingCurrent = Microsoft.UI.Xaml.Window.Current;
		var window = new Microsoft.UI.Xaml.Window(true);
		window.Activate();
		Assert.AreEqual(existingCurrent, Microsoft.UI.Xaml.Window.Current);
	}
}
