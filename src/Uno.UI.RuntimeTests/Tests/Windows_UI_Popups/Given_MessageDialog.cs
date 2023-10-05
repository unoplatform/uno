using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
using System.Linq;
using Private.Infrastructure;
#if HAS_UNO
using Uno.UI.WinRT.Extensions.UI.Popups;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Popups
{
	[TestClass]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
	public class Given_MessageDialog
	{
		[TestMethod]
		[RunsOnUIThread]
		public void Should_Close_Open_Flyouts()
		{
			if (WindowHelper.IsXamlIsland)
			{
				// MessageDialog is not supported in Uno Islands.
				return;
			}

			var button = new Microsoft.UI.Xaml.Controls.Button();
			var flyout = new Flyout();
			FlyoutBase.SetAttachedFlyout(button, flyout);
			WindowHelper.WindowContent = button;
			Assert.AreEqual(0, GetNonMessageDialogPopupsCount());
			FlyoutBase.ShowAttachedFlyout(button);
			Assert.AreEqual(1, GetNonMessageDialogPopupsCount());
			var messageDialog = new MessageDialog("Should_Close_Open_Popups");
			var asyncOperation = messageDialog.ShowAsync();
			Assert.AreEqual(0, GetNonMessageDialogPopupsCount());
			asyncOperation.Cancel();
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task Should_Not_Close_Open_ContentDialogs()
		{
			if (WindowHelper.IsXamlIsland)
			{
				// MessageDialog is not supported in Uno Islands.
				return;
			}

			Assert.AreEqual(0, GetNonMessageDialogPopupsCount());

			var contentDialog = new ContentDialog
			{
				Title = "Title",
				Content = "My Dialog Content"
			};
			contentDialog.XamlRoot = WindowHelper.XamlRoot;

			_ = contentDialog.ShowAsync();

            await WindowHelper.WaitFor(() => GetNonMessageDialogPopupsCount() > 0);
			Assert.AreEqual(1, GetNonMessageDialogPopupsCount());

			var messageDialog = new MessageDialog("Hello");
#if WINUI_WINDOWING
			var handle = global::WinRT.Interop.WindowNative.GetWindowHandle(WindowHelper.CurrentTestWindow);
			global::WinRT.Interop.InitializeWithWindow.Initialize(messageDialog, handle);
#endif
			var asyncOperation = messageDialog.ShowAsync();
			Assert.AreEqual(1, GetNonMessageDialogPopupsCount());
			contentDialog.Hide();
			asyncOperation.Cancel();
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Cancel_Then_CloseDialog()
		{
			if (WindowHelper.IsXamlIsland)
			{
				// MessageDialog is not supported in Uno Islands and desktop windows.
				return;
			}

			var messageDialog = new MessageDialog("When_Cancel_Then_CloseDialog");
#if WINUI_WINDOWING
			var handle = global::WinRT.Interop.WindowNative.GetWindowHandle(WindowHelper.CurrentTestWindow);
			global::WinRT.Interop.InitializeWithWindow.Initialize(messageDialog, handle);
#endif
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

		private int GetNonMessageDialogPopupsCount()
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);
#if HAS_UNO
			return popups.Count(p => p.Child is not MessageDialogContentDialog);
#else
			return popups.Count;
#endif
		}
	}
}
