#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Islands;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_Window_SystemBackdrop
{
	[TestInitialize]
	public void Initialize() => UnitTestsApp.App.EnsureApplication();

	[TestMethod]
	public void When_Head_Renders_No_Material_Then_Root_Keeps_Themed_Background()
	{
		var window = ((UnitTestsApp.App)Application.Current).MainWindow;
		var previousBackdrop = window.SystemBackdrop;

		try
		{
			window.SystemBackdrop = new MicaBackdrop();

			// The unit-test host's wrapper inherits the no-op SetSystemBackdrop, so no material is
			// ever drawn - even on a Windows 11 build where MicaController.IsSupported() says true.
			// Asking the OS instead of the head is what used to leave this window transparent over
			// nothing.
			Assert.IsFalse(window.HasSupportedSystemBackdrop);

			var root = (XamlIslandRoot)window.Content!.XamlRoot!.VisualTree.RootElement!;
			Assert.IsFalse(root.HasTransparentBackground);

			var background = (SolidColorBrush)root.Background;
			Assert.AreEqual(255, background.Color.A, "The fallback background must stay opaque.");
		}
		finally
		{
			window.SystemBackdrop = previousBackdrop;
		}
	}
}
