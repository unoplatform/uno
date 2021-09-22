using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Popups
{
	[TestClass]
	public class Given_MessageDialog
	{
#if !__WASM__
		[TestMethod]
		[RunsOnUIThread]
		public async Task Should_Close_Open_Popups()
		{
			var button = new Windows.UI.Xaml.Controls.Button();
			var flyout = new Flyout();
			FlyoutBase.SetAttachedFlyout(button, flyout);
			WindowHelper.WindowContent = button;
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopups(Window.Current).Count);
			FlyoutBase.ShowAttachedFlyout(button);
			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopups(Window.Current).Count);
			var messageDialog = new MessageDialog("Hello");
			var asyncOperation = messageDialog.ShowAsync();
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopups(Window.Current).Count);
			asyncOperation.Cancel();
		}
#endif
	}
}
