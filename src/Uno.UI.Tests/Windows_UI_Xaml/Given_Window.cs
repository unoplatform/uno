﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
	public void New_Window_Becomes_Current()
	{
		// This test expects the created Window to be the first window set.
		// So for it to pass reliably, we need to cleanup current window so that
		// we guarantee that the new window becomes "Current".
		Window.CleanupCurrentForTestsOnly();

		var window = new Microsoft.UI.Xaml.Window();
		window.Activate();
		Assert.AreEqual(window, Microsoft.UI.Xaml.Window.Current);
	}
}
