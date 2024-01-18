using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Core;
using FluentAssertions;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;


#if !WINDOWS_UWP && !WINAPPSDK
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_Window
{
#if !WINAPPSDK
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
#endif

	[TestMethod]
	[RunsOnUIThread]
	public void When_Secondary_Window_From_Xaml()
	{
		var sut = new RedWindow();
		sut.Activate();
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Secondary_Window_Background()
	{
		var sut = new RedWindow();
		sut.Activate();
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Secondary_Window_No_Background()
	{
		var sut = new NoBackgroundWindow();
		sut.Activate();
	}
}
