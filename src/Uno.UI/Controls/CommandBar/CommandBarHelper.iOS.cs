#nullable enable
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using UIKit;
using Uno.UI.Extensions;

namespace Uno.UI.Controls
{
	public static class CommandBarHelper
	{
		internal static void SetNavigationBar(CommandBar commandBar, UIKit.UINavigationBar? navigationBar)
		{
			commandBar.GetRenderer(() => new CommandBarRenderer(commandBar)).Native = navigationBar;
		}

		internal static void SetNavigationItem(CommandBar commandBar, UIKit.UINavigationItem? navigationItem)
		{
			commandBar.GetRenderer(() => new CommandBarNavigationItemRenderer(commandBar)).Native = navigationItem;
		}

		/// <summary>
		/// Finds and configures the <see cref="CommandBar" /> for a given page <see cref="UIViewController" />.
		/// </summary>
		/// <param name="pageController">The controller of the page</param>
		public static void PageCreated(UIViewController pageController)
		{
			var topCommandBar = pageController.FindTopCommandBar();
			if (topCommandBar == null)
			{
				// The default CommandBar style contains information that might be relevant to all pages, including those without a CommandBar.
				// For example the Uno.UI.Toolkit.CommandBarExtensions.BackButtonTitle attached property is often set globally to "" through
				// a default CommandBar style in order to remove the back button text throughout an entire application.
				// In order to leverage this information, we create a new CommandBar instance that only exists to "render" the NavigationItem.
				// Since Uno 3.0 objects which are not part of the Visualtree does not get the Global Styles applied. Hence the fact we are manually applying it here.
				topCommandBar = new CommandBar
				{
					Style = Application.Current.Resources[typeof(CommandBar)] as Style
				};
			}

			// Hook CommandBar to NavigationItem
			SetNavigationItem(topCommandBar, pageController.NavigationItem);
		}

		/// <summary>
		/// Cleanups the <see cref="CommandBar" /> of a page <see cref="UIViewController" />.
		/// </summary>
		/// <param name="pageController">The controller of the page</param>
		public static void PageDestroyed(UIViewController pageController)
		{
			if (pageController.FindTopCommandBar() is { } topCommandBar)
			{
				SetNavigationItem(topCommandBar, null);
			}
		}

		/// <summary>
		/// When a page <see cref="UIViewController" /> will appear, connects the <see cref="CommandBar" /> to the navigation controller.
		/// </summary>
		/// <param name="pageController">The controller of the page</param>
		public static void PageWillAppear(UIViewController pageController)
		{
			if (pageController.NavigationController is not null)
			{
				var topCommandBar = pageController.FindTopCommandBar();
				if (topCommandBar != null)
				{
					if (topCommandBar.Visibility == Visibility.Visible)
					{
						SetNavigationBar(topCommandBar, pageController.NavigationController.NavigationBar);

						// When the CommandBar is visible, we need to call SetNavigationBarHidden
						// AFTER it has been rendered. Otherwise, it causes a bug introduced
						// in iOS 11 in which the BackButtonIcon is not rendered properly.
						pageController.NavigationController.SetNavigationBarHidden(hidden: false, animated: true);
					}
					else
					{
						// Even if the CommandBar should technically be collapsed,
						// we don't hide it using the NavigationController because it
						// automatically disables the back gesture.
						// In order to visually hide it, the CommandBarRenderer
						// will hide the native view using the UIView.Hidden property.
						pageController.NavigationController.SetNavigationBarHidden(hidden: false, animated: true);

						SetNavigationBar(topCommandBar, pageController.NavigationController.NavigationBar);
					}
				}
				else // No CommandBar
				{
					pageController.NavigationController.SetNavigationBarHidden(true, true);
				}
			}
		}

		/// <summary>
		/// When a page <see cref="UIViewController" /> did disappear, disconnects the <see cref="CommandBar" /> from the navigation controller.
		/// </summary>
		/// <param name="pageController">The controller of the page</param>
		public static void PageDidDisappear(UIViewController pageController)
		{
			if (pageController.FindTopCommandBar() is { } topCommandBar)
			{
				// Set the native navigation bar to null so it does not render when the page is not visible
				SetNavigationBar(topCommandBar, null);
			}
		}

		public static void PageWillDisappear(UIViewController pageController)
		{
			if (pageController.FindTopCommandBar() is { } topCommandBar)
			{
				// Set the native navigation bar to null so it does not render when the page is not visible
				SetNavigationBar(topCommandBar, null);
			}
		}

		private static CommandBar? FindTopCommandBar(this UIViewController controller)
		{
			return (controller.View as Page)?.TopAppBar as CommandBar
				?? FindTopCommandBar(controller.View);
		}

		internal static CommandBar? FindTopCommandBar(UIView? view)
		{
			return view.FindFirstDescendant<CommandBar>(
				x => x is not Frame, // prevent looking into the nested page
				_ => true
			);
		}
	}
}
