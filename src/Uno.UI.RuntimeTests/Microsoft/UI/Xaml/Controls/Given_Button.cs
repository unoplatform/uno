using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Input.Preview.Injection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using Windows.Foundation;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Controls.Primitives;
using Color = Windows.UI.Color;
using Microsoft.UI.Xaml.Data;
using Uno.UI.DevTools.Input;


using Colors = Microsoft.UI.Colors;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Button
	{
		[TestMethod]
		// Excluded on native WinUI: Uno mirrors the MUX controls/dev resources (bac7a9c33),
		// which size the back button 40x36 with a margin-only Small style. Shipped WinUI overrides
		// those in dxaml generic.xaml with Normal 40x40 / Small 32x32, and that is what native WinUI
		// renders in every release (verified identical at 1.6.9, 1.7.3 and 1.8.2), so no WindowsAppSDK
		// bump makes this pass at 40x36. Re-enable if WinUI promotes the controls/dev sizes to generic.xaml.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_NavigationViewButtonStyles()
		{
			var normalBtn = (Button)XamlReader.Load("""
				<Button xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Style="{StaticResource NavigationBackButtonNormalStyle}" />
				""");
			var normalBtnRect = await UITestHelper.Load(normalBtn);

			var smallBtn = (Button)XamlReader.Load("""
				<Button xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Style="{StaticResource NavigationBackButtonSmallStyle}" />
				""");
			var smallBtnRect = await UITestHelper.Load(smallBtn);

			// Aligned with WinUI bac7a9c33: both styles share NavigationBackButtonWidth/Height (40x36);
			// the Small style only differs from the Normal style by trimming the right margin.
			// A small tolerance accounts for sub-pixel rounding in the transformed bounds.
			Assert.AreEqual(40, normalBtnRect.Width, 0.5);
			Assert.AreEqual(36, normalBtnRect.Height, 0.5);
			Assert.AreEqual(40, smallBtnRect.Width, 0.5);
			Assert.AreEqual(36, smallBtnRect.Height, 0.5);
			Assert.AreEqual(new Thickness(4, 2, 4, 2), normalBtn.Margin);
			Assert.AreEqual(new Thickness(4, 2, 0, 2), smallBtn.Margin);
		}

		[TestMethod]
		public async Task When_Enabled_Inside_Disabled_Control()
		{
			var SUT = new Button() { IsEnabled = true };

			var cc = new ContentControl()
			{
				IsEnabled = false,
				Content = SUT
			};

			await UITestHelper.Load(cc);

			// The button should be disabled because the outer control is disabled
			Assert.IsFalse(cc.IsEnabled);
			Assert.IsFalse(SUT.IsEnabled);

			// Once the outer control is enabled, the button can control its own IsEnabled
			cc.IsEnabled = true;
			Assert.IsTrue(cc.IsEnabled);
			Assert.IsTrue(SUT.IsEnabled);

			// We test once again to test behaviour when the value is set while running
			// instead of during initialization
			cc.IsEnabled = false;
			Assert.IsFalse(cc.IsEnabled);
			Assert.IsFalse(SUT.IsEnabled);

			// Now let's make sure Button.IsEnabled doesn't
			// affect the outer ContentControl.IsEnabled

			// cc.IsEnbaled is false
			SUT.IsEnabled = true;
			Assert.IsFalse(cc.IsEnabled);
			Assert.IsFalse(SUT.IsEnabled);

			SUT.IsEnabled = false;
			Assert.IsFalse(cc.IsEnabled);
			Assert.IsFalse(SUT.IsEnabled);

			cc.IsEnabled = true;
			Assert.IsTrue(cc.IsEnabled);
			Assert.IsFalse(SUT.IsEnabled);

			SUT.IsEnabled = true;
			Assert.IsTrue(cc.IsEnabled);
			Assert.IsTrue(SUT.IsEnabled);

			SUT.IsEnabled = false;
			Assert.IsTrue(cc.IsEnabled);
			Assert.IsFalse(SUT.IsEnabled);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Command_Executing_IsEnabled()
		{
			var command = new IsExecutingCommand(true);
			await RunIsExecutingCommandCommon(command);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Command_Executing_With_Delay_IsEnabled()
		{
			var command = new IsExecutingCommand(false);
			await RunIsExecutingCommandCommon(command);
		}

		private const DynamicallyAccessedMemberTypes ActivatorRequirements = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

		[TestMethod]
		[DataRow(typeof(Button))]
		[DataRow(typeof(ToggleButton))]
		[DataRow(typeof(RepeatButton))]
		public async Task When_BorderThickness_Zero([DynamicallyAccessedMembers(ActivatorRequirements)] Type type)
		{
			// Keep PreserveMetadata() calls in sync with the types in [DataRow] above.
			PreserveMetadata(typeof(Button));
			PreserveMetadata(typeof(ToggleButton));
			PreserveMetadata(typeof(RepeatButton));

			var grid = new Grid
			{
				Width = 120,
				Height = 120,
				Background = new SolidColorBrush(Colors.Yellow)
			};

			var button = (ButtonBase)Activator.CreateInstance(type);

			button.Content = "";
			button.Background = new SolidColorBrush(Colors.Transparent);
			button.BorderThickness = new Thickness(0);
			button.Width = 100;
			button.Height = 100;

			grid.Children.Add(button);

			await UITestHelper.Load(grid);

			var borderThicknessZero = await UITestHelper.ScreenShot(grid);

			button.Visibility = Visibility.Collapsed;

			var opacityZero = await UITestHelper.ScreenShot(grid);

			await ImageAssert.AreEqualAsync(opacityZero, borderThicknessZero);

			static void PreserveMetadata([DynamicallyAccessedMembers(ActivatorRequirements)] Type type)
			{
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Command_Never_Stops()
		{
			var command = new NeverEndingCommand();

			var (firstButton, secondButton) = await CreateCommandFocusWindowContentAsync(command);

			firstButton.Focus(FocusState.Programmatic);

			await TestServices.WindowHelper.WaitForIdle();

			command.Execute(null);

			// Focus should stay on the button
			Assert.AreNotEqual(FocusState.Unfocused, firstButton.FocusState);

			secondButton.Focus(FocusState.Programmatic);

			// The button cannot be refocused
			Assert.IsFalse(firstButton.Focus(FocusState.Programmatic));
		}
#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_DoubleTap_Timing()
		{
			// This is actually a test for GestureRecognizer and pointer gesture events

			var SUT = new Button();

			var doubleTaps = 0;
			SUT.DoubleTapped += (_, _) => doubleTaps++;

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger(id: 42);

			await Press(0);
			await Release(1);
			await Press(1);
			await Release(1);

			Assert.AreEqual(1, doubleTaps);

			await Press(10000);
			await Release(1);
			await Press(200);
			await Release(1);

			Assert.AreEqual(2, doubleTaps);

			await Press(10000);
			await Release(1);
			await Press(400);
			await Release(1);

			Assert.AreEqual(3, doubleTaps);

			await Press(10000);
			await Release(1);
			await Press(501);
			await Release(1);

			Assert.AreEqual(3, doubleTaps);

			async Task Press(uint i)
			{
				var secondPress = Finger.GetPress(42, SUT.GetAbsoluteBounds().GetCenter());
				var pointerInfo = secondPress.PointerInfo;
				pointerInfo.TimeOffsetInMilliseconds = i;
				secondPress.PointerInfo = pointerInfo;
				injector.InjectTouchInput(new[]
				{
					secondPress
				});
				await WindowHelper.WaitForIdle();

			}
			async Task Release(uint i)
			{
				var secondPress = Finger.GetRelease(42, SUT.GetAbsoluteBounds().GetCenter());
				var pointerInfo = secondPress.PointerInfo;
				pointerInfo.TimeOffsetInMilliseconds = i;
				secondPress.PointerInfo = pointerInfo;
				injector.InjectTouchInput(new[]
				{
					secondPress
				});
				await WindowHelper.WaitForIdle();
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Tapped_PointerPressed_Is_Not_Raised()
		{
			var SUT = new Button()
			{
				Content = "text"
			};

			bool pressedInvoked = false;
			SUT.PointerPressed += (_, _) => pressedInvoked = true;

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Press(SUT.GetAbsoluteBounds().GetCenter());
			finger.Release();
			finger.Press(SUT.GetAbsoluteBounds().GetCenter());
			finger.Release();

			Assert.IsFalse(pressedInvoked);
		}
#endif

#if HAS_UNO
		[TestMethod]
		public async Task When_Button_Flyout_TemplateBinding()
		{
			try
			{
				var SUT = new Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ButtonControls.Button_Flyout_TemplateBinding();

				WindowHelper.WindowContent = SUT;

				await WindowHelper.WaitForIdle();

				var innerButton = SUT.FindName("innerButton") as Button;
				Assert.IsNotNull(innerButton);
				innerButton.RaiseClick();

				await TestServices.WindowHelper.WaitForIdle();

				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);

				var innerFlyoutItem = popups.Select(p
					=> ((p.Child as MenuFlyoutPresenter)?.TemplatedRoot as FrameworkElement)?.FindName("innerFlyoutItem") as MenuFlyoutItem).Trim().FirstOrDefault();

				Assert.IsNotNull(innerFlyoutItem);

				Assert.AreEqual("42", innerFlyoutItem.Text);
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
			}
		}
#endif

		[TestMethod]
		public async Task When_Command_CanExecute_Throws()
		{
			// Here we are testing against a bug where
			// a data-bound ICommand that throws in its CanExecute
			// can cause the binding to reset to its FallbackValue.
			var vm = new
			{
				UnstableCommand = new DelegateCommand(x => throw new Exception("fail...")),
			};

			var sut = new Button();
			sut.SetBinding(Button.CommandProperty, new Binding { Path = new(nameof(vm.UnstableCommand)), FallbackValue = new NoopCommand() });
			sut.DataContext = vm;

			await UITestHelper.Load(sut, x => x.IsLoaded);

			Assert.AreEqual(vm.UnstableCommand, sut.Command, "Binding did not set the proper value.");
		}

		private async Task RunIsExecutingCommandCommon(IsExecutingCommand command)
		{
			void FocusManager_LosingFocus(object sender, LosingFocusEventArgs e)
			{
				Assert.Fail("The button should not lose focus when its command is executing");
			}

			try
			{
				var (firstButton, secondButton) = await CreateCommandFocusWindowContentAsync(command);

				firstButton.Focus(FocusState.Programmatic);

				await TestServices.WindowHelper.WaitForIdle();

				FocusManager.LosingFocus += FocusManager_LosingFocus;

				// Execute command to simulate work
				await command.SimulateExecutionAsync();

				// Focus should stay on the button
				Assert.AreNotEqual(FocusState.Unfocused, firstButton.FocusState);
			}
			finally
			{
				FocusManager.LosingFocus -= FocusManager_LosingFocus;
			}
		}

		private async Task<(Button firstButton, Button secondButton)> CreateCommandFocusWindowContentAsync(ICommand firstButtonCommand)
		{
			var firstButton = new Button()
			{
				Content = "Test button",
				Command = firstButtonCommand
			};
			var secondButton = new Button() { Content = "Do not focus me!" };

			var stackPanel = new StackPanel()
			{
				Children =
				{
					firstButton,
					secondButton,
				}
			};

			TestServices.WindowHelper.WindowContent = stackPanel;

			await TestServices.WindowHelper.WaitForIdle();

			return (firstButton, secondButton);
		}

		public class IsExecutingCommand : ICommand
		{
			private readonly bool _synchronousCompletion;
			private bool IsExecuting;

			public IsExecutingCommand(bool synchronousCompletion)
			{
				_synchronousCompletion = synchronousCompletion;
			}

			public event EventHandler CanExecuteChanged;

			public bool CanExecute(object parameter) => !IsExecuting;

			public void Execute(object parameter)
			{
				// Intentionally blank, we care only about CanExecute behavior
			}

			public async Task SimulateExecutionAsync()
			{
				IsExecuting = true;
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
				if (!_synchronousCompletion)
				{
					await Task.Delay(100);
				}
				IsExecuting = false;
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public class NeverEndingCommand : ICommand
		{
			private bool _wasStarted;

			public NeverEndingCommand()
			{
			}

			public event EventHandler CanExecuteChanged;

			public bool CanExecute(object parameter) => !_wasStarted;

			public void Execute(object parameter)
			{
				_wasStarted = true;
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public class DelegateCommand : ICommand
		{
			private readonly Func<object, bool> canExecuteImpl;
			private readonly Action<object> executeImpl;

			public event EventHandler CanExecuteChanged;

			public DelegateCommand(Func<object, bool> canExecute = null, Action<object> execute = null)
			{
				this.canExecuteImpl = canExecute;
				this.executeImpl = execute;
			}

			public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, default);

			public bool CanExecute(object parameter) => canExecuteImpl?.Invoke(parameter) ?? true;
			public void Execute(object parameter) => executeImpl?.Invoke(parameter);
		}

		public class NoopCommand() : DelegateCommand(null, null) { }

		[TestMethod]
		public async Task When_AccentButtonStyle_Has_Same_Height_As_Default()
		{
			var defaultButton = new Button { Content = "Default" };
			var accentButton = (Button)XamlReader.Load("""
				<Button xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						Content="Accent"
						Style="{StaticResource AccentButtonStyle}" />
				""");

			var panel = new StackPanel
			{
				Children =
				{
					defaultButton,
					accentButton,
				}
			};

			await UITestHelper.Load(panel);

			Assert.AreEqual(defaultButton.Padding, accentButton.Padding, "Padding should match between default and AccentButtonStyle buttons");
			Assert.AreEqual(defaultButton.ActualHeight, accentButton.ActualHeight, "ActualHeight should match between default and AccentButtonStyle buttons");
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/12028")]
		public async Task When_Button_Does_Not_Stay_Pressed_During_Async_Click_Handler()
		{
			// A Button should return to Normal/PointerOver visual state as soon as the
			// pointer is released, even when the Click handler starts a long-running async operation.
			// If the button stays in "Pressed" state during the async operation, that is a visual state bug.

			var asyncTaskCompleted = false;
			var asyncTaskStarted = new TaskCompletionSource<bool>();
			// Keeps the Click handler in flight until the test has asserted the visual state,
			// so the assertion can't race the handler completing (no wall-clock delay to tune).
			var asyncHandlerGate = new TaskCompletionSource<bool>();

			var button = new Button { Content = "Async Button", Width = 200, Height = 50 };
			button.Click += async (_, _) =>
			{
				asyncTaskStarted.TrySetResult(true);
				await asyncHandlerGate.Task;
				asyncTaskCompleted = true;
			};

			var buttonRect = await UITestHelper.Load(button);
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
			using var mouse = injector.GetMouse();

			var center = new Point(buttonRect.X + buttonRect.Width / 2, buttonRect.Y + buttonRect.Height / 2);

			await WindowHelper.WaitFor(() => VisualTreeHelper.GetChildrenCount(button) > 0, timeoutMS: 2000);
			var templateRoot = VisualTreeHelper.GetChildrenCount(button) > 0
				? VisualTreeHelper.GetChild(button, 0) as FrameworkElement
				: null;
			var commonStatesGroup = templateRoot is null
				? null
				: VisualStateManager.GetVisualStateGroups(templateRoot)?.FirstOrDefault(g => g.Name == "CommonStates");
			Assert.IsNotNull(commonStatesGroup, "CommonStates group should exist in button template");

			// Move over button (sets PointerOver state)
			mouse.MoveTo(center);
			await WindowHelper.WaitForIdle();

			try
			{
				mouse.Press();
				await WindowHelper.WaitForIdle();

				// Sanity check: the visual-state logic actually engaged before we test that it disengages.
				Assert.AreEqual("Pressed", commonStatesGroup.CurrentState?.Name, "Button should be in Pressed state while the pointer is held down");

				// Release — Click fires, async handler starts and blocks on the gate.
				mouse.Release();
				await asyncTaskStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(asyncTaskCompleted, "Async handler should still be running at this point");

				var currentState = commonStatesGroup.CurrentState?.Name;
				Assert.AreNotEqual(
					"Pressed",
					currentState,
					$"Button must not remain in Pressed state after pointer release (async task still running). Current state: '{currentState}'");
			}
			finally
			{
				// Always release the gate so a failed assertion can't leave the handler awaiting forever.
				asyncHandlerGate.TrySetResult(true);
			}

			await WindowHelper.WaitFor(() => asyncTaskCompleted, timeoutMS: 2000);
		}

#if HAS_UNO
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22229")]
		public async Task When_ContextFlyout_Touch_LongPress_Opens_Flyout_Without_Click()
		{
			var flyout = new MenuFlyout();
			flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
			var flyoutOpened = 0;
			flyout.Opened += (_, _) => flyoutOpened++;

			var clicks = 0;
			var holding = 0;
			var button = new Button { Content = "Long press me", Width = 200, Height = 50, ContextFlyout = flyout };
			button.Click += (_, _) => clicks++;
			button.Holding += (_, _) => holding++;

			var buttonRect = await UITestHelper.Load(button);
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				finger.Press(buttonRect.GetCenter());
				await WindowHelper.WaitFor(() => flyoutOpened == 1, timeoutMS: 5000, message: "The context flyout should open from the hold, before the release");

				Assert.AreEqual(1, holding, "The hold should be recognized before the release");

				finger.Release();
				await WindowHelper.WaitForIdle();
				await Task.Delay(150);

				Assert.AreEqual(1, flyoutOpened);
				Assert.AreEqual(0, clicks, "Releasing a hold that opened the context flyout must not click the button");
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
			}
		}

		// On a draggable/pannable element the context menu of a touch hold is delayed (ContextMenuProcessor, 500 ms after
		// the Holding gesture) to leave room for a drag/pan. Mirrors WinUI PointerInputProcessor: the pointer up stops that
		// delayed menu, so the release is a plain click and the menu must not pop up afterwards.
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22229")]
		public async Task When_ContextFlyout_In_ScrollViewer_Touch_Released_Before_Delayed_Flyout()
		{
			var flyout = new MenuFlyout();
			flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
			var flyoutOpened = 0;
			flyout.Opened += (_, _) => flyoutOpened++;

			var clicks = 0;
			var holding = 0;
			var button = new Button { Content = "Long press me", Width = 200, Height = 50, ContextFlyout = flyout };
			button.Click += (_, _) => clicks++;
			button.Holding += (_, _) => holding++;

			var scrollViewer = new ScrollViewer { Content = button, Width = 300, Height = 200 };
			await UITestHelper.Load(scrollViewer);
			var buttonRect = button.GetAbsoluteBounds();
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				finger.Press(buttonRect.GetCenter());
				// Release once the hold is recognized, within the 500 ms the menu of a hold on a pannable element is delayed by.
				await WindowHelper.WaitFor(() => holding == 1, timeoutMS: 5000, message: "The hold should be recognized before the release");

				Assert.AreEqual(0, flyoutOpened, "The context flyout of a hold on a pannable element is delayed");

				finger.Release();
				await WindowHelper.WaitForIdle();
				// Longer than the remaining delay: a menu still pending would open here.
				await Task.Delay(800);

				Assert.AreEqual(0, flyoutOpened, "The context flyout must not open once the pointer has been released");
				Assert.AreEqual(1, clicks, "A hold released before the delayed context flyout is a click (no flyout opened)");
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
			}
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22229")]
		public async Task When_ContextFlyout_In_ScrollViewer_Touch_Held_Until_Delayed_Flyout_Opens()
		{
			var flyout = new MenuFlyout();
			flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
			var flyoutOpened = 0;
			flyout.Opened += (_, _) => flyoutOpened++;

			var clicks = 0;
			var button = new Button { Content = "Long press me", Width = 200, Height = 50, ContextFlyout = flyout };
			button.Click += (_, _) => clicks++;

			var scrollViewer = new ScrollViewer { Content = button, Width = 300, Height = 200 };
			await UITestHelper.Load(scrollViewer);
			var buttonRect = button.GetAbsoluteBounds();
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
			using var finger = injector.GetFinger();

			try
			{
				finger.Press(buttonRect.GetCenter());
				await WindowHelper.WaitFor(() => flyoutOpened == 1, timeoutMS: 5000, message: "The delayed context flyout should open while still holding");

				finger.Release();
				await WindowHelper.WaitForIdle();
				await Task.Delay(150);

				Assert.AreEqual(1, flyoutOpened);
				Assert.AreEqual(0, clicks, "Releasing a hold that opened the context flyout must not click the button");
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
			}
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22229")]
		public async Task When_ContextFlyout_RightClick_Opens_Flyout_Without_Click()
		{
			var flyout = new MenuFlyout();
			flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
			var flyoutOpened = 0;
			flyout.Opened += (_, _) => flyoutOpened++;

			var clicks = 0;
			var button = new Button { Content = "Right click me", Width = 200, Height = 50, ContextFlyout = flyout };
			button.Click += (_, _) => clicks++;

			var buttonRect = await UITestHelper.Load(button);
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
			using var mouse = injector.GetMouse();

			try
			{
				mouse.PressRight(buttonRect.GetCenter());
				mouse.ReleaseRight();
				await WindowHelper.WaitForIdle();
				await Task.Delay(150);

				Assert.AreEqual(1, flyoutOpened, "The context flyout should open from the right click");
				Assert.AreEqual(0, clicks, "A right click that opened the context flyout must not click the button");
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
			}
		}
#endif
	}
}
