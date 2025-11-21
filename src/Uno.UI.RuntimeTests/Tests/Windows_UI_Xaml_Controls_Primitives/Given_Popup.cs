using System;
using System.Threading.Tasks;
using Combinatorial.MSTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives.PopupPages;
using Windows.Foundation;
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

#if HAS_UNO
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task VerifyBackButtonClosesLightDismissPopup()
		{
			Popup popup1 = null;

			var popupOpenedEvent = new Event();
			var popupClosedEvent = new Event();

			var openedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>("Opened");
			var closedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>("Closed");

			await RunOnUIThread(() =>
			{
				var rootPanel = (StackPanel)(XamlReader.Load(
					"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='400' Height='400' >" +
					"  <Popup x:Name='popup1' IsLightDismissEnabled='True' >" +
					"    <Border Background='Red' Width='100' Height='100' />" +
					"  </Popup>" +
					"</StackPanel>"));

				TestServices.WindowHelper.WindowContent = rootPanel;

				popup1 = (Popup)(rootPanel.FindName("popup1"));

				openedRegistration.Attach(popup1, (sender, e) =>
				{
					popupOpenedEvent.Set();
				});

				closedRegistration.Attach(popup1, (sender, e) =>
				{
					popupClosedEvent.Set();
				});

				popup1.IsOpen = true;
			});

			await popupOpenedEvent.WaitForDefault();

			LOG_OUTPUT("Close the Light Dismiss enabled Popup using the Back button.");
			bool backButtonPressHandled = await TestServices.Utilities.InjectBackButtonPress();
			VERIFY_IS_TRUE(backButtonPressHandled);
			await popupClosedEvent.WaitForDefault();

			LOG_OUTPUT("After closing a Popup, further back button presses should not get handled");
			backButtonPressHandled = await TestServices.Utilities.InjectBackButtonPress();
			VERIFY_IS_FALSE(backButtonPressHandled);

			await RunOnUIThread(() =>
			{
				popup1.IsLightDismissEnabled = false;
				popup1.IsOpen = true;
			});
			await popupOpenedEvent.WaitForDefault();

			LOG_OUTPUT("A Back button press should not dismiss a Popup that is not Light Dismiss enabled");
			backButtonPressHandled = await TestServices.Utilities.InjectBackButtonPress();
			VERIFY_IS_FALSE(backButtonPressHandled);
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(popup1.IsOpen);
			});
		}
#endif

#if HAS_UNO
		[TestMethod]
		public async Task When_NonLightDismiss_Popup_Does_Not_Register_BackListener()
		{
			var manager = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
			bool hadHandlersBefore = manager.HasAnyBackHandlers;

			var popup = new Popup
			{
				IsLightDismissEnabled = false,
				Child = new Border { Width = 100, Height = 100 }
			};
			popup.XamlRoot = TestServices.WindowHelper.XamlRoot;

			try
			{
				popup.IsOpen = true;
				await WindowHelper.WaitForIdle();

				// Non-light-dismiss popup should NOT register as a back listener
				Assert.AreEqual(hadHandlersBefore, manager.HasAnyBackHandlers,
					"HasAnyBackHandlers should not change for non-light-dismiss popup.");

				// Back press should not be handled
				bool handled = await TestServices.Utilities.InjectBackButtonPress();
				Assert.IsFalse(handled, "Back press should not be handled by non-light-dismiss popup.");
				Assert.IsTrue(popup.IsOpen, "Non-light-dismiss popup should remain open after back press.");
			}
			finally
			{
				popup.IsOpen = false;
			}
		}

		[TestMethod]
		public async Task When_LightDismiss_Popup_HasAnyBackHandlers_Transitions()
		{
			var manager = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();

			var popup = new Popup
			{
				IsLightDismissEnabled = true,
				Child = new Border { Width = 100, Height = 100 }
			};
			popup.XamlRoot = TestServices.WindowHelper.XamlRoot;

			try
			{
				var hadHandlersBefore = manager.HasAnyBackHandlers;

				popup.IsOpen = true;
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(manager.HasAnyBackHandlers,
					"Should have handlers after light-dismiss popup opens.");

				popup.IsOpen = false;
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(hadHandlersBefore, manager.HasAnyBackHandlers,
					"Should return to the same state as before the popup was opened.");
			}
			finally
			{
				popup.IsOpen = false;
			}
		}
