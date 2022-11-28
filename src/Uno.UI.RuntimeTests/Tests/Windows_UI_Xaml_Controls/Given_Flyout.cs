using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Flyout
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Unloaded_Before_Shown()
		{
			var button = new Button()
			{
				Flyout = new Flyout
				{
					Content = new Border { Width = 50, Height = 30 }
				}
			};

			TestServices.WindowHelper.WindowContent = button;

			await TestServices.WindowHelper.WaitForIdle();

			TestServices.WindowHelper.WindowContent = null;

			await TestServices.WindowHelper.WaitForIdle();
		}

#if IS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_LoadedAndUnloaded_Check_Binding()
		{

			var (flyout, content) = CreateFlyoutWithBindingMultipleChildren();

			const double MarginValue = 105;
			const int TargetWidth = 88;

			var border = new Border()
			{
				DataContext = "My Data Context"
			};

			var buttonA = new Button()
			{
				Content = "Main Button",
				Flyout = flyout,
			};

			border.Child = buttonA;

			TestServices.WindowHelper.WindowContent = border;

			await TestServices.WindowHelper.WaitForLoaded(buttonA);

			await TestServices.WindowHelper.WaitForIdle();

			buttonA.RaiseClick();

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.WindowHelper.WaitForLoaded(content);

			flyout.Hide();

			await TestServices.WindowHelper.WaitForIdle();

			border.Child = null;

			await TestServices.WindowHelper.WaitForIdle();

			border.DataContext = "A New Context";
			border.Child = buttonA;

			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.WindowHelper.WaitForLoaded(buttonA);

			buttonA.RaiseClick();

			await TestServices.WindowHelper.WaitForLoaded(content);

			var stackPanel = content as StackPanel;

			Assert.AreEqual("A New Context", (stackPanel.Children[0] as TextBlock).Text);

			flyout.Hide();

	}
