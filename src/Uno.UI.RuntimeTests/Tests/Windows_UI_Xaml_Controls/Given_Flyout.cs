using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.FlyoutPages;
using Uno.UI.RuntimeTests.FramePages;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.Extensions;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using MenuBar = Microsoft/* UWP don't rename */.UI.Xaml.Controls.MenuBar;
using MenuBarItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.MenuBarItem;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Flyout
	{
		private string GetAllIsOpens() => string.Join(" ", VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Select(p => p.IsOpen));

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

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_LoadedAndUnloaded_Check_Binding()
		{

			var (flyout, content) = CreateFlyoutWithBindingMultipleChildren();

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
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				windowHeight = TestServices.WindowHelper.XamlRoot.Size.Height;
			}

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

			Assert.AreEqual(button, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));

			FlyoutBase.ShowAttachedFlyout(button);
			flyoutButton.Focus(FocusState.Pointer);
			await TestServices.WindowHelper.WaitForIdle();

			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutButton.XamlRoot)[0];

			Assert.AreEqual(popup.Visibility, Visibility.Visible);
			Assert.AreNotEqual(button, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));

			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();

			// The visibility of the popup remains on, but it's closed.
			Assert.AreEqual(popup.Visibility, Visibility.Visible);
			Assert.AreEqual(button, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));

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
			await TestServices.WindowHelper.WaitForLoaded(button);

			var flyout = new Flyout();
			var flyoutButton = new Button() { Content = "Flyout content" };
			flyout.Content = flyoutButton;
			FlyoutBase.SetAttachedFlyout(button, flyout);
			button.Focus(FocusState.Pointer);

			Assert.AreEqual(button, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));

			FlyoutBase.ShowAttachedFlyout(button);
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitForLoaded(flyoutButton);

			Assert.AreEqual(flyoutButton, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));

			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.WindowHelper.WindowContent = null;
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PlacementTarget_Binding()
		{
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				Assert.Inconclusive($"Not supported under XAML islands yet https://github.com/unoplatform/uno/issues/8978");
			}

			var SUT = new When_PlacementTarget_Binding();
			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);

			try
			{
				SUT.DataContext = 42;

				Assert.AreEqual(SUT.DataContext, SUT.myButton.Content);

				SUT.contextFlyout.ShowAt(SUT.myButton);

				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(SUT.DataContext, SUT.myButton.Content);
			}
			finally
			{
#if HAS_UNO
				SUT.contextFlyout.Close();
#endif
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Opening_And_Closing_Nested_Flyouts()
		{
			var flyout1 = new Flyout();
			var flyout2 = new Flyout();
			var flyout3 = new Flyout();
			var flyout4 = new Flyout();
			var flyout5 = new Flyout();
			try
			{
				var button1 = new Button
				{
					Content = "button1",
					Flyout = flyout1
				};

				var button2 = new Button
				{
					Content = "button2",
					Flyout = flyout2
				};

				var button3 = new Button
				{
					Content = "button3",
					Flyout = flyout3
				};

				var button4 = new Button
				{
					Content = "button4",
					Flyout = flyout4
				};

				var button5 = new Button
				{
					Content = "button5",
					Flyout = flyout5
				};

				flyout1.Content = button2;
				flyout2.Content = button3;
				flyout3.Content = button4;
				flyout4.Content = button5;
				flyout5.Content = new TextBox { Text = "text" };

				var output = "";

				flyout1.Closing += (_, args) => output += $"closing1 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";
				flyout2.Closing += (_, args) => output += $"closing2 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";
				flyout3.Closing += (_, args) => output += $"closing3 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";
				flyout4.Closing += (_, args) => output += $"closing4 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";
				flyout5.Closing += (_, args) => output += $"closing5 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";

				flyout1.Closed += (_, _) => output += $"closed1 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()}\n";
				flyout2.Closed += (_, _) => output += $"closed2 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()}\n";
				flyout3.Closed += (_, _) => output += $"closed3 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()}\n";
				flyout4.Closed += (_, _) => output += $"closed4 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()}\n";
				flyout5.Closed += (_, _) => output += $"closed5 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()}\n";

				await UITestHelper.Load(button1);

				var xamlRoot = button1.XamlRoot;
				Assert.IsNotNull(xamlRoot);
				Assert.AreEqual(TestServices.WindowHelper.XamlRoot, xamlRoot);
				flyout1.XamlRoot = xamlRoot;
				flyout2.XamlRoot = xamlRoot;
				flyout3.XamlRoot = xamlRoot;
				flyout4.XamlRoot = xamlRoot;
				flyout5.XamlRoot = xamlRoot;

				flyout1.ShowAt(button1);
				await TestServices.WindowHelper.WaitForLoaded(button2);
				flyout2.ShowAt(button2);
				await TestServices.WindowHelper.WaitForLoaded(button3);
				flyout3.ShowAt(button3);
				await TestServices.WindowHelper.WaitForLoaded(button4);
				flyout4.ShowAt(button4);
				await TestServices.WindowHelper.WaitForLoaded(button5);
				flyout5.ShowAt(button5);
				await TestServices.WindowHelper.WaitForIdle();

				flyout1.Hide();
				await TestServices.WindowHelper.WaitForIdle();

#if UNO_HAS_ENHANCED_LIFECYCLE
				var expected =
				"""
				closing1 True True True True True True True True True True False
				closing2 True True True True True True True True True True False
				closing3 True True True True True True True True True True False
				closing4 True True True True True True True True True True False
				closing5 True True True True True True True True True True False
				closed1 False False False False False 
				closing3 False False False False False  False
				closing4 False False False False False  False
				closing5 False False False False False  False
				closed2 False False False False False 
				closing4 False False False False False  False
				closing5 False False False False False  False
				closed3 False False False False False 
				closing5 False False False False False  False
				closed4 False False False False False 
				closed5 False False False False False 

				""";
#elif HAS_UNO
				var expected =
				"""
				closing1 True True True True True True True True True True False
				closing2 True True True True True True True True True True False
				closing3 True True True True True True True True True True False
				closing4 True True True True True True True True True True False
				closing5 True True True True True True True True True True False
				closed1 False False False False False 
				closing2 False False False False False  False
				closing3 False False False False False  False
				closing4 False False False False False  False
				closing5 False False False False False  False
				closed2 False False False False False 
				closing3 False False False False False  False
				closing4 False False False False False  False
				closing5 False False False False False  False
				closed3 False False False False False 
				closing4 False False False False False  False
				closing5 False False False False False  False
				closed4 False False False False False 
				closing5 False False False False False  False
				closed5 False False False False False 

				""";
#else
				var expected =
				"""
				closing1 True True True True True True True True True True False
				closing2 True True True True True True True True True True False
				closing3 True True True True True True True True True True False
				closing4 True True True True True True True True True True False
				closing5 True True True True True True True True True True False
				closed5 False False False False False 

				""";
#endif

				Assert.AreEqual(expected.Replace("\r\n", "\n"), output);
			}
			finally
			{
				flyout1.Hide();
				flyout2.Hide();
				flyout3.Hide();
				flyout4.Hide();
				flyout5.Hide();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Opening_And_Closing_Nested_Flyouts_Not_Open()
		{
			var flyout1 = new Flyout();
			var flyout2 = new Flyout();
			try
			{
				var button1 = new Button
				{
					Content = "button1",
					Flyout = flyout1
				};

				var button2 = new Button
				{
					Content = "button2",
					Flyout = flyout2
				};

				flyout1.Content = button2;
				flyout2.Content = new TextBox { Text = "text" };

				var output = "";

				flyout1.Closing += (_, args) => output += $"closing1 {flyout1.IsOpen} {flyout2.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";
				flyout2.Closing += (_, args) => output += $"closing2 {flyout1.IsOpen} {flyout2.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";

				flyout1.Closed += (_, _) => output += $"closed1 {flyout1.IsOpen} {flyout2.IsOpen} {GetAllIsOpens()}\n";
				flyout2.Closed += (_, _) => output += $"closed2 {flyout1.IsOpen} {flyout2.IsOpen} {GetAllIsOpens()}\n";

				TestServices.WindowHelper.WindowContent = button1;
				await TestServices.WindowHelper.WaitForIdle();

				var xamlRoot = button1.XamlRoot;
				Assert.IsNotNull(xamlRoot);
				Assert.AreEqual(TestServices.WindowHelper.XamlRoot, xamlRoot);
				flyout1.XamlRoot = xamlRoot;
				flyout2.XamlRoot = xamlRoot;

				flyout1.ShowAt(button1);
				await TestServices.WindowHelper.WaitForIdle();

				// flyout2 is not open, so Closing should NOT be invoked.

				flyout1.Hide();
				await TestServices.WindowHelper.WaitForIdle();

				var expected =
				"""
				closing1 True False True False
				closed1 False False 

				""";

				Assert.AreEqual(expected.Replace("\r\n", "\n"), output);
			}
			finally
			{
				flyout1.Hide();
				flyout2.Hide();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#else
		[Ignore("Fails")]
#endif
		public async Task When_Opening_And_Closing_Nested_Flyouts_Canceled()
		{
			var flyout1 = new Flyout();
			var flyout2 = new Flyout();
			var flyout3 = new Flyout();
			var flyout4 = new Flyout();
			var flyout5 = new Flyout();
			var cancel = true;
			try
			{
				var button1 = new Button
				{
					Content = "button1",
					Flyout = flyout1
				};

				var button2 = new Button
				{
					Content = "button2",
					Flyout = flyout2
				};

				var button3 = new Button
				{
					Content = "button3",
					Flyout = flyout3
				};

				var button4 = new Button
				{
					Content = "button4",
					Flyout = flyout4
				};

				var button5 = new Button
				{
					Content = "button5",
					Flyout = flyout5
				};

				flyout1.Content = button2;
				flyout2.Content = button3;
				flyout3.Content = button4;
				flyout4.Content = button5;
				flyout5.Content = new TextBox { Text = "text" };

				var output = "";

				flyout1.Closing += (_, args) => output += $"closing1 {flyout1.IsOpen} {flyout1.IsOpen} {flyout1.IsOpen} {flyout1.IsOpen} {flyout1.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";
				flyout2.Closing += (_, args) => output += $"closing2 {flyout2.IsOpen} {flyout2.IsOpen} {flyout2.IsOpen} {flyout2.IsOpen} {flyout2.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";
				flyout3.Closing += (_, args) =>
				{
					output += $"closing3 {flyout3.IsOpen} {flyout3.IsOpen} {flyout3.IsOpen} {flyout3.IsOpen} {flyout3.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";
					args.Cancel = cancel;
				};
				flyout4.Closing += (_, args) => output += $"closing4 {flyout4.IsOpen} {flyout4.IsOpen} {flyout4.IsOpen} {flyout4.IsOpen} {flyout4.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";
				flyout5.Closing += (_, args) => output += $"closing5 {flyout5.IsOpen} {flyout5.IsOpen} {flyout5.IsOpen} {flyout5.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()} {args.Cancel}\n";

				flyout1.Closed += (_, _) => output += $"closed1 {flyout1.IsOpen} {flyout1.IsOpen} {flyout1.IsOpen} {flyout1.IsOpen} {flyout1.IsOpen} {GetAllIsOpens()}\n";
				flyout2.Closed += (_, _) => output += $"closed2 {flyout2.IsOpen} {flyout2.IsOpen} {flyout2.IsOpen} {flyout2.IsOpen} {flyout2.IsOpen} {GetAllIsOpens()}\n";
				flyout3.Closed += (_, _) => output += $"closed3 {flyout3.IsOpen} {flyout3.IsOpen} {flyout3.IsOpen} {flyout3.IsOpen} {flyout3.IsOpen} {GetAllIsOpens()}\n";
				flyout4.Closed += (_, _) => output += $"closed4 {flyout4.IsOpen} {flyout4.IsOpen} {flyout4.IsOpen} {flyout4.IsOpen} {flyout4.IsOpen} {GetAllIsOpens()}\n";
				flyout5.Closed += (_, _) => output += $"closed5 {flyout5.IsOpen} {flyout5.IsOpen} {flyout5.IsOpen} {flyout5.IsOpen} {flyout5.IsOpen} {GetAllIsOpens()}\n";

				TestServices.WindowHelper.WindowContent = button1;
				await TestServices.WindowHelper.WaitForIdle();

				var xamlRoot = button1.XamlRoot;
				Assert.IsNotNull(xamlRoot);
				Assert.AreEqual(TestServices.WindowHelper.XamlRoot, xamlRoot);
				flyout1.XamlRoot = xamlRoot;
				flyout2.XamlRoot = xamlRoot;
				flyout3.XamlRoot = xamlRoot;
				flyout4.XamlRoot = xamlRoot;
				flyout5.XamlRoot = xamlRoot;

				flyout1.ShowAt(button1);
				await TestServices.WindowHelper.WaitForIdle();
				flyout2.ShowAt(button2);
				await TestServices.WindowHelper.WaitForIdle();
				flyout3.ShowAt(button3);
				await TestServices.WindowHelper.WaitForIdle();
				flyout4.ShowAt(button4);
				await TestServices.WindowHelper.WaitForIdle();
				flyout5.ShowAt(button5);
				await TestServices.WindowHelper.WaitForIdle();

				flyout1.Hide();
				await TestServices.WindowHelper.WaitForIdle();

				var expected =
				"""
				closing1 True True True True True True True True True True False
				closing2 True True True True True True True True True True False
				closing3 True True True True True True True True True True False
				closed1 False False False False False True True True
				closing2 False False False False False True True True False
				closing3 True True True True True True True True False
				closed2 False False False False False True True True
				closing3 True True True True True True True True False

				""";

				Assert.AreEqual(expected.Replace("\r\n", "\n"), output);
			}
			finally
			{
				cancel = false;
				flyout1.Hide();
				flyout2.Hide();
				flyout3.Hide();
				flyout4.Hide();
				flyout5.Hide();
			}
		}

		//#if HAS_UNO
		//		[TestMethod]
		//		[RunsOnUIThread]
		//#if __MACOS__
		//		[Ignore("Currently fails on macOS, part of #9282 epic")]
		//#endif
		//		public async Task When_Window_Unfocused()
		//		{
		//			var flyout1 = new Flyout();
		//			try
		//			{
		//				var button1 = new Button
		//				{
		//					Content = "button1",
		//					Flyout = flyout1
		//				};

		//				flyout1.Content = new TextBox { Text = "text" };

		//				var output = "";

		//				flyout1.Closing += (_, args) => output += $"closing1 {flyout1.IsOpen} {args.Cancel}\n";
		//				flyout1.Closed += (_, _) => output += $"closed1 {flyout1.IsOpen}\n";

		//				TestServices.WindowHelper.WindowContent = button1;
		//				await TestServices.WindowHelper.WaitForIdle();

		//				flyout1.ShowAt(button1);
		//				await TestServices.WindowHelper.WaitForIdle();

		//				Window.Current.OnNativeActivated(CoreWindowActivationState.Deactivated);
		//				await TestServices.WindowHelper.WaitForIdle();

		//				var expected =
		//				"""
		//				closing1 True False
		//				closed1 False

		//				""";

		//				Assert.AreEqual(expected.Replace("\r\n", "\n"), output);
		//			}
		//			finally
		//			{
		//				Window.Current.OnNativeActivated(CoreWindowActivationState.CodeActivated);
		//				flyout1.Hide();
		//			}
		//		}

		//#if __SKIA__ || __WASM__
		//		[TestMethod]
		//		[RunsOnUIThread]
		//		public async Task When_Window_Resized()
		//		{
		//			var flyout1 = new Flyout();
		//			try
		//			{
		//				var button1 = new Button
		//				{
		//					Content = "button1",
		//					Flyout = flyout1
		//				};

		//				flyout1.Content = new TextBox { Text = "text" };

		//				var output = "";

		//				flyout1.Closing += (_, args) => output += $"closing1 {flyout1.IsOpen} {args.Cancel}\n";
		//				flyout1.Closed += (_, _) => output += $"closed1 {flyout1.IsOpen}\n";

		//				TestServices.WindowHelper.WindowContent = button1;
		//				await TestServices.WindowHelper.WaitForIdle();

		//				flyout1.ShowAt(button1);
		//				await TestServices.WindowHelper.WaitForIdle();

		//				Window.Current.OnNativeSizeChanged(Window.Current.Bounds.Size.Add(new Size(0.0001, 0)));
		//				await TestServices.WindowHelper.WaitForIdle();

		//				var expected =
		//				"""
		//				closing1 True False
		//				closed1 False

		//				""";

		//				Assert.AreEqual(expected.Replace("\r\n", "\n"), output);
		//			}
		//			finally
		//			{
		//				Window.Current.OnNativeSizeChanged(Window.Current.Bounds.Size.Subtract(new Size(0.0001, 0)));
		//				flyout1.Hide();
		//			}
		//		}
		//#endif

		//		[TestMethod]
		//		[RunsOnUIThread]
		//#if __MACOS__
		//		[Ignore("Currently fails on macOS, part of #9282 epic")]
		//#endif
		//		public async Task When_Window_Unfocused_Canceled()
		//		{
		//			var flyout1 = new Flyout();
		//			var cancel = true;
		//			try
		//			{
		//				var button1 = new Button
		//				{
		//					Content = "button1",
		//					Flyout = flyout1
		//				};

		//				flyout1.Content = new TextBox { Text = "text" };

		//				var output = "";

		//				flyout1.Closing += (_, args) =>
		//				{
		//					output += $"closing1 {flyout1.IsOpen} {args.Cancel}\n";
		//					args.Cancel = cancel;
		//				};
		//				flyout1.Closed += (_, _) => output += $"closed1 {flyout1.IsOpen}\n";

		//				TestServices.WindowHelper.WindowContent = button1;
		//				await TestServices.WindowHelper.WaitForIdle();

		//				flyout1.ShowAt(button1);
		//				await TestServices.WindowHelper.WaitForIdle();

		//				Window.Current.OnNativeActivated(CoreWindowActivationState.Deactivated);
		//				await TestServices.WindowHelper.WaitForIdle();

		//				var expected =
		//					"""
		//					closing1 True False

		//					""";

		//				Assert.AreEqual(expected.Replace("\r\n", "\n"), output);
		//			}
		//			finally
		//			{
		//				cancel = false;
		//				Window.Current.OnNativeActivated(CoreWindowActivationState.CodeActivated);
		//				flyout1.Hide();
		//			}
		//		}

		//#if __SKIA__ || __WASM__
		//		[TestMethod]
		//		[RunsOnUIThread]
		//		public async Task When_Window_Resized_Canceled()
		//		{
		//			var flyout1 = new Flyout();
		//			var cancel = true;
		//			try
		//			{
		//				var button1 = new Button
		//				{
		//					Content = "button1",
		//					Flyout = flyout1
		//				};

		//				flyout1.Content = new TextBox { Text = "text" };

		//				var output = "";

		//				flyout1.Closing += (_, args) =>
		//				{
		//					output += $"closing1 {flyout1.IsOpen} {args.Cancel}\n";
		//					args.Cancel = cancel;
		//				};
		//				flyout1.Closed += (_, _) => output += $"closed1 {flyout1.IsOpen}\n";

		//				TestServices.WindowHelper.WindowContent = button1;
		//				await TestServices.WindowHelper.WaitForIdle();

		//				flyout1.ShowAt(button1);
		//				await TestServices.WindowHelper.WaitForIdle();

		//				Window.Current.OnNativeSizeChanged(Window.Current.Bounds.Size.Add(new Size(0.0001, 0)));
		//				await TestServices.WindowHelper.WaitForIdle();

		//				var expected =
		//					"""
		//					closing1 True False

		//					""";

		//				Assert.AreEqual(expected.Replace("\r\n", "\n"), output);
		//			}
		//			finally
		//			{
		//				Window.Current.OnNativeSizeChanged(Window.Current.Bounds.Size.Subtract(new Size(0.0001, 0)));
		//				cancel = false;
		//				flyout1.Hide();
		//			}
		//		}
		//#endif

		//		[TestMethod]
		//		[RunsOnUIThread]
		//#if __MACOS__
		//		[Ignore("Currently fails on macOS, part of #9282 epic")]
		//#endif
		//		public async Task When_Window_Unfocused_Nested_Flyouts()
		//		{
		//			var flyout1 = new Flyout();
		//			var flyout2 = new Flyout();
		//			var flyout3 = new Flyout();
		//			var flyout4 = new Flyout();
		//			var flyout5 = new Flyout();
		//			try
		//			{
		//				var button1 = new Button
		//				{
		//					Content = "button1",
		//					Flyout = flyout1
		//				};

		//				var button2 = new Button
		//				{
		//					Content = "button2",
		//					Flyout = flyout2
		//				};

		//				var button3 = new Button
		//				{
		//					Content = "button3",
		//					Flyout = flyout3
		//				};

		//				var button4 = new Button
		//				{
		//					Content = "button4",
		//					Flyout = flyout4
		//				};

		//				var button5 = new Button
		//				{
		//					Content = "button5",
		//					Flyout = flyout5
		//				};

		//				flyout1.Content = button2;
		//				flyout2.Content = button3;
		//				flyout3.Content = button4;
		//				flyout4.Content = button5;
		//				flyout5.Content = new TextBox { Text = "text" };

		//				var output = "";

		//				flyout1.Closing += (_, args) => output += $"closing1 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout1)} {args.Cancel}\n";
		//				flyout2.Closing += (_, args) => output += $"closing2 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout2)} {args.Cancel}\n";
		//				flyout3.Closing += (_, args) => output += $"closing3 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout3)} {args.Cancel}\n";
		//				flyout4.Closing += (_, args) => output += $"closing4 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout4)} {args.Cancel}\n";
		//				flyout5.Closing += (_, args) => output += $"closing5 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout5)} {args.Cancel}\n";

		//				flyout1.Closed += (_, _) => output += $"closed1 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout1)}\n";
		//				flyout2.Closed += (_, _) => output += $"closed2 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout2)}\n";
		//				flyout3.Closed += (_, _) => output += $"closed3 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout3)}\n";
		//				flyout4.Closed += (_, _) => output += $"closed4 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout4)}\n";
		//				flyout5.Closed += (_, _) => output += $"closed5 {flyout1.IsOpen} {flyout2.IsOpen} {flyout3.IsOpen} {flyout4.IsOpen} {flyout5.IsOpen} {GetAllIsOpens(flyout5)}\n";

		//				TestServices.WindowHelper.WindowContent = button1;
		//				await TestServices.WindowHelper.WaitForIdle();

		//				flyout1.ShowAt(button1);
		//				await TestServices.WindowHelper.WaitForIdle();
		//				flyout2.ShowAt(button2);
		//				await TestServices.WindowHelper.WaitForIdle();
		//				flyout3.ShowAt(button3);
		//				await TestServices.WindowHelper.WaitForIdle();
		//				flyout4.ShowAt(button4);
		//				await TestServices.WindowHelper.WaitForIdle();
		//				flyout5.ShowAt(button5);
		//				await TestServices.WindowHelper.WaitForIdle();

		//				Window.Current.OnNativeActivated(CoreWindowActivationState.Deactivated);
		//				await TestServices.WindowHelper.WaitForIdle();

		//				var expected =
		//				"""
		//				closing5 True True True True True True True True True True False
		//				closing4 True True True True False True True True True False
		//				closing5 True True True True False True True True True False
		//				closing3 True True True False False True True True False
		//				closing4 True True True False False True True True False
		//				closing5 True True True False False True True True False
		//				closing2 True True False False False True True False
		//				closing3 True True False False False True True False
		//				closing4 True True False False False True True False
		//				closing5 True True False False False True True False
		//				closing1 True False False False False True False
		//				closing2 True False False False False True False
		//				closing3 True False False False False True False
		//				closing4 True False False False False True False
		//				closing5 True False False False False True False
		//				closed1 False False False False False 
		//				closing2 False False False False False  False
		//				closing3 False False False False False  False
		//				closing4 False False False False False  False
		//				closing5 False False False False False  False
		//				closed2 False False False False False 
		//				closing3 False False False False False  False
		//				closing4 False False False False False  False
		//				closing5 False False False False False  False
		//				closed3 False False False False False 
		//				closing4 False False False False False  False
		//				closing5 False False False False False  False
		//				closed4 False False False False False 
		//				closing5 False False False False False  False
		//				closed5 False False False False False 

		//				""";

		//				Assert.AreEqual(expected.Replace("\r\n", "\n"), output);
		//			}
		//			finally
		//			{
		//				Window.Current.OnNativeActivated(CoreWindowActivationState.CodeActivated);
		//				flyout1.Hide();
		//				flyout2.Hide();
		//				flyout3.Hide();
		//				flyout4.Hide();
		//				flyout5.Hide();
		//			}
		//		}
		//#endif

		[TestMethod]
		public async Task When_Opening_XamlRootIsSet()
		{
			var flyout = new Flyout();
			try
			{
				var host = new Button() { Content = "Asd" };
				TestServices.WindowHelper.WindowContent = host;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForLoaded(host);

				var capture = default(XamlRoot);

				flyout.Opening += (s, e) => capture = (s as Flyout).XamlRoot;

				flyout.ShowAt(host);
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();
				flyout.Hide();

				Assert.AreEqual(host.XamlRoot, capture, "Flyout did not inherit the XamlRoot from its placementTarget.");
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
		public async Task When_Button_ContextFlyout_XamlRoot()
		{
			var flyout = new Flyout();
			var host = new Button() { Content = "Asd" };
			host.ContextFlyout = flyout;
			TestServices.WindowHelper.WindowContent = host;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitForLoaded(host);

			Assert.AreEqual(host.XamlRoot, flyout.XamlRoot);
		}

		[TestMethod]
		public async Task When_SplitButton_Flyout_XamlRoot()
		{
			var flyout = new Flyout();
			var host = new Microsoft/* UWP don't rename */.UI.Xaml.Controls.SplitButton() { Content = "Asd" };
			host.Flyout = flyout;
			TestServices.WindowHelper.WindowContent = host;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitForLoaded(host);

			Assert.AreEqual(host.XamlRoot, flyout.XamlRoot);
		}

		[TestMethod]
		public async Task When_Button_Flyout_XamlRoot()
		{
			var flyout = new Flyout();
			var host = new Button() { Content = "Asd" };
			host.Flyout = flyout;
			TestServices.WindowHelper.WindowContent = host;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitForLoaded(host);

			Assert.AreEqual(host.XamlRoot, flyout.XamlRoot);
		}

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Flyout_Popup_XamlRoot()
		{
			var flyout = new Flyout();
			try
			{
				var host = new Button() { Content = "Asd" };
				flyout.Content = new Button() { Content = "Test" };

				TestServices.WindowHelper.WindowContent = host;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForLoaded(host);

				flyout.ShowAt(host);
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot);
				Assert.AreEqual(host.XamlRoot, flyout.XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].Child.XamlRoot);
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
		public async Task When_AttachedFlyout_Popup_XamlRoot()
		{
			var flyout = new Flyout();
			try
			{
				var host = new Button() { Content = "Asd" };
				flyout.Content = new Button() { Content = "Test" };
				FlyoutBase.SetAttachedFlyout(host, flyout);

				TestServices.WindowHelper.WindowContent = host;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForLoaded(host);

				Assert.IsNull(flyout.XamlRoot);
				FlyoutBase.ShowAttachedFlyout(host);

				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot);
				Assert.AreEqual(host.XamlRoot, flyout.XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].Child.XamlRoot);
			}
			finally
			{
				flyout.Hide();
			}
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		[DataRow(true)]
		[DataRow(false)]
		public async Task When_CloseLightDismissablePopups(bool isLightDismissEnabled)
		{
			var flyout = new Flyout()
			{
				Content = new Button() { Content = "Test" }
			};
			bool opened = false;
			flyout.Opened += (s, e) =>
			{
				var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyout.XamlRoot).FirstOrDefault(p => p.AssociatedFlyout == flyout);
				Assert.IsNotNull(popup);
				popup.IsLightDismissEnabled = isLightDismissEnabled;
				opened = true;
			};
			try
			{
				var ownerButton = new Button()
				{
					Content = "Owner",
					Flyout = flyout
				};
				TestServices.WindowHelper.WindowContent = ownerButton;
				await TestServices.WindowHelper.WaitForLoaded(ownerButton);
				((IInvokeProvider)ownerButton.GetAutomationPeer()).Invoke();
				await TestServices.WindowHelper.WaitFor(() => opened);
				var popupRoot = ownerButton.XamlRoot.VisualTree.PopupRoot;
				popupRoot.CloseLightDismissablePopups();
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual(!isLightDismissEnabled, flyout.IsOpen);
			}
			finally
			{
				flyout?.Hide();
			}
		}
#endif

#if __IOS__
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Native_DatePickerFlyout_Placement()
		{
			var flyout = new NativeDatePickerFlyout();
			try
			{
				var grid = new Grid();
				var host = new Button() { Content = "Asd", Margin = new Thickness(30, 0) };
				grid.Children.Add(host);
				flyout.Content = new Button() { Content = "Test" };
				FlyoutBase.SetAttachedFlyout(host, flyout);

				TestServices.WindowHelper.WindowContent = grid;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForLoaded(grid);

				FlyoutBase.ShowAttachedFlyout(host);

				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot);
				var popupPanel = popups[0].PopupPanel;
				var child = popupPanel.Children[0];
				var transform = child.TransformToVisual(grid);
				var topLeft = transform.TransformPoint(default);
				Assert.AreEqual(0, topLeft.X); // Positioned on the left edge of the screen
				Assert.IsTrue(topLeft.Y > 100); // Positioned lower on the screen
			}
			finally
			{
				flyout.Hide();
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Unbound_FullFlyout()
		{
			var host = new Button
			{
				Content = "Asd",
				Flyout = new Flyout
				{
					Placement = FlyoutPlacementMode.Full,
					FlyoutPresenterStyle = new Style
					{
						TargetType = typeof(FlyoutPresenter),
						Setters =
						{
							// style reset
							new Setter(FlyoutPresenter.MarginProperty, new Thickness(0)),
							new Setter(FlyoutPresenter.PaddingProperty, new Thickness(0)),
							new Setter(FlyoutPresenter.BorderThicknessProperty, new Thickness(0)),

							// remove limit from default style
							// Note that NaN isn't allowed to be set for MaxWidth and MaxHeight. However,
							// WinUI swallows failure HResults specifically when applying style setters.
							// So, Uno catches the exception as well.
							new Setter(FlyoutPresenter.MaxWidthProperty, double.NaN),
							new Setter(FlyoutPresenter.MaxHeightProperty, double.NaN),

							// full stretch
							new Setter(FlyoutPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
							new Setter(FlyoutPresenter.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch),
							new Setter(FlyoutPresenter.VerticalAlignmentProperty, VerticalAlignment.Stretch),
							new Setter(FlyoutPresenter.VerticalContentAlignmentProperty, VerticalAlignment.Stretch),
						}
					},
					Content = new Border
					{
						Background = new SolidColorBrush(Colors.SkyBlue),
						Child = new TextBlock { Text = "Asd" },
					},
				},
			};

			TestServices.WindowHelper.WindowContent = host;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitForLoaded(host);

			try
			{
				host.Flyout.ShowAt(host);

				bool AnyPopupIsOpen() => VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Any();
				await TestServices.WindowHelper.WaitFor(AnyPopupIsOpen, message: "Timeout waiting on flyout to open");

				var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).LastOrDefault();
				var presenter = popup.Child as FlyoutPresenter;
				await TestServices.WindowHelper.WaitForLoaded(presenter);

				var bounds = ApplicationView.GetForCurrentView().VisibleBounds;

				Assert.IsTrue(
					presenter.ActualWidth >= bounds.Width && presenter.ActualHeight >= bounds.Height,
					$"flyout not taking the full size offered: flyout={presenter.ActualWidth}x{presenter.ActualHeight}, VisibleBounds={bounds.Width}x{bounds.Height}");
			}
			finally
			{
				host.Flyout.Hide();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Open_In_GotFocus()
		{
			bool keepOpening = true;
			var flyout = new Flyout
			{
				Content = new Button { Content = "Test" }
			};
			try
			{
				bool closed = false;

				var container = new StackPanel();
				var host = new Button
				{
					Content = "Asd",
					Flyout = flyout
				};
				var unfocusButton = new Button();
				container.Children.Add(unfocusButton);
				container.Children.Add(host);

				TestServices.WindowHelper.WindowContent = container;
				await TestServices.WindowHelper.WaitForLoaded(container);
				await TestServices.WindowHelper.WaitForIdle();
				unfocusButton.Focus(FocusState.Programmatic);
				await TestServices.WindowHelper.WaitForIdle();
				bool gotFocus = false;
				bool wasClosedWhenHostGotFocus = false;
				host.GotFocus += (s, e) =>
				{
					gotFocus = true;
					if (closed)
					{
						wasClosedWhenHostGotFocus = true;
					}
					if (!host.Flyout.IsOpen && keepOpening)
					{
						host.Flyout.ShowAt(host);
					}
				};

				bool opened = false;
				flyout.Opened += (s, e) => opened = true;

				host.Focus(FocusState.Programmatic);

				await TestServices.WindowHelper.WaitFor(() => opened);
				Assert.AreNotEqual(host, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));

				opened = false;
				flyout.Closed += (s, e) => closed = true;

				gotFocus = false;
				flyout.Hide();

				await TestServices.WindowHelper.WaitFor(() => gotFocus);
				await TestServices.WindowHelper.WaitFor(() => closed);
				Assert.IsFalse(wasClosedWhenHostGotFocus);
				Assert.IsFalse(flyout.IsOpen);
			}
			finally
			{
				keepOpening = false;
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
					Assert.AreEqual(targetScreenBounds.Top, contentScreenBounds.Top, delta: 3);
					break;
				case VerticalPosition.Center:
					Assert.AreEqual(targetCenter.Y, contentCenter.Y, delta: 2);
					break;
				case VerticalPosition.BottomFlush:
					Assert.AreEqual(targetScreenBounds.Bottom, contentScreenBounds.Bottom, delta: 3);
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
