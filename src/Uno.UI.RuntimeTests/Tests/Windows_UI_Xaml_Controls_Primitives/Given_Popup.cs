using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives.PopupPages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
using Windows.Media.Capture.Core;
using Windows.System;
using Windows.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Popup
	{
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task Check_Can_Reach_Main_Visual_Tree()
		{
			var page = new ReachMainTreePage();
			WindowHelper.WindowContent = page;

			await WindowHelper.WaitForLoaded(page);

			Assert.IsTrue(CanReach(page.DummyTextBlock, page));

			try
			{
				page.TargetPopup.IsOpen = true;
				await WindowHelper.WaitForLoaded(page.PopupButton);

				Assert.IsTrue(CanReach(page.PopupButton, page));
			}
			finally
			{
				page.TargetPopup.IsOpen = false;
			}
		}

#if __ANDROID__
		[TestMethod]
		public async Task Check_Can_Reach_Main_Visual_Tree_Alternate_Mode()
		{
			var originalConfig = FeatureConfiguration.Popup.UseNativePopup;
			FeatureConfiguration.Popup.UseNativePopup = !originalConfig;
			try
			{
				await Check_Can_Reach_Main_Visual_Tree();
			}
			finally
			{
				FeatureConfiguration.Popup.UseNativePopup = originalConfig;
			}
		}
#endif

		[TestMethod]
		public void When_IsLightDismissEnabled_Default()
		{
			var popup = new Popup();
			Assert.IsFalse(popup.IsLightDismissEnabled);
		}

		[TestMethod]
		public void When_Closed_Immediately()
		{
			var popup = new Popup();
			popup.XamlRoot = TestServices.WindowHelper.XamlRoot;
			popup.IsOpen = true;
			// Should not throw
			popup.IsOpen = false;
		}

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Removed_From_VisualTree()
		{
			var stackPanel = new StackPanel();
			var button = new Button() { Content = "Test" };
			var popup = new Popup()
			{
				Child = new Button() { Content = "Test" }
			};
			stackPanel.Children.Add(button);
			stackPanel.Children.Add(popup);
			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(stackPanel);

			Assert.IsFalse(popup.IsOpen);

			popup.IsOpen = true;

			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);

			stackPanel.Children.Remove(popup);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(popup.IsOpen);
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);

			popup.IsOpen = true;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);

			popup.IsOpen = false;
		}

#if HAS_UNO
		[TestMethod]
		public async Task When_Escape_Handled()
		{
			var popup = new Popup
			{
				Child = new Button { Content = "Test" }
			};
			popup.XamlRoot = WindowHelper.XamlRoot;

			Assert.IsFalse(popup.IsOpen);

			popup.IsOpen = true;

			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);

			await WindowHelper.WaitForIdle();

			var args = new KeyRoutedEventArgs(popup, VirtualKey.Escape, VirtualKeyModifiers.None);
			popup.SafeRaiseEvent(UIElement.KeyDownEvent, args);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(args.Handled);
			Assert.IsFalse(popup.IsOpen);
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);

			popup.IsOpen = true;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);

			popup.IsOpen = false;
		}

		[TestMethod]
		public async Task When_Escape_Canceled()
		{
			var menu = new MenuFlyout();
			menu.Items.Add(new MenuFlyoutItem() { Text = "Text" });
			menu.XamlRoot = WindowHelper.XamlRoot;

			var trigger = new Button();
			await UITestHelper.Load(trigger);

			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);

			menu.ShowAt(trigger);

			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);
			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot)[0];

			menu.Closing += (_, e) => e.Cancel = true;

			var args = new KeyRoutedEventArgs(popup, VirtualKey.Escape, VirtualKeyModifiers.None);
			((UIElement)FocusManager.GetFocusedElement(WindowHelper.XamlRoot))!.SafeRaiseEvent(UIElement.KeyDownEvent, args);
			await WindowHelper.WaitForIdle();

			// It's unclear what the right behavior is, but we don't care.
			// This test just "documents" the current behavior, and can't run on WinUI.
#if __SKIA__ || __WASM__
			Assert.IsTrue(args.Handled);
#else
			Assert.IsFalse(args.Handled);
#endif
			Assert.IsTrue(popup.IsOpen);
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(true)]
		[DataRow(false)]
		public async Task When_CloseLightDismissablePopups(bool isLightDismissEnabled)
		{
			var popup = new Popup()
			{
				Child = new Button() { Content = "Test" },
				IsLightDismissEnabled = isLightDismissEnabled
			};
			try
			{
				TestServices.WindowHelper.WindowContent = popup;
				popup.IsOpen = true;
				await WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Count > 0);
				var popupRoot = TestServices.WindowHelper.XamlRoot.VisualTree.PopupRoot;
				popupRoot.CloseLightDismissablePopups();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(!isLightDismissEnabled, popup.IsOpen);
			}
			finally
			{
				popup.IsOpen = false;
			}
		}
#endif

		private static bool CanReach(DependencyObject startingElement, DependencyObject targetElement)
		{
			var currentElement = startingElement;
			while (currentElement != null)
			{
				if (currentElement == targetElement)
				{
					return true;
				}

				// Quoting WCT DataGrid:
				//		// Walk up the visual tree. Try using the framework element's
				//		// parent.  We do this because Popups behave differently with respect to the visual tree,
				//		// and it could have a parent even if the VisualTreeHelper doesn't find it.
				DependencyObject parent = null;
				if (currentElement is FrameworkElement fe)
				{
					parent = fe.Parent;
				}
				if (parent == null)
				{
					parent = VisualTreeHelper.GetParent(currentElement);
				}

				currentElement = parent;
			}

			// Did not hit targetElement
			return false;
		}
	}
}
