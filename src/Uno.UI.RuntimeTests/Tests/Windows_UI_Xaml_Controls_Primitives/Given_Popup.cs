using System;
using System.Threading.Tasks;
using Combinatorial.MSTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives.PopupPages;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Popup
	{
		[TestMethod]
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
		public async Task When_Child_Visual_Parents_Do_Not_Include_Popup()
		{
			var popup = await LoadAndOpenPopupWithButtonAsync();
			bool found = SearchPopupChildAscendants(popup, element => element == popup, element => VisualTreeHelper.GetParent(element));

			Assert.IsFalse(found);

			// Should not throw
			popup.IsOpen = false;
		}

		[TestMethod]
		public async Task When_Child_Logical_Parents_Include_Popup()
		{
			var popup = await LoadAndOpenPopupWithButtonAsync();
			bool found = SearchPopupChildAscendants(popup, element => element == popup, element => (element as FrameworkElement)?.Parent);

			Assert.IsTrue(found);

			// Should not throw
			popup.IsOpen = false;
		}

		[TestMethod]
		public async Task When_Child_Visual_Parent_Is_Canvas()
		{
			var popup = await LoadAndOpenPopupWithButtonAsync();
			var child = (FrameworkElement)popup.Child;
			var parent = VisualTreeHelper.GetParent(child);
			Assert.IsInstanceOfType(parent, typeof(Canvas));
#if HAS_UNO // It is actually a PopupRoot, but it is internal in WinUI
			Assert.IsInstanceOfType(parent, typeof(PopupRoot));
#endif

			// Should not throw
			popup.IsOpen = false;
		}

		[TestMethod]
		public async Task When_Child_Logical_Parent_Is_Popup()
		{
			var popup = await LoadAndOpenPopupWithButtonAsync();
			var child = (FrameworkElement)popup.Child;

			Assert.AreEqual(popup, child.Parent);

			// Should not throw
			popup.IsOpen = false;
		}

#if HAS_UNO // PopupPanel is Uno-specific
		[TestMethod]
		public async Task When_Child_Visual_Parents_Do_Not_Include_PopupPanel()
		{
			var popup = await LoadAndOpenPopupWithButtonAsync();
			bool found = SearchPopupChildAscendants(popup, element => element is PopupPanel, VisualTreeHelper.GetParent);

			Assert.IsFalse(found);

			// Should not throw
			popup.IsOpen = false;
		}

		[TestMethod]
		public async Task When_Child_Logical_Parents_Do_Not_Include_PopupPanel()
		{
			var popup = await LoadAndOpenPopupWithButtonAsync();
			bool found = SearchPopupChildAscendants(popup, element => element is PopupPanel, element => (element as FrameworkElement)?.Parent);

			Assert.IsFalse(found);

			// Should not throw
			popup.IsOpen = false;
		}
#endif

		private async Task<Popup> LoadAndOpenPopupWithButtonAsync()
		{
			var popup = new Popup();
			popup.XamlRoot = TestServices.WindowHelper.XamlRoot;
			var button = new Button()
			{
				Content = "test"
			};
			popup.Child = button;
			popup.IsOpen = true;
			await WindowHelper.WaitForLoaded(button);
			return popup;
		}

		private bool SearchPopupChildAscendants(Popup popup, Predicate<DependencyObject> predicate, Func<DependencyObject, DependencyObject> getParent)
		{
			DependencyObject current = popup.Child;
			while (current != null)
			{
				if (predicate(current))
				{
					return true;
				}

				current = getParent(current);
			}

			return false;
		}

		[TestMethod]
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

			Assert.HasCount(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot));

			stackPanel.Children.Remove(popup);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(popup.IsOpen);
			Assert.IsEmpty(VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot));

			popup.IsOpen = true;
			await WindowHelper.WaitForIdle();

			Assert.HasCount(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot));

			popup.IsOpen = false;
		}

