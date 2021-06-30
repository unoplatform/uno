using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Uno.Disposables;
using Windows.UI.Xaml.Automation.Peers;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	internal static class ControlHelper
	{
		internal static async Task DoClickUsingTap(ButtonBase button)
		{
			var clickEvent = new TaskCompletionSource<object>();

			void OnButtonOnClick(object sender, RoutedEventArgs args)
			{
				clickEvent.TrySetResult(null);
			}


			await RunOnUIThread.ExecuteAsync(() =>
			{
				button.Click += OnButtonOnClick;

				InputHelper.Tap(button);

			});

			using var _ = Disposable.Create(() => button.Click -= OnButtonOnClick);

			await clickEvent.Task;
		}

		internal static async Task DoClickUsingAP(ButtonBase button)
		{
			var clickEvent = new TaskCompletionSource<object>();

			void OnButtonOnClick(object sender, RoutedEventArgs args)
			{
				clickEvent.TrySetResult(null);
			}


			await RunOnUIThread.ExecuteAsync(() =>
			{
				button.Click += OnButtonOnClick;

				var buttonAP = FrameworkElementAutomationPeer.CreatePeerForElement(button) as ButtonAutomationPeer;

				buttonAP.Invoke();
			});

			using var _ = Disposable.Create(() => button.Click -= OnButtonOnClick);

			await clickEvent.Task;
		}


		public static async Task ClickFlyoutCloseButton(DependencyObject element, bool isAccept)
		{
			// The Flyout close button could either be a part of the Flyout itself or it could be in the AppBar
			// We look in both places.
			ButtonBase button = default;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				string buttonName = isAccept ? "AcceptButton" : "DismissButton";

				button = TreeHelper.GetVisualChildByNameFromOpenPopups(buttonName, element) as ButtonBase;
				;

				//if (button == null)
				//{
				//	var cmdBar = TestServices.Utilities.RetrieveOpenBottomCommandBar();
				//	if (cmdBar != null)
				//	{
				//		button = cmdBar.PrimaryCommands[isAccept ? 0 : 1] as ButtonBase;
				//	}
				//}
			});

			Assert.IsNotNull(button, "Close button not found");

			await DoClickUsingAP(button);
		}

		public static async Task ValidateUIElementTree(
			Size windowSizeOverride,
			double scale,
			Func<Task<Panel>> setup,
			Func<Task> cleanup = null,
			bool disableHittestingOnRoot = true,
			bool ignorePopups = false)
		{
			throw new NotImplementedException();
		}
	}
}
