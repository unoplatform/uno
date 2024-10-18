using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppKit;
using Foundation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class NativeMenuBarPresenter : FrameworkElement
	{
		private MenuBar _menuBar;

		public NativeMenuBarPresenter()
		{
			Loaded += MenuBarPresenter_Loaded;
			Unloaded += MenuBarPresenter_Unloaded;
		}

		private void MenuBarPresenter_Unloaded(object sender, RoutedEventArgs e)
		{
			NSApplication.SharedApplication.MainMenu = null;
		}

		private void MenuBarPresenter_Loaded(object sender, RoutedEventArgs e)
		{
			_menuBar = this.FindFirstParent<MenuBar>() ?? throw new InvalidOperationException($"MenuBarPresenter must be used with a MenuBar control");

			NSMenu menubar = new NSMenu();
			menubar.AutoEnablesItems = false;

			NSApplication.SharedApplication.MainMenu = menubar;

			foreach (var item in _menuBar.Items)
			{
				NSMenuItem appMenuItem = new NSMenuItem() { Enabled = true };
				menubar.AddItem(appMenuItem);

				AddSubMenus(appMenuItem, item);
			}
		}

		private void AddSubMenus(NSMenuItem platformItem, MenuBarItem item)
		{
			platformItem.Submenu = new NSMenu(item.Title) { AutoEnablesItems = false };

			if (item.Items.Any())
			{
				ProcessMenuItems(platformItem, item.Items);
			}
		}

		private void ProcessMenuItems(NSMenuItem platformItem, IList<MenuFlyoutItemBase> items)
		{
			if (items != null)
			{

				if (platformItem.Submenu == null)
				{
					platformItem.Submenu = new NSMenu();
				}

				foreach (var subItem in items)
				{
					switch (subItem)
					{
						case MenuFlyoutSubItem flyoutSubItem:
							var subPlatformItem = new NSMenuItem(flyoutSubItem.Text) { Enabled = true };
							platformItem.Submenu.AddItem(subPlatformItem);
							ProcessMenuItems(subPlatformItem, flyoutSubItem.Items);
							break;

						case MenuFlyoutItem flyoutItem:
							var subPlatformItem2 = new NSMenuItem(flyoutItem.Text, (s, e) => OnItemActivated(flyoutItem)) { Enabled = true };
							flyoutItem.InvokeClick();
							platformItem.Submenu.AddItem(subPlatformItem2);
							break;

						case MenuFlyoutSeparator separatorItem:
							platformItem.Submenu.AddItem(NSMenuItem.SeparatorItem);
							break;
					}
				}
			}
		}

		private void OnItemActivated(MenuFlyoutItem flyoutItem)
		{
			flyoutItem.InvokeClick();
		}

		protected override Size MeasureOverride(Size availableSize)
			=> MeasureFirstChild(availableSize);

		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeFirstChild(finalSize);
	}
}