#endif

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

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PlacementTarget_In_ScrollViewer_And_Scrolled()
		{
			var scrollViewer = new ScrollViewer
			{
				Height = 200,
				Width = 300
			};

			var stackPanel = new StackPanel();

			// Add some spacer elements
			for (int i = 0; i < 10; i++)
			{
				stackPanel.Children.Add(new Border { Height = 50 });
			}

			// Add the placement target
			var placementTarget = new Border
			{
				Width = 100,
				Height = 50,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Blue)
			};
			stackPanel.Children.Add(placementTarget);

			// Add more spacer elements
			for (int i = 0; i < 10; i++)
			{
				stackPanel.Children.Add(new Border { Height = 50 });
			}

			scrollViewer.Content = stackPanel;

			var popupChild = new Border
			{
				Width = 80,
				Height = 40,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
			};

			var flyout = new Flyout
			{
				XamlRoot = WindowHelper.XamlRoot,
				Content = popupChild
			};

			FlyoutBase.SetAttachedFlyout(placementTarget, flyout);

			try
			{
				TestServices.WindowHelper.WindowContent = scrollViewer;
				await WindowHelper.WaitForLoaded(scrollViewer);

				// Open the popup
				FlyoutBase.ShowAttachedFlyout(placementTarget);
				await TestServices.WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count > 0);
				await WindowHelper.WaitForIdle();

				// Get the initial position of the popup child relative to the window
				var initialPosition = popupChild.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));

				// Scroll down
				scrollViewer.ChangeView(null, 200, null, disableAnimation: true);
				await WindowHelper.WaitForIdle();

				// Get the new position of the popup child
				var newPosition = popupChild.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));

				// The popup should not move with the scroll (Y position should have decreased)
				Assert.AreEqual(initialPosition.Y, newPosition.Y, "Popup should not move when scrolling");
				Assert.IsFalse(newPosition.Y < initialPosition.Y, "Popup should not move when scrolling down");
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Popup_In_Visual_Tree_And_Scroll()
		{
			var scrollViewer = new ScrollViewer
			{
				Height = 200,
				Width = 300
			};

			var stackPanel = new StackPanel();

			// Add some spacer elements
			for (int i = 0; i < 10; i++)
			{
				stackPanel.Children.Add(new Border { Height = 50 });
			}

			// Add the placement target
			var placementTarget = new Popup
			{
				Width = 100,
				Height = 50,
			};
			stackPanel.Children.Add(placementTarget);

			// Add more spacer elements
			for (int i = 0; i < 10; i++)
			{
				stackPanel.Children.Add(new Border { Height = 50 });
			}

			scrollViewer.Content = stackPanel;

			var popupChild = new Border
			{
				Width = 80,
				Height = 40,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
			};

			placementTarget.Child = popupChild;

			try
			{
				TestServices.WindowHelper.WindowContent = scrollViewer;
				await WindowHelper.WaitForLoaded(scrollViewer);

				// Open the popup
				placementTarget.IsOpen = true;
				await TestServices.WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count > 0);

				// Get the initial position of the popup child relative to the window
				var initialPosition = popupChild.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));
				await WindowHelper.WaitForIdle();

				// Scroll down
				scrollViewer.ChangeView(null, 200, null, disableAnimation: true);
				Point newPosition = default;
				await WindowHelper.WaitFor(() =>
				{
					// Get the new position of the popup child
					newPosition = popupChild.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));
					return initialPosition != newPosition;
				});

				// The popup should not move with the scroll (Y position should have decreased)
				Assert.AreNotEqual(initialPosition.Y, newPosition.Y, "Popup should move when scrolling");
				Assert.IsTrue(newPosition.Y < initialPosition.Y, "Popup should move when scrolling down");
			}
			finally
			{
				placementTarget.IsOpen = false;
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
