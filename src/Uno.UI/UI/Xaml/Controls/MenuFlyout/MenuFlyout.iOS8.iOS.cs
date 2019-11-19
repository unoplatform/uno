using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Client;
using Uno.Extensions;
using Uno.UI.Services;
using UIKit;
using Uno.UI.Controls;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
    public partial class MenuFlyout
    {
		private static DependencyProperty IsDestructiveProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.MenuFlyoutItemExtensions", "IsDestructive");

		private UIAlertController _alertController;

		private void ShowAlert(UIView placementTarget)
		{
			_alertController = new UIAlertController();

			Items
				.OfType<MenuFlyoutItem>()
				.Trim()
				.Where(item => item.Visibility == Visibility.Visible)
				.Select(item => UIAlertAction.Create(
					item.Text,
					true == (item.GetValue(IsDestructiveProperty) as bool?) ? UIAlertActionStyle.Destructive : UIAlertActionStyle.Default,
					_ =>
					{
						item.InvokeClick();
						Hide();
					}
				))
				.Concat(UIAlertAction.Create(
					LocalizedCancelString,
					UIAlertActionStyle.Cancel,
					_ => this.Hide()
				))
				.ForEach(_alertController.AddAction);

			Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				switch (UIDevice.CurrentDevice.UserInterfaceIdiom)
				{
					case UIUserInterfaceIdiom.Pad:
						if (placementTarget.Superview != null)
						{
							// This UIView exists in the visual tree.
							_alertController.PopoverPresentationController.SourceView = placementTarget;
							_alertController.PopoverPresentationController.SourceRect = placementTarget.Bounds;
						}
						else if (placementTarget is AppBarButton appBarButton)
						{
							// This AppBarButton doesn't exist in the visual tree and is likely rendered natively as a UIBarButton.
							var barButtonItem = appBarButton.GetRenderer(() => new AppBarButtonRenderer(appBarButton)).Native;
							_alertController.PopoverPresentationController.BarButtonItem = barButtonItem;
						}
						_alertController.PopoverPresentationController.PermittedArrowDirections = UIPopoverArrowDirection.Any;
						placementTarget.FindViewController().PresentViewController(_alertController, true, null);
						break;
					case UIUserInterfaceIdiom.Phone:
						placementTarget.FindViewController().PresentViewController(_alertController, true, null);
						break;
					case UIUserInterfaceIdiom.Unspecified:
						break;
				}
			});

		}

		private void HideAlert()
		{
			_alertController?.DismissViewController(true, null);
			_alertController = null;
		}
	}
}
