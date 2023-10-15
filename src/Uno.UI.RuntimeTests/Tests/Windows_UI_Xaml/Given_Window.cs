using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Core;
#if !WINDOWS_UWP
using Uno.UI.Xaml;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
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

			// This used to crash on wasm which was trying to create a second D&D extension
			var sut = new Window(WindowType.DesktopXamlSource);
			sut.Close();
		}
#endif
	}
}
