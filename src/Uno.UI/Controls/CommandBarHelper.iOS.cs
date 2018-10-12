#if __IOS__
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using UIKit;

namespace Uno.UI.Controls
{
	public static class CommandBarHelper
	{
		internal static void SetNavigationBar(CommandBar commandBar, UIKit.UINavigationBar navigationBar)
		{
			commandBar.GetRenderer(() => new CommandBarRenderer(commandBar)).Native = navigationBar;
		}

		internal static void SetNavigationItem(CommandBar commandBar, UIKit.UINavigationItem navigationItem)
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
				topCommandBar = new CommandBar();
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
			var topCommandBar = pageController.FindTopCommandBar();
			if (topCommandBar != null)
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
			var topCommandBar = pageController.FindTopCommandBar();
			if (topCommandBar != null)
			{
				// Hook CommandBar to NavigationBar
				// We do it here because we know the NavigationController is set (and NavigationBar is available)
				SetNavigationBar(topCommandBar, pageController.NavigationController.NavigationBar);

				// We call this method after rendering the CommandBar to work around buggy behaviour on iOS 11, but we have to make 
				// sure not to overwrite the visibility set by the renderer.
				var isHidden = topCommandBar.Visibility == Visibility.Collapsed;
				pageController.NavigationController.SetNavigationBarHidden(hidden: isHidden, animated: true);

				if (isHidden && pageController.NavigationController.InteractivePopGestureRecognizer != null)
				{
					//set a gesture recognizer in the case that the UINavigationBar is hidden
					var callback = new Uno.UI.Helpers.NativeFramePresenterUIGestureRecognizerDelegate(() => pageController.NavigationController);
					pageController.NavigationController.InteractivePopGestureRecognizer.Delegate = callback;
				}
			}
			else // No CommandBar
			{
				pageController.NavigationController.SetNavigationBarHidden(true, true);
			}
		}

		/// <summary>
		/// When a page <see cref="UIViewController" /> did disappear, disconnects the <see cref="CommandBar" /> from the navigation controller.
		/// </summary>
		/// <param name="pageController">The controller of the page</param>
		public static void PageDidDisappear(UIViewController pageController)
		{
			var topCommandBar = pageController.FindTopCommandBar();
			if (topCommandBar != null)
			{
				// Set the native navigation bar to null so it does not render when the page is not visible
				SetNavigationBar(topCommandBar, null);
			}
		}

		private static CommandBar FindTopCommandBar(this UIViewController controller)
		{
			return (controller.View as Page)?.TopAppBar as CommandBar
				?? controller.View.FindFirstChild<CommandBar>();
		}
	}
}
#endif
