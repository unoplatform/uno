using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.Core;

#if !__SKIA__
using FluentAssertions;
#endif

#if !WINDOWS_UWP && !WINAPPSDK
using Uno.UI.Xaml;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Window
	{
#if !WINAPPSDK && !__SKIA__
		[TestMethod]
		[RunsOnUIThread]
		public void When_CreateNewWindow()
		{
			if (!CoreApplication.IsFullFledgedApp)
			{
				Assert.Inconclusive("This test can only be run in a full-fledged app");
				return;
			}

			var act = () => new Window(WindowType.DesktopXamlSource);
			act.Should().Throw<InvalidOperationException>();
		}
#endif

#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		public void When_Create_Multiple_Windows()
		{
			if (!CoreApplication.IsFullFledgedApp)
			{
				Assert.Inconclusive("This test can only be run in a full-fledged app");
				return;
			}

			var startingNumberOfWindows = ApplicationHelper.Windows.Count;

			for (int i = 0; i < 10; i++)
			{
				var sut = new Window(WindowType.DesktopXamlSource);
				sut.Close();
			}

			var endNumberOfWindows = ApplicationHelper.Windows.Count;
			Assert.AreEqual(startingNumberOfWindows, endNumberOfWindows)
		}
#endif
	}
}
