using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Client;
using UIKit;
using Uno.Disposables;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Controls
{
	public partial class MenuFlyout
	{
		private UIActionSheet _actionSheet;
		private SerialDisposable _subscriptions;

		private void ShowActionSheet(UIView placementTarget)
		{
			_subscriptions = new SerialDisposable();

			var availableItems = Items.Where(item => item.Visibility == Visibility.Visible);

#pragma warning disable CS0618 // Type or member is obsolete
			_actionSheet = new UIActionSheet(
				title: null,
				del: null,
				cancelTitle: GetValue(CancelTextIosOverrideProperty) as string ?? LocalizedCancelString,
				destroy: null,
				other: availableItems.OfType<MenuFlyoutItem>().Select(item => item.Text).ToArray()
			);
#pragma warning restore CS0618 // Type or member is obsolete

			_actionSheet.Dismissed += OnDismissed;

			EventHandler<UIButtonEventArgs> handler =
				(_, args) =>
				{
					var item = availableItems.OfType<MenuFlyoutItem>().ElementAtOrDefault((int)args.ButtonIndex);

					if (item != null)
					{
						item.InvokeClick();
						Hide();
					}
				};

			_actionSheet.Clicked += handler;
			_subscriptions.Disposable = Disposable.Create(() => _actionSheet.Clicked -= handler);

			_ = CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.Normal,
				() =>
				{
					switch (UIDevice.CurrentDevice.UserInterfaceIdiom)
					{
						case UIUserInterfaceIdiom.Pad:
							_actionSheet.ShowFrom(placementTarget.Bounds, placementTarget, animated: true);
							break;
						case UIUserInterfaceIdiom.Phone:
							_actionSheet.ShowInView(UIApplication.SharedApplication.KeyWindow);
							break;
						case UIUserInterfaceIdiom.Unspecified:
							break;
					}
				}
			);
		}

		private void OnDismissed(object sender, UIButtonEventArgs e)
		{
			_subscriptions.Dispose();
		}

		private void HideActionSheet()
		{
			_actionSheet?.DismissWithClickedButtonIndex(_actionSheet.CancelButtonIndex, animated: true);
		}
	}
}
