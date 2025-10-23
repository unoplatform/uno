using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input.TestPages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;
using SamplesApp.UITests;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input
{
	[TestClass]
	public class Given_FocusManager
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task GotLostFocus()
		{
			using var _ = new AssertionScope();
			var buttons = new Button[4];
			RoutedEventHandler[] onButtonGotFocus = new RoutedEventHandler[4];
			RoutedEventHandler[] onButtonLostFocus = new RoutedEventHandler[4];
			EventHandler<FocusManagerGotFocusEventArgs> onFocusManagerGotFocus = null;
			EventHandler<FocusManagerLostFocusEventArgs> onFocusManagerLostFocus = null;

			try
			{
				var panel = new StackPanel();
				var wasEventRaised = false;

				var receivedGotFocus = new bool[4];
				var receivedLostFocus = new bool[4];

				for (var i = 0; i < 4; i++)
				{
					var button = new Button
					{
						Content = $"Button {i}",
						Name = $"Button{i}"
					};

					panel.Children.Add(button);
					buttons[i] = button;
				}

				TestServices.WindowHelper.WindowContent = panel;

				await TestServices.WindowHelper.WaitForIdle();

				const int tries = 10;
				var initialSuccess = false;
				for (var i = 0; i < tries; i++)
				{
					initialSuccess = buttons[0].Focus(FocusState.Programmatic);
					if (initialSuccess)
					{
						break;
					}
					//await TestServices.WindowHelper.WaitForIdle(); //
					await Task.Delay(50); //
				}

				initialSuccess.Should().BeTrue("initialSuccess");
				AssertHasFocus(buttons[0]);
				await TestServices.WindowHelper.WaitForIdle();

				AssertHasFocus(buttons[0]);

				for (var i = 0; i < 4; i++)
				{
					var inner = i;

					onButtonGotFocus[i] = (o, e) =>
					{
						receivedGotFocus[inner] = true;
						wasEventRaised = true;
						FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot).Should().NotBeNull($"buttons[{i}].GotFocus");
					};

					onButtonLostFocus[i] = (o, e) =>
					{
						receivedLostFocus[inner] = true;
						wasEventRaised = true;
						FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot).Should().NotBeNull($"buttons[{i}].LostFocus");
					};

					buttons[i].GotFocus += onButtonGotFocus[i];

					buttons[i].LostFocus += onButtonLostFocus[i];
				}

				onFocusManagerGotFocus = (o, e) =>
				{
					FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot).Should().NotBeNull($"FocusManager.GotFocus - element");
					wasEventRaised = true;
				};

				onFocusManagerLostFocus = (o, e) =>
				{
					FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot).Should().NotBeNull($"FocusManager.LostFocus - element");
					wasEventRaised = true;
				};

				FocusManager.GotFocus += onFocusManagerGotFocus;

				FocusManager.LostFocus += onFocusManagerLostFocus;

				buttons[1].Focus(FocusState.Programmatic);
				buttons[3].Focus(FocusState.Programmatic);
				buttons[2].Focus(FocusState.Programmatic);

				AssertHasFocus(buttons[2]);
				wasEventRaised.Should().BeFalse("No event raised");
				await TestServices.WindowHelper.WaitForIdle();
				AssertHasFocus(buttons[2]);
				wasEventRaised.Should().BeTrue("event raised");


				receivedGotFocus[0].Should().BeFalse("receivedGotFocus[0]");
				receivedGotFocus[1].Should().BeTrue("receivedGotFocus[1]");
				receivedGotFocus[2].Should().BeTrue("receivedGotFocus[2]");
				receivedGotFocus[3].Should().BeTrue("receivedGotFocus[3]");

				receivedLostFocus[0].Should().BeTrue("receivedLostFocus[0]");
				receivedLostFocus[1].Should().BeTrue("receivedLostFocus[1]");
				receivedLostFocus[2].Should().BeFalse("receivedLostFocus[2]");
				receivedLostFocus[3].Should().BeTrue("receivedLostFocus[3]");
			}
			finally
			{
				for (var i = 0; i < 4; i++)
				{
					buttons[i].GotFocus -= onButtonGotFocus[i];
					buttons[i].LostFocus -= onButtonLostFocus[i];
				}

				FocusManager.GotFocus -= onFocusManagerGotFocus;

				FocusManager.LostFocus -= onFocusManagerLostFocus;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task IsTabStop_False_Check_Inner()
		{
			var outerControl = new ContentControl() { IsTabStop = false };
			var innerControl = new Button { Content = "Inner" };
			var contentRoot = new Border
			{
				Child = new Grid
				{
					Children =
					{
						new TextBlock {Text = "Spinach"},
						innerControl
					}
				}
			};
			outerControl.Content = contentRoot;

			TestServices.WindowHelper.WindowContent = outerControl;
			await TestServices.WindowHelper.WaitForIdle();

			outerControl.Focus(FocusState.Programmatic);
			AssertHasFocus(innerControl);
			Assert.AreEqual(FocusState.Unfocused, outerControl.FocusState);


		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Page_Navigates_Focus_Without_Outer_Wrapper()
		{
			var frame = new Frame();
			TestServices.WindowHelper.WindowContent = frame;
			frame.Navigate(typeof(TwoButtonFirstPage));
			((TwoButtonFirstPage)frame.Content).FocusFirst();

			await TestServices.WindowHelper.WaitForIdle();

			var expectedSequence = new string[]
			{
				null,
				"SecondPageFirstButton"
			};

			Func<Task> navigationAction = async () =>
			{
				frame.Navigate(typeof(TwoButtonSecondPage));

				await WaitForLoadedEvent((TwoButtonSecondPage)frame.Content);
				await TestServices.WindowHelper.WaitForIdle();
			};

			await AssertNavigationFocusSequence(expectedSequence, navigationAction);


		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Page_Navigates_Focus_With_Outer_Before()
		{
			var stackPanel = new StackPanel();
			var frame = new Frame();
			stackPanel.Children.Add(new ToggleButton() { Name = "OuterButton" });
			stackPanel.Children.Add(frame);
			TestServices.WindowHelper.WindowContent = stackPanel;
			frame.Navigate(typeof(TwoButtonFirstPage));
			await WaitForLoadedEvent((TwoButtonFirstPage)frame.Content);
			((TwoButtonFirstPage)frame.Content).FocusFirst();

			await TestServices.WindowHelper.WaitForIdle();

			var expectedSequence = new string[]
			{
				"OuterButton"
			};

			Func<Task> navigationAction = async () =>
			{
				frame.Navigate(typeof(TwoButtonSecondPage));

				await WaitForLoadedEvent((TwoButtonSecondPage)frame.Content);
			};

			await AssertNavigationFocusSequence(expectedSequence, navigationAction);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Page_Navigates_Focus_Outside_Frame()
		{
			var stackPanel = new StackPanel();
			var frame = new Frame();
			var outerButton = new ToggleButton() { Name = "OuterButton" };
			stackPanel.Children.Add(outerButton);
			stackPanel.Children.Add(frame);
			TestServices.WindowHelper.WindowContent = stackPanel;

			await TestServices.WindowHelper.WaitForLoaded(stackPanel);

			frame.Navigate(typeof(TwoButtonFirstPage));
			await WaitForLoadedEvent((TwoButtonFirstPage)frame.Content);
			outerButton.Focus(FocusState.Programmatic);

			await TestServices.WindowHelper.WaitForIdle();

			// Focus should stay on the outer button
			var expectedSequence = new string[]
			{
			};

			Func<Task> navigationAction = async () =>
			{
				frame.Navigate(typeof(TwoButtonSecondPage));

				await WaitForLoadedEvent((TwoButtonSecondPage)frame.Content);
			};

			await AssertNavigationFocusSequence(expectedSequence, navigationAction);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
#if !__SKIA__
		[Ignore("This test is not supported on this platform.")]
#endif
		public async Task When_Tapped_Empty_Space()
		{
			Button button = new Button() { Content = "Button" };
			var border = new Border()
			{
				Padding = new Thickness(100),
				Child = button
			};

			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForLoaded(border);

			button.Focus(FocusState.Programmatic);

			await TestServices.WindowHelper.WaitFor(
				() => object.ReferenceEquals(FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot), button));

			bool losingFocus = false;
			button.LosingFocus += (s, e) => losingFocus = true;

			// Tap outside of the button
			TestServices.InputHelper.Tap(new(10, 10));

			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(losingFocus, "Button should lose focus");

			await TestServices.WindowHelper.WaitFor(
				() => !object.ReferenceEquals(FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot), button));
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Page_Navigates_Focus_With_Outer_After()
		{
			var stackPanel = new StackPanel();
			var frame = new Frame();
			stackPanel.Children.Add(new ToggleButton() { Name = "OuterButton" });
			stackPanel.Children.Add(frame);
			stackPanel.Children.Add(new ToggleButton() { Name = "OuterButtonAfter" });
			TestServices.WindowHelper.WindowContent = stackPanel;
			frame.Navigate(typeof(TwoButtonFirstPage));
			await WaitForLoadedEvent((TwoButtonFirstPage)frame.Content);
			((TwoButtonFirstPage)frame.Content).FocusFirst();

			await TestServices.WindowHelper.WaitForIdle();

			var expectedSequence = new string[]
			{
				"OuterButtonAfter"
			};

			Func<Task> navigationAction = async () =>
			{
				frame.Navigate(typeof(TwoButtonSecondPage));

				await WaitForLoadedEvent((TwoButtonSecondPage)frame.Content);
			};

			await AssertNavigationFocusSequence(expectedSequence, navigationAction);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Page_Navigates_Back_Without_Outer_Wrapper()
		{
			var frame = new Frame();
			TestServices.WindowHelper.WindowContent = frame;
			frame.Navigate(typeof(TwoButtonFirstPage));
			frame.Navigate(typeof(TwoButtonSecondPage));
			await WaitForLoadedEvent((TwoButtonSecondPage)frame.Content);
			((TwoButtonSecondPage)frame.Content).FocusFirst();

			await TestServices.WindowHelper.WaitForIdle();

			var expectedSequence = new string[]
			{
				null,
				"FirstPageFirstButton"
			};

			Func<Task> navigationAction = async () =>
			{
				frame.GoBack();

				await WaitForLoadedEvent((TwoButtonFirstPage)frame.Content);
				await TestServices.WindowHelper.WaitForIdle();
			};

			await AssertNavigationFocusSequence(expectedSequence, navigationAction);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		[Ignore("This test is flaky on CI, see #9080")]
		public async Task When_Page_Navigates_Back_With_Outer_Before()
		{
			var stackPanel = new StackPanel();
			var frame = new Frame();
			stackPanel.Children.Add(new ToggleButton() { Name = "OuterButton" });
			stackPanel.Children.Add(frame);
			TestServices.WindowHelper.WindowContent = stackPanel;
			frame.Navigate(typeof(TwoButtonFirstPage));
			frame.Navigate(typeof(TwoButtonSecondPage));
			((TwoButtonSecondPage)frame.Content).FocusFirst();

			await TestServices.WindowHelper.WaitForIdle();

			var expectedSequence = new string[]
			{
				"OuterButton"
			};

			Func<Task> navigationAction = async () =>
			{
				frame.GoBack();
				await WaitForLoadedEvent((TwoButtonFirstPage)frame.Content);

				await TestServices.WindowHelper.WaitForIdle();
			};

			await AssertNavigationFocusSequence(expectedSequence, navigationAction);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Page_Navigates_Back_With_Outer_After()
		{
			var stackPanel = new StackPanel();
			var frame = new Frame();
			stackPanel.Children.Add(new ToggleButton() { Name = "OuterButton" });
			stackPanel.Children.Add(frame);
			stackPanel.Children.Add(new ToggleButton() { Name = "OuterButtonAfter" });
			TestServices.WindowHelper.WindowContent = stackPanel;
			frame.Navigate(typeof(TwoButtonFirstPage));
			frame.Navigate(typeof(TwoButtonSecondPage));
			((TwoButtonSecondPage)frame.Content).FocusFirst();

			await TestServices.WindowHelper.WaitForIdle();

			var expectedSequence = new string[]
			{
				"OuterButtonAfter"
			};

			Func<Task> navigationAction = async () =>
			{
				frame.GoBack();
				await WaitForLoadedEvent((TwoButtonFirstPage)frame.Content);

				await TestServices.WindowHelper.WaitForIdle();
			};

			await AssertNavigationFocusSequence(expectedSequence, navigationAction);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Page_Navigates_From_Page_ListViewItem()
		{
			var stackPanel = new StackPanel();
			var frame = new Frame();
			stackPanel.Children.Add(frame);
			stackPanel.Children.Add(new ToggleButton() { Name = "OuterButtonAfter" });
			TestServices.WindowHelper.WindowContent = stackPanel;
			frame.Navigate(typeof(ListViewItemPage));
			var sourcePage = (ListViewItemPage)frame.Content;
			await WaitForLoadedEvent(sourcePage);

			await TestServices.WindowHelper.WaitFor(() => sourcePage.List.ContainerFromIndex(0) is not null);
			var container = sourcePage.List.ContainerFromIndex(0) as ListViewItem;
			container.Focus(FocusState.Programmatic);

			await TestServices.WindowHelper.WaitForIdle();

			var expectedSequence = new string[]
			{
				"OuterButtonAfter"
			};

			Func<Task> navigationAction = async () =>
			{
				frame.Navigate(typeof(TwoButtonSecondPage));
				await WaitForLoadedEvent((TwoButtonSecondPage)frame.Content);

				await TestServices.WindowHelper.WaitForIdle();
			};

			await AssertNavigationFocusSequence(expectedSequence, navigationAction);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_ScrollViewer_TryMoveFocus()
		{
			var textBox1 = new TextBox();
			var textBox2 = new TextBox();
			var stackPanel = new StackPanel()
			{
				Children = {
					textBox1,
					textBox2
				}
			};
			var button = new Button();
			var scrollViewer = new ScrollViewer();
			scrollViewer.Content = stackPanel;
			var grid = new Grid()
			{
				Children =
				{
					scrollViewer,
					button
				}
			};

			TestServices.WindowHelper.WindowContent = grid;

			await TestServices.WindowHelper.WaitForIdle();

			textBox1.Focus(FocusState.Programmatic);

			var moved = FocusManager.TryMoveFocus(
				FocusNavigationDirection.Next,
				new FindNextElementOptions() { SearchRoot = TestServices.WindowHelper.XamlRoot.Content });
			var focused = FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot);

			Assert.IsTrue(moved);
			Assert.AreEqual(textBox2, focused);

			moved = FocusManager.TryMoveFocus(
				FocusNavigationDirection.Next,
				new FindNextElementOptions() { SearchRoot = TestServices.WindowHelper.XamlRoot.Content });
			focused = FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot);

			Assert.IsTrue(moved);
			Assert.AreEqual(button, focused);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("https://github.com/unoplatform/uno/issues/15457")]
#endif
		public async Task When_FocusChanged_PreventScroll()
		{
			var ts1 = new ToggleSwitch();
			var ts2 = new ToggleSwitch();
			var SUT = new ScrollViewer
			{
				Content = new StackPanel
				{
					Spacing = 1200,
					Children =
					{
						ts1, ts2
					}
				}
			};

			await UITestHelper.Load(SUT);

			Assert.AreEqual(0, SUT.VerticalOffset);

			ts2.Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();
#if __WASM__ // wasm needs an additional delay for some reason, probably because of smooth scrolling?
			await Task.Delay(2000);
#endif

			Assert.AreEqual(0, SUT.VerticalOffset);
			SUT.ScrollToVerticalOffset(99999);

			ts1.Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();
#if __WASM__ // wasm needs an additional delay for some reason, probably because of smooth scrolling?
			await Task.Delay(2000);
#endif

			Assert.AreEqual(SUT.ScrollableHeight, SUT.VerticalOffset);
		}


		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/uno-private/issues/868")]
		public async Task When_Focus_TextBox_Inside_NavigationView()
		{
			var SUT = new NavigationViewPage();
			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			SUT.NavigationViewControl.IsPaneOpen = true;
			await TestServices.WindowHelper.WaitForIdle();

			var suggestBox = SUT.SearchBox;

			var waitForFocusToComplete = Task.Delay(5000);

			var focusSearchBox = TestServices.RunOnUIThread(() =>
			{
				// We need to validate that we are returning from this operation.
				suggestBox.Focus(FocusState.Programmatic);
			});

			var completedTask = await Task.WhenAny(focusSearchBox, waitForFocusToComplete);
			if (completedTask == waitForFocusToComplete)
			{
				Assert.Fail("AutoSuggestBox did not get the focus");
			}

			suggestBox.Text = "Test";

			SUT.NavigationViewControl.IsPaneOpen = false;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.NavigationViewControl.IsPaneOpen);
		}


		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Focus_Same_Input_Multiple_Times()
		{
			const int waitAfterFocus = 600;
			var SUT = new NavigationViewPage();
			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			var suggestBox = SUT.SearchBox;
			var rootGrid = SUT.RootGrid;

			for (var x = 0; x < 6; x++)
			{
				SUT.NavigationViewControl.IsPaneOpen = true;
				await TestServices.WindowHelper.WaitForIdle();

				suggestBox.Focus(FocusState.Pointer);
				await Task.Delay(waitAfterFocus);
				await TestServices.WindowHelper.WaitForIdle();

				rootGrid.Focus(FocusState.Pointer);
				await Task.Delay(waitAfterFocus);
				await TestServices.WindowHelper.WaitForIdle();
			}

			// No need to assert. Test will crash when the focus logic is broken.
		}

		private async Task WaitForLoadedEvent(FocusNavigationPage page)
		{
			await TestServices.WindowHelper.WaitFor(() => page.LoadedEventFinished);
		}

		private async Task AssertNavigationFocusSequence(string[] expectedSequence, Func<Task> navigationSequence)
		{
			var actualSequence = new List<string>();
			void FocusManager_GettingFocus(object sender, GettingFocusEventArgs e)
			{
				if (e.NewFocusedElement is null)
				{
					actualSequence.Add(null);
				}
				else if (
					e.NewFocusedElement is FrameworkElement fw &&
					!string.IsNullOrEmpty(fw.Name))
				{
					actualSequence.Add(fw.Name);
				}
				else
				{
					actualSequence.Add($"[{e.NewFocusedElement.GetType().Name}]");
				}
			}
			try
			{
				FocusManager.GettingFocus += FocusManager_GettingFocus;

				await navigationSequence();

				CollectionAssert.AreEqual(expectedSequence, actualSequence);

			}
			finally
			{
				FocusManager.GettingFocus -= FocusManager_GettingFocus;
			}
		}

		private void AssertHasFocus(Control control)
		{
			control.Should().NotBeNull("control");
			var focused = FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot);
			focused.Should().NotBeNull("focused element");
			focused.Should().BeSameAs(control, "must be same element");
			control.FocusState.Should().NotBe(FocusState.Unfocused);
		}
	}
}
