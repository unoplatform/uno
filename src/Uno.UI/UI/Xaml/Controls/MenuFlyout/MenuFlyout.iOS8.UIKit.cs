using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Client;
using Uno.Extensions;
using UIKit;
using Uno.UI.Controls;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class MenuFlyout
	{
		private static DependencyProperty IsDestructiveProperty = ToolkitHelper.GetProperty("Uno.UI.Toolkit.MenuFlyoutItemExtensions, Uno.UI.Toolkit", "IsDestructive");

		private UIAlertController _alertController;

		internal override UIView NativeTarget
		{
			get
			{
				if (Target is AppBarButton appBarButton
					&& appBarButton.Superview == null
					&& appBarButton.Parent is CommandBar commandBar
					&& GetNativeAppBarButton(appBarButton) is { } nativeAppBarButton
					&& commandBar.GetRenderer(() => new CommandBarRenderer(commandBar)).Native is { } navigationBar)
				{
					// If the AppBarButton is a proxy for a native navigation bar button, find the corresponding native view.

					if (nativeAppBarButton.Image is { } image) // AppBarButton.Icon is set
					{
						return navigationBar.FindSubviewsOfType<UIImageView>().FirstOrDefault(v => v.Image == image);
					}
					else if (!nativeAppBarButton.Title.IsNullOrEmpty()) // AppBarButton.Content is set to a string
					{
						return navigationBar.FindSubviewsOfType<UIButton>()
							.SelectMany(b => b.FindSubviewsOfType<UILabel>())
							.FirstOrDefault(l => l.Text == nativeAppBarButton.Title);
					}
					// Don't worry about the CustomView case (AppBarButton.Content is a FrameworkElement) - if it's set, it means the
					// AppBarButton is attached to the visual tree and can be used as Target in the standard way
				}

				return null;
			}
		}

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
					GetValue(CancelTextIosOverrideProperty) as string ?? LocalizedCancelString,
					UIAlertActionStyle.Cancel,
					_ => this.Hide()
				))
				.ForEach(_alertController.AddAction);

			_ = Dispatcher.RunAsync(global::Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				switch (UIDevice.CurrentDevice.UserInterfaceIdiom)
				{
#if !__TVOS__
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
							var barButtonItem = GetNativeAppBarButton(appBarButton);
							_alertController.PopoverPresentationController.BarButtonItem = barButtonItem;
						}
						_alertController.PopoverPresentationController.PermittedArrowDirections = UIPopoverArrowDirection.Any;
						placementTarget.FindViewController().PresentViewController(_alertController, true, null);
						break;
#endif
					case UIUserInterfaceIdiom.Phone or UIUserInterfaceIdiom.TV:
						placementTarget.FindViewController().PresentViewController(_alertController, true, null);
						break;
					case UIUserInterfaceIdiom.Unspecified:
						break;
				}
			});

		}

		private static UIBarButtonItem GetNativeAppBarButton(AppBarButton appBarButton) => appBarButton.GetRenderer(() => new AppBarButtonRenderer(appBarButton)).Native;

		private void HideAlert()
		{
			_alertController?.DismissViewController(true, null);
			_alertController = null;
		}
	}
}
