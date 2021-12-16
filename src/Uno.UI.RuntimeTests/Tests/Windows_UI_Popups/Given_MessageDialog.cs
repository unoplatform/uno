using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
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
#if __SKIA__
		[Ignore("https://github.com/unoplatform/uno/issues/7271")]
#endif
		[TestMethod]
		[RunsOnUIThread]
		public async Task Should_Close_Open_Flyouts()
		{
			var button = new Windows.UI.Xaml.Controls.Button();
			var flyout = new Flyout();
			FlyoutBase.SetAttachedFlyout(button, flyout);
			WindowHelper.WindowContent = button;
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopups(Window.Current).Count);
			FlyoutBase.ShowAttachedFlyout(button);
			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopups(Window.Current).Count);
			var messageDialog = new MessageDialog("Should_Close_Open_Popups");
			var asyncOperation = messageDialog.ShowAsync();
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopups(Window.Current).Count);
			asyncOperation.Cancel();
		}

#if __SKIA__
		[Ignore("https://github.com/unoplatform/uno/issues/7271")]
#endif
		[TestMethod]
		[RunsOnUIThread]
		public async Task Should_Not_Close_Open_ContentDialogs()
		{
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopups(Window.Current).Count);

			var contentDialog = new ContentDialog
			{
				Title = "Title",
				Content = "My Dialog Content"
			};

			contentDialog.ShowAsync();

			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopups(Window.Current).Count);

			var messageDialog = new MessageDialog("Hello");
			var asyncOperation = messageDialog.ShowAsync();
			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopups(Window.Current).Count);
			contentDialog.Hide();
			asyncOperation.Cancel();
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
#if __WASM__ || __SKIA__
		[Ignore("Message dialog not implemented  https://github.com/unoplatform/uno/issues/7271")]
#endif
		public async Task When_Cancel_Then_CloseDialog()
		{
			var messageDialog = new MessageDialog("When_Cancel_Then_CloseDialog");
			var asyncOperation = messageDialog.ShowAsync();

			Assert.AreEqual(AsyncStatus.Started, asyncOperation.Status);

			await WindowHelper.WaitForIdle();

#if __IOS__ //in iOS we want to force calling in a different thread than UI
			await Task.Run(() => asyncOperation.Cancel());
#else
			asyncOperation.Cancel();
#endif

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(AsyncStatus.Canceled, asyncOperation.Status);
		}
	}
}