#endif

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Attached_To_Border_Check_Placement()
		{
			var (flyout, content) = CreateFlyout();

			const double MarginValue = 105;
			const int TargetWidth = 88;
			var target = new Border
			{
				Margin = new Thickness(MarginValue),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Width = TargetWidth,
				Height = 23,
				Background = new SolidColorBrush(Colors.Red)
			};

			TestServices.WindowHelper.WindowContent = target;

			await TestServices.WindowHelper.WaitForLoaded(target);

			await TestServices.WindowHelper.WaitFor(() => target.ActualWidth == TargetWidth); // For some reason target is initially stretched on iOS

			try
			{
				FlyoutBase.SetAttachedFlyout(target, flyout);
				FlyoutBase.ShowAttachedFlyout(target);

				await TestServices.WindowHelper.WaitForLoaded(content);

				var contentCenter = content.GetOnScreenBounds().GetCenter();
				var targetCenter = target.GetOnScreenBounds().GetCenter();

				Assert.IsTrue(targetCenter.X > MarginValue);
				Assert.AreEqual(targetCenter.X, contentCenter.X, delta: 2);
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Attached_To_TextBlock_Check_Placement()
		{
			var (flyout, content) = CreateFlyout();

			const double MarginValue = 105;
			var target = new TextBlock
			{
				Margin = new Thickness(MarginValue),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Text = "Tweetle beetle battle"
			};

			TestServices.WindowHelper.WindowContent = target;

			await TestServices.WindowHelper.WaitForLoaded(target);

			try
			{
				FlyoutBase.SetAttachedFlyout(target, flyout);
				FlyoutBase.ShowAttachedFlyout(target);

				await TestServices.WindowHelper.WaitForLoaded(content);

				var contentCenter = content.GetOnScreenBounds().GetCenter();
				var targetCenter = target.GetOnScreenBounds().GetCenter();

				Assert.IsTrue(targetCenter.X > MarginValue);
				Assert.AreEqual(targetCenter.X, contentCenter.X, delta: 2);
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
		[DataRow(FlyoutPlacementMode.Top, HorizontalPosition.Center, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.Bottom, HorizontalPosition.Center, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.Left, HorizontalPosition.BeyondLeft, VerticalPosition.Center)]
		[DataRow(FlyoutPlacementMode.Right, HorizontalPosition.BeyondRight, VerticalPosition.Center)]
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedTop, HorizontalPosition.BeyondLeft, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedBottom, HorizontalPosition.BeyondLeft, VerticalPosition.BottomFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedTop, HorizontalPosition.BeyondRight, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedBottom, HorizontalPosition.BeyondRight, VerticalPosition.BottomFlush)]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task Check_Placement_All(
			FlyoutPlacementMode placementMode,
			HorizontalPosition horizontalPosition,
			VerticalPosition verticalPosition)
		{
			var (flyout, content) = CreateFlyout();

			flyout.Placement = placementMode;

			const double MarginValue = 97;
			const int TargetWidth = 88;
			var target = new Border
			{
				Margin = new Thickness(MarginValue),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Width = TargetWidth,
				Height = 23,
				Background = new SolidColorBrush(Colors.Red)
			};

			TestServices.WindowHelper.WindowContent = target;

			await TestServices.WindowHelper.WaitForLoaded(target);
			await TestServices.WindowHelper.WaitFor(() => target.ActualWidth == TargetWidth); // For some reason target is initially stretched on iOS

			try
			{
				FlyoutBase.SetAttachedFlyout(target, flyout);
				FlyoutBase.ShowAttachedFlyout(target);

				await TestServices.WindowHelper.WaitForLoaded(content);

				VerifyRelativeContentPosition(horizontalPosition, verticalPosition, content, MarginValue, target);
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
		[DataRow(FlyoutPlacementMode.Top, HorizontalPosition.Center, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.Bottom, HorizontalPosition.Center, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.Left, HorizontalPosition.BeyondLeft, VerticalPosition.Center)]
		[DataRow(FlyoutPlacementMode.Right, HorizontalPosition.BeyondRight, VerticalPosition.Center)]
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedTop, HorizontalPosition.BeyondLeft, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedBottom, HorizontalPosition.BeyondLeft, VerticalPosition.BottomFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedTop, HorizontalPosition.BeyondRight, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedBottom, HorizontalPosition.BeyondRight, VerticalPosition.BottomFlush)]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task Check_Placement_All_WithPosition(
			FlyoutPlacementMode placementMode,
			HorizontalPosition horizontalPosition,
			VerticalPosition verticalPosition)
		{
			var (flyout, content) = CreateFlyout();
			var position = new Windows.Foundation.Point(50, 50);
			var options = new FlyoutShowOptions
			{
				Placement = placementMode,
				Position = position,
			};

			const double MarginValue = 97;
			const int TargetWidth = 88;
			var target = new Border
			{
				Margin = new Thickness(MarginValue),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Width = TargetWidth,
				Height = 23,
				Background = new SolidColorBrush(Colors.Red)
			};

			TestServices.WindowHelper.WindowContent = target;

			await TestServices.WindowHelper.WaitForLoaded(target);
			await TestServices.WindowHelper.WaitFor(() => target.ActualWidth == TargetWidth); // For some reason target is initially stretched on iOS

			try
			{
				flyout.ShowAt(target, options);

				await TestServices.WindowHelper.WaitForLoaded(content);

				VerifyRelativeContentPosition(position, horizontalPosition, verticalPosition, content, MarginValue, target);
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
#if __ANDROID__
		[Ignore("Popup successfully fits left-aligned on Android - possibly because the status bar offset changes the layouting?")]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Too_Large_For_Any_Fallback()
		{
			var target = new TextBlock
			{
				Text = "Anchor",
				VerticalAlignment = VerticalAlignment.Bottom
			};

			var stretchedTargetWrapper = new Border
			{
				MinHeight = 600,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = target
			};

			var windowHeight = ApplicationView.GetForCurrentView().VisibleBounds.Height;

			var flyoutContent = new Ellipse
			{
				Width = 90,
				Height = windowHeight * 1.5,
				Fill = new SolidColorBrush(Colors.Tomato)
			};

			var presenterStyle = new Style
			{
				TargetType = typeof(FlyoutPresenter),
				Setters =
				{
					new Setter(FrameworkElement.MaxHeightProperty, windowHeight*1.7)
				}
			};

			var flyout = new Flyout
			{
				FlyoutPresenterStyle = presenterStyle,
				Content = flyoutContent,
				Placement = FlyoutPlacementMode.Top
			};

			TestServices.WindowHelper.WindowContent = stretchedTargetWrapper;

			await TestServices.WindowHelper.WaitForLoaded(target);

			try
			{
				FlyoutBase.SetAttachedFlyout(target, flyout);
				FlyoutBase.ShowAttachedFlyout(target);

				await TestServices.WindowHelper.WaitForLoaded(flyoutContent);

				var contentScreenBounds = flyoutContent.GetOnScreenBounds();
				var contentCenter = contentScreenBounds.GetCenter();
				var targetScreenBounds = target.GetOnScreenBounds();
				var targetCenter = targetScreenBounds.GetCenter();

				var presenter = await TestServices.WindowHelper.WaitForNonNull(() => flyoutContent.FindFirstParent<FlyoutPresenter>());

				VerifyRelativeContentPosition(HorizontalPosition.Center,
					VerticalPosition.BeyondTop,
					presenter, // The content itself is in a ScrollViewer and its bounds will exceed the visible area
					0,
					target
				);
			}
			finally
			{
				flyout.Hide();
			}

		}

		[TestMethod]
		[DataRow(FlyoutPlacementMode.Top, HorizontalPosition.Center, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.Bottom, HorizontalPosition.Center, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.Left, HorizontalPosition.BeyondLeft, VerticalPosition.Center)]
		[DataRow(FlyoutPlacementMode.Right, HorizontalPosition.BeyondRight, VerticalPosition.Center)]
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedTop, HorizontalPosition.BeyondLeft, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedBottom, HorizontalPosition.BeyondLeft, VerticalPosition.BottomFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedTop, HorizontalPosition.BeyondRight, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedBottom, HorizontalPosition.BeyondRight, VerticalPosition.BottomFlush)]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task Check_Placement_All_MenuFlyout(
			FlyoutPlacementMode placementMode,
			HorizontalPosition horizontalPosition,
			VerticalPosition verticalPosition)
		{
			var flyout = CreateBasicMenuFlyout();

			flyout.Placement = placementMode;

			const double MarginValue = 97;
			const int TargetWidth = 88;
			var target = new Border
			{
				Margin = new Thickness(MarginValue),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Width = TargetWidth,
				Height = 23,
				Background = new SolidColorBrush(Colors.Red)
			};

			TestServices.WindowHelper.WindowContent = target;

			await TestServices.WindowHelper.WaitForLoaded(target);
			await TestServices.WindowHelper.WaitFor(() => target.ActualWidth == TargetWidth); // For some reason target is initially stretched on iOS

			try
			{
				FlyoutBase.SetAttachedFlyout(target, flyout);
				FlyoutBase.ShowAttachedFlyout(target);

				var presenter = flyout.Presenter;

				await TestServices.WindowHelper.WaitForLoaded(presenter);

				var content = presenter.FindFirstChild<ScrollViewer>();

				VerifyRelativeContentPosition(horizontalPosition, verticalPosition, content, MarginValue, target);
			}
			finally
			{
				flyout.Hide();
			}
		}

#if HAS_UNO && !__MACOS__ // For macOS, see https://github.com/unoplatform/uno/issues/626 
		[TestMethod]
		[RunsOnUIThread]
		public async Task Test_Flyout_Binding()
		{

			var (flyout, content) = CreateFlyoutWithBinding();

			var buttonA = new Button()
			{
				Content = "Button A",
				Flyout = flyout,
				DataContext = "My Data Context",
			};

			TestServices.WindowHelper.WindowContent = buttonA;

			await TestServices.WindowHelper.WaitForLoaded(buttonA);

			buttonA.RaiseClick();

			await TestServices.WindowHelper.WaitForLoaded(content);

			var stackPanel = content as StackPanel;
			Assert.AreEqual("My Data Context", (stackPanel.Children[0] as TextBlock).Text);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task Test_Flyout_Binding_In_MenuFlyoutItem()
		{
			var menuBar = new MenuBar();
			var menuBarItem = new MenuBarItem();
			var menuFlyoutItem = new MenuFlyoutItem();

			menuFlyoutItem.SetBinding(MenuFlyoutItem.TextProperty, new Binding { Path = "." });

			menuBar.Items.Add(menuBarItem);
			menuBarItem.Items.Add(menuFlyoutItem);

			menuBar.DataContext = "42";

			TestServices.WindowHelper.WindowContent = menuBar;

			await TestServices.WindowHelper.WaitForLoaded(menuBar);

			menuBarItem.Invoke();

			await TestServices.WindowHelper.WaitForLoaded(menuFlyoutItem);

			Assert.AreEqual("42", menuFlyoutItem.Text);

			menuBarItem.CloseMenuFlyout();
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task Test_Flyout_Binding_With_SetAttachedFlyout()
		{
			Border b1 = new()
			{
				DataContext = "41",
				Width = 10,
				Height = 10,
			};
			Border b2 = new()
			{
				DataContext = "42",
				Width = 10,
				Height = 10,
			};

			var stackPanel = new StackPanel()
			{
				Children = { b1, b2 }
			};

			TestServices.WindowHelper.WindowContent = stackPanel;
			await TestServices.WindowHelper.WaitForLoaded(stackPanel);

			var tb = new TextBlock();
			tb.SetBinding(TextBlock.TextProperty, new Binding { Path = "." });
			var SUT = new Flyout
			{
				Content = tb
			};

			FlyoutBase.SetAttachedFlyout(b1, SUT);
			FlyoutBase.SetAttachedFlyout(b2, SUT);

			FlyoutBase.ShowAttachedFlyout(b1);
			await TestServices.WindowHelper.WaitForLoaded(tb);
			Assert.AreEqual("41", tb.Text);
			SUT.Close();

			FlyoutBase.ShowAttachedFlyout(b2);
			await TestServices.WindowHelper.WaitForLoaded(tb);
			Assert.AreEqual("42", tb.Text);
			SUT.Close();
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Flyout_Content_Takes_Focus()
		{
			var stackPanel = new StackPanel();
			var button = new Button() { Content = "Flyout owner" };
			stackPanel.Children.Add(button);
			TestServices.WindowHelper.WindowContent = stackPanel;
			await TestServices.WindowHelper.WaitForIdle();

			var flyout = new Flyout();
			var flyoutButton = new Button() { Content = "Flyout content" };
			flyout.Content = flyoutButton;
			FlyoutBase.SetAttachedFlyout(button, flyout);
			button.Focus(FocusState.Pointer);

			Assert.AreEqual(button, FocusManager.GetFocusedElement());

			FlyoutBase.ShowAttachedFlyout(button);
			flyoutButton.Focus(FocusState.Pointer);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreNotEqual(button, FocusManager.GetFocusedElement());

			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(button, FocusManager.GetFocusedElement());

			TestServices.WindowHelper.WindowContent = null;
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Flyout_Has_Focusable_Child()
		{
			var stackPanel = new StackPanel();
			var button = new Button() { Content = "Flyout owner" };
			stackPanel.Children.Add(button);
			TestServices.WindowHelper.WindowContent = stackPanel;
			await TestServices.WindowHelper.WaitForIdle();

			var flyout = new Flyout();
			var flyoutButton = new Button() { Content = "Flyout content" };
			flyout.Content = flyoutButton;
			FlyoutBase.SetAttachedFlyout(button, flyout);
			button.Focus(FocusState.Pointer);

			Assert.AreEqual(button, FocusManager.GetFocusedElement());

			FlyoutBase.ShowAttachedFlyout(button);
			await TestServices.WindowHelper.WaitForIdle();

			var focused = FocusManager.GetFocusedElement();
			Assert.IsInstanceOfType(focused, typeof(Popup));

			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.WindowHelper.WindowContent = null;
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Hide_Always_Closing()
		{
			Flyout flyout = new Flyout();
			var closingCalled = false;
			flyout.Closing += (sender, args) => closingCalled = true;

			flyout.Hide();
			Assert.IsTrue(closingCalled);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Opening_Canceled()
		{
			Flyout flyout = new Flyout();
			try
			{
				var button = new Button()
				{
					Content = "Test"
				};
				TestServices.WindowHelper.WindowContent = button;
				await TestServices.WindowHelper.WaitForIdle();

				
				var innerBorder = new Border()
				{
					Width = 100,
					Height = 100
				};
				flyout.Content = innerBorder;
				flyout.Opening += (sender, args) => ((Flyout)sender).Hide();
				flyout.ShowAt(button);

				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsFalse(flyout.IsOpen);
				Assert.IsFalse(innerBorder.IsLoaded);
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Opening_And_Closing_Canceled()
		{
			Flyout flyout = new Flyout();
			bool cancelClosing = true;
			try
			{
				var button = new Button()
				{
					Content = "Test"
				};
				TestServices.WindowHelper.WindowContent = button;
				await TestServices.WindowHelper.WaitForIdle();


				var innerBorder = new Border()
				{
					Width = 100,
					Height = 100
				};
				flyout.Content = innerBorder;
				flyout.Opening += (sender, args) => ((Flyout)sender).Hide();
				
				void OnClosing(object sender, FlyoutBaseClosingEventArgs args)
				{
					args.Cancel = cancelClosing;
				}
				flyout.Closing += OnClosing;
				flyout.ShowAt(button);

				await TestServices.WindowHelper.WaitForIdle();
				
				Assert.IsTrue(flyout.IsOpen);
				Assert.IsTrue(innerBorder.IsLoaded);
			}
			finally
			{
				cancelClosing = false;
				flyout.Hide();
			}
		}

		private static void VerifyRelativeContentPosition(HorizontalPosition horizontalPosition, VerticalPosition verticalPosition, FrameworkElement content, double minimumTargetOffset, FrameworkElement target)
		{
			var contentScreenBounds = content.GetOnScreenBounds();
			var contentCenter = contentScreenBounds.GetCenter();
			var targetScreenBounds = target.GetOnScreenBounds();
			var targetCenter = targetScreenBounds.GetCenter();

			Assert.IsTrue(targetCenter.X > minimumTargetOffset);
			Assert.IsTrue(targetCenter.Y > minimumTargetOffset);
			switch (horizontalPosition)
			{
				case HorizontalPosition.BeyondLeft:
					NumberAssert.LessOrEqual(contentScreenBounds.Right, targetScreenBounds.Left);
					break;
				case HorizontalPosition.LeftFlush:
					Assert.AreEqual(targetScreenBounds.Left, contentScreenBounds.Left, delta: 2);
					break;
				case HorizontalPosition.Center:
					Assert.AreEqual(targetCenter.X, contentCenter.X, delta: 2);
					break;
				case HorizontalPosition.RightFlush:
					Assert.AreEqual(targetScreenBounds.Right, contentScreenBounds.Right, delta: 2);
					break;
				case HorizontalPosition.BeyondRight:
					NumberAssert.GreaterOrEqual(contentScreenBounds.Left, targetScreenBounds.Right);
					break;
			}

			switch (verticalPosition)
			{
				case VerticalPosition.BeyondTop:
					NumberAssert.LessOrEqual(contentScreenBounds.Bottom, targetScreenBounds.Top);
					break;
				case VerticalPosition.TopFlush:
					Assert.AreEqual(targetScreenBounds.Top, contentScreenBounds.Top, delta: 2);
					break;
				case VerticalPosition.Center:
					Assert.AreEqual(targetCenter.Y, contentCenter.Y, delta: 2);
					break;
				case VerticalPosition.BottomFlush:
					Assert.AreEqual(targetScreenBounds.Bottom, contentScreenBounds.Bottom, delta: 2);
					break;
				case VerticalPosition.BeyondBottom:
					NumberAssert.GreaterOrEqual(contentScreenBounds.Top, targetScreenBounds.Bottom);
					break;
			}
		}

		private static void VerifyRelativeContentPosition(Windows.Foundation.Point position, HorizontalPosition horizontalPosition, VerticalPosition verticalPosition, FrameworkElement content, double minimumTargetOffset, FrameworkElement target)
		{
			var contentScreenBounds = content.GetOnScreenBounds();
#if __ANDROID__
			if (FeatureConfiguration.Popup.UseNativePopup)
			{
				// Adjust for status bar height, which is omitted from TransformToVisual() for elements inside of a native popup.
				var rootViewBounds = ((FrameworkElement)Window.Current.Content).GetOnScreenBounds();
				contentScreenBounds.Y += rootViewBounds.Y;
			}
#endif
			var contentCenter = contentScreenBounds.GetCenter();
			var targetScreenBounds = target.GetOnScreenBounds();
			var targetCenter = targetScreenBounds.GetCenter();
			var anchorPoint = new Windows.Foundation.Point(targetScreenBounds.X + position.X, targetScreenBounds.Y + position.Y);

			Assert.IsTrue(targetCenter.X > minimumTargetOffset);
			Assert.IsTrue(targetCenter.Y > minimumTargetOffset);
			switch (horizontalPosition)
			{
				case HorizontalPosition.BeyondLeft:
					NumberAssert.GreaterOrEqual(anchorPoint.X, contentScreenBounds.Right);
					break;
				case HorizontalPosition.LeftFlush:
					Assert.AreEqual(anchorPoint.X, contentScreenBounds.Left, delta: 2);
					break;
				case HorizontalPosition.Center:
					Assert.AreEqual(anchorPoint.X, contentCenter.X, delta: 2);
					break;
				case HorizontalPosition.RightFlush:
					Assert.AreEqual(anchorPoint.X, contentScreenBounds.Right, delta: 2);
					break;
				case HorizontalPosition.BeyondRight:
					NumberAssert.LessOrEqual(anchorPoint.X, contentScreenBounds.Left);
					break;
			}

			switch (verticalPosition)
			{
				case VerticalPosition.BeyondTop:
					NumberAssert.GreaterOrEqual(anchorPoint.Y, contentScreenBounds.Bottom);
					break;
				case VerticalPosition.TopFlush:
					Assert.AreEqual(anchorPoint.Y, contentScreenBounds.Top, delta: 2);
					break;
				case VerticalPosition.Center:
					Assert.AreEqual(anchorPoint.Y, contentCenter.Y, delta: 2);
					break;
				case VerticalPosition.BottomFlush:
					Assert.AreEqual(anchorPoint.Y, contentScreenBounds.Bottom, delta: 2);
					break;
				case VerticalPosition.BeyondBottom:
					NumberAssert.LessOrEqual(anchorPoint.Y, contentScreenBounds.Top);
					break;
			}
		}

		private (Flyout Flyout, FrameworkElement Content) CreateFlyout()
		{
			var content = new Grid { Height = 64, Width = 64, Background = new SolidColorBrush(Colors.Green) };
			var flyout = new Flyout
			{
				Content = content,
				FlyoutPresenterStyle = GetSimpleFlyoutPresenterStyle()
			};
			return (flyout, content);
		}

		private (Flyout Flyout, FrameworkElement Content) CreateFlyoutWithBinding()
		{

			var flyout = new Flyout
			{
				FlyoutPresenterStyle = GetSimpleFlyoutPresenterStyle()
			};

			var content = new StackPanel
			{
				Children =
				{
					new TextBlock().Apply(x => x.SetBinding(TextBlock.TextProperty, new Binding())),
					new Button
					{
						Content = "Button B",
					}.Apply(x => x.SetBinding(Button.CommandParameterProperty, new Binding() { Source = flyout }))
				}
			};

			flyout.Content = content;
			return (flyout, content);
		}

		private (Flyout Flyout, FrameworkElement Content) CreateFlyoutWithBindingMultipleChildren()
		{
			var flyout = new Flyout
			{
				FlyoutPresenterStyle = GetSimpleFlyoutPresenterStyle()
			};

			var content = new StackPanel
			{
				Children =
				{
					new TextBlock().Apply(x => x.SetBinding(TextBlock.TextProperty, new Binding())),
					new Button
					{
						Content = "Button A",
					}.Apply(x => x.SetBinding(Button.CommandParameterProperty, new Binding() { Source = flyout })),
					new Button
					{
						Content = "Button B",
					}.Apply(x => x.SetBinding(Button.CommandParameterProperty, new Binding() { Source = flyout })),
					new Button
					{
						Content = "Button C",
					}.Apply(x => x.SetBinding(Button.CommandParameterProperty, new Binding() { Source = flyout })),
					new Button
					{
						Content = "Button D",
					}.Apply(x => x.SetBinding(Button.CommandParameterProperty, new Binding() { Source = flyout }))
				}
			};

			flyout.Content = content;
			return (flyout, content);
		}

		private static Style GetSimpleFlyoutPresenterStyle() => new Style
		{
			TargetType = typeof(FlyoutPresenter),
			Setters =
					{
						new Setter(FlyoutPresenter.PaddingProperty, new Thickness(0)),
						new Setter(FlyoutPresenter.MinWidthProperty, 0d),
						new Setter(FlyoutPresenter.MinHeightProperty, 0d),
						new Setter(FlyoutPresenter.BorderThicknessProperty, new Thickness(0)),
					}
		};

		private MyMenuFlyout CreateBasicMenuFlyout()
		{
			var flyout = new MyMenuFlyout
			{
				Items =
				{
					new MenuFlyoutItem { Text = "Red" },
					new MenuFlyoutItem { Text = "Blue" },
					new MenuFlyoutItem { Text = "Green" },
				}
			};

			return flyout;
		}

		public enum HorizontalPosition
		{
			BeyondLeft,
			LeftFlush,
			Center,
			RightFlush,
			BeyondRight
		}

		public enum VerticalPosition
		{
			BeyondTop,
			TopFlush,
			Center,
			BottomFlush,
			BeyondBottom
		}
	}

	public partial class MyMenuFlyout : MenuFlyout
	{
		public MenuFlyoutPresenter Presenter { get; private set; }

		protected override Control CreatePresenter()
		{
			var presenter = base.CreatePresenter();
			Presenter = presenter as MenuFlyoutPresenter;
			return presenter;
		}
	}
}
