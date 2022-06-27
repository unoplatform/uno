using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.ViewManagement;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_CoreWindow
	{
#if !WINDOWS_UWP
		[TestMethod]
		[RunsOnUIThread]
		public void CoreWindow_Bounds_Implemented()
		{ 
			var w = new Windows.UI.Xaml.Window();
			var vb = ApplicationView.GetForCurrentView().VisibleBounds; 
			var SUT = w.CoreWindow.Bounds;
#if __IOS__
			Assert.AreEqual(w.Bounds.Width, SUT.Width);
			Assert.AreEqual(w.Bounds.Height, SUT.Height);
#else
			Assert.AreEqual(vb.Width, SUT.Width);
			Assert.AreEqual(vb.Height, SUT.Height); 
#endif
		}
#endif
	}
}