#if HAS_UNO // FeatureConfiguration is Uno-only
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeIOS)] // On iOS native the flyout sizing is not handled differently, so the results are different.
		[CombinatorialData]
		public async Task When_ConstrainedByVisibleBounds(bool constrain)
		{
			var constrainedPreviously = FeatureConfiguration.Popup.ConstrainByVisibleBounds;
			var visibleBoundsDisposable = ScreenHelper.OverrideVisibleBounds(new Thickness(100, 100, 100, 100), false);
			try
			{
				FeatureConfiguration.Popup.ConstrainByVisibleBounds = constrain;

				var content = new Border
				{
					Background = new SolidColorBrush(Windows.UI.Colors.Red),
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
				};
				var popup = new Popup
				{
					Child = content
				};

				popup.DesiredPlacement = PopupPlacementMode.Auto;
				popup.PlacementTarget = (FrameworkElement)WindowHelper.XamlRoot.Content;
				popup.XamlRoot = WindowHelper.XamlRoot;

				popup.IsOpen = true;

				await WindowHelper.WaitForLoaded(content);

				var xamlRoot = WindowHelper.XamlRoot;

				if (constrain)
				{
					var constrainedHeight = xamlRoot.VisualTree.VisibleBounds.Height;
					var constrainedWidth = xamlRoot.VisualTree.VisibleBounds.Width;
					Assert.AreEqual(constrainedHeight, content.ActualHeight);
					Assert.AreEqual(constrainedWidth, content.ActualWidth);
				}
				else
				{
					var unconstrainedHeight = xamlRoot.VisualTree.Size.Height;
					var unconstrainedWidth = xamlRoot.VisualTree.Size.Width;
					Assert.AreEqual(unconstrainedHeight, content.ActualHeight);
					Assert.AreEqual(unconstrainedWidth, content.ActualWidth);
				}
			}
			finally
			{
				visibleBoundsDisposable?.Dispose();
				FeatureConfiguration.Popup.ConstrainByVisibleBounds = constrainedPreviously;
			}
		}
#endif

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

			Assert.HasCount(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot));

			await WindowHelper.WaitForIdle();

			var args = new KeyRoutedEventArgs(popup, VirtualKey.Escape, VirtualKeyModifiers.None);
			popup.SafeRaiseEvent(UIElement.KeyDownEvent, args);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(args.Handled);
			Assert.IsFalse(popup.IsOpen);
			Assert.IsEmpty(VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot));

			popup.IsOpen = true;
			await WindowHelper.WaitForIdle();

			Assert.HasCount(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot));

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

			Assert.IsEmpty(VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot));

			menu.ShowAt(trigger);

			Assert.HasCount(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot));
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
		[CombinatorialData]
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

		[TestMethod]
		public async Task When_Multiple_Popups_Opened_Order_Is_Most_Recent_First()
		{
			// This test validates that GetOpenPopupsForXamlRoot returns popups in order
			// with the most recently opened popup at the head (index 0)
			var popup1 = new Popup
			{
				Child = new Button { Content = "Popup 1" }
			};
			popup1.XamlRoot = WindowHelper.XamlRoot;

			var popup2 = new Popup
			{
				Child = new Button { Content = "Popup 2" }
			};
			popup2.XamlRoot = WindowHelper.XamlRoot;

			var popup3 = new Popup
			{
				Child = new Button { Content = "Popup 3" }
			};
			popup3.XamlRoot = WindowHelper.XamlRoot;

			try
			{
				// Open popups in sequence: 1, 2, 3
				popup1.IsOpen = true;
				await WindowHelper.WaitForIdle();

				popup2.IsOpen = true;
				await WindowHelper.WaitForIdle();

				popup3.IsOpen = true;
				await WindowHelper.WaitForIdle();

				var openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot);

				// Verify count
				Assert.HasCount(3, openPopups, "Should have 3 open popups");

				// Verify order: most recently opened (popup3) should be first
				Assert.AreSame(popup3, openPopups[0], "Most recently opened popup should be at index 0");
				Assert.AreSame(popup2, openPopups[1], "Second most recently opened popup should be at index 1");
				Assert.AreSame(popup1, openPopups[2], "First opened popup should be at index 2");
			}
			finally
			{
				popup1.IsOpen = false;
				popup2.IsOpen = false;
				popup3.IsOpen = false;
			}
		}

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
