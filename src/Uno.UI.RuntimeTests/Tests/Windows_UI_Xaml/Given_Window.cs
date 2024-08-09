using System;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Core;
using FluentAssertions;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Private.Infrastructure;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml.Media;

#if !WINDOWS_UWP && !WINAPPSDK
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

#if !WINAPPSDK
[TestClass]
public class Given_Window
{
	[TestCleanup]
	public void CleanupTest()
	{
		TestServices.WindowHelper.CloseAllSecondaryWindows();
	}

#if !HAS_UNO_WINUI
	[TestMethod]
	[RunsOnUIThread]
	public void When_Primary_Window_UWP()
	{
		// The current window on UWP should be a CoreWindow.
		var mainVisualTreeXamlRoot = WinUICoreServices.Instance.MainVisualTree?.XamlRoot;
		Assert.AreEqual(Window.Current.Content.XamlRoot, mainVisualTreeXamlRoot);
	}
#endif

#if HAS_UNO_WINUI

	[TestMethod]
	[RunsOnUIThread]
	public void When_CreateNewWindow()
	{
		if (!CoreApplication.IsFullFledgedApp)
		{
			Assert.Inconclusive("This test can only be run in a full-fledged app");
			return;
		}

		if (NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment without multiwindow support");
		}

		var act = () => new Window(WindowType.DesktopXamlSource);
		act.Should().Throw<InvalidOperationException>();
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Create_Multiple_Windows()
	{
		if (!CoreApplication.IsFullFledgedApp)
		{
			Assert.Inconclusive("This test can only be run in a full-fledged app");
			return;
		}

		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		var startingNumberOfWindows = ApplicationHelper.Windows.Count;

		for (int i = 0; i < 10; i++)
		{
			var sut = new Window(WindowType.DesktopXamlSource);
			sut.Close();
		}

		var endNumberOfWindows = ApplicationHelper.Windows.Count;
		Assert.AreEqual(startingNumberOfWindows, endNumberOfWindows);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Close_Non_Activated_Window()
	{
		if (!CoreApplication.IsFullFledgedApp)
		{
			Assert.Inconclusive("This test can only be run in a full-fledged app");
			return;
		}

		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		var sut = new Window(WindowType.DesktopXamlSource);
		bool closedFired = false;
		sut.Closed += (s, e) => closedFired = true;
		sut.Close();

		Assert.IsTrue(closedFired);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_Opens()
	{
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		var sut = new Window();
		bool activated = false;
		sut.Content = new Border();
		sut.Activated += (s, e) => activated = true;
		sut.Activate();
		await TestServices.WindowHelper.WaitFor(() => activated);
		Assert.IsTrue(activated);
		await TestServices.WindowHelper.WaitForLoaded(sut.Content as FrameworkElement);
		Assert.IsTrue(sut.Bounds.Width > 0);
		Assert.IsTrue(sut.Bounds.Height > 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_Content_Loads()
	{
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		var sut = new Window();
		bool loaded = false;
		var border = new Border();
		var button = new Button();
		button.Content = "Hello!";
		border.Child = button;
		sut.Content = border;
		button.Loaded += (s, e) => loaded = true;
		sut.Activate();
		await TestServices.WindowHelper.WaitFor(() => loaded);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_Content_Non_Zero_Size()
	{
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		var sut = new Window();
		var button = new Button();
		button.Content = "Hello!";
		sut.Content = button;
		sut.Activate();
		await TestServices.WindowHelper.WaitFor(() => button.ActualWidth > 0);
		Assert.IsTrue(button.ActualWidth > 0);
		Assert.IsTrue(button.ActualHeight > 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_From_Xaml()
	{
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		var sut = new RedWindow();
		sut.Activate();
		await TestServices.WindowHelper.WaitForLoaded(sut.Content as FrameworkElement);

		// Verify that center of window is red
		var initialScreenshot = await UITestHelper.ScreenShot(sut.Content as FrameworkElement);

		var color = initialScreenshot.GetPixel(initialScreenshot.Width / 2, initialScreenshot.Height / 2);
		color.Should().Be(Colors.Red);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_No_Background_Light_Dark()
	{
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		using var _ = ThemeHelper.UseDarkTheme();
		var sut = new NoBackgroundWindow();

		await VerifyWindowBackgroundAsync(sut, false, Colors.Black);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_No_Background_Light()
	{
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		var sut = new NoBackgroundWindow();

		await VerifyWindowBackgroundAsync(sut, false, Colors.White);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_No_Background_Switch_Theme()
	{
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}

		var sut = new NoBackgroundWindow();

		await VerifyWindowBackgroundAsync(sut, false, Colors.White);

		using var _ = ThemeHelper.UseDarkTheme();
		await VerifyWindowBackgroundAsync(sut, true, Colors.Black);
	}

	private static async Task VerifyWindowBackgroundAsync(Window sut, bool wasActivated, Color expectedColor)
	{
		if (!wasActivated)
		{
			bool activated = false;
			sut.Activated += (s, e) => activated = true;
			sut.Activate();

			await TestServices.WindowHelper.WaitFor(() => activated);
		}

		await TestServices.WindowHelper.WaitForLoaded(sut.Content as FrameworkElement);

		// Verify that center of window is red
		var rootElement = sut.Content.XamlRoot.VisualTree.RootElement;
		Assert.IsInstanceOfType(rootElement, typeof(Panel));
		var rootElementAsPanel = (Panel)rootElement;
		var rootElementBackground = rootElementAsPanel.Background;
		Assert.IsInstanceOfType(rootElementBackground, typeof(SolidColorBrush));
		var rootElementBackgroundAsSolidColorBrush = (SolidColorBrush)rootElementBackground;
		Assert.AreEqual(expectedColor, rootElementBackgroundAsSolidColorBrush.Color);
	}
#endif
}
#endif
