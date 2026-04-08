using System;
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
using Combinatorial.MSTest;
using Uno.UI.Toolkit.DevTools.Input;


#if HAS_UNO_WINUI || WINAPPSDK || WINUI
using Colors = Microsoft.UI.Colors;
#else
using Colors = Windows.UI.Colors;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Button
	{
		[TestMethod]
		[CombinatorialData]
		public async Task When_NavigationViewButtonStyles(bool useFluent)
		{
			using var _ = useFluent ? null : StyleHelper.UseUwpStyles();

			var normalBtn = (Button)XamlReader.Load("""
				<Button xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Style="{StaticResource NavigationBackButtonNormalStyle}" />
				""");
			var normalBtnRect = await UITestHelper.Load(normalBtn);

			var smallBtn = (Button)XamlReader.Load("""
				<Button xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Style="{StaticResource NavigationBackButtonSmallStyle}" />
				""");
			var smallBtnRect = await UITestHelper.Load(smallBtn);

			Assert.AreEqual(new Size(40, 40), new Size(normalBtnRect.Width, normalBtnRect.Height));
			Assert.AreEqual(new Size(32, 32), new Size(smallBtnRect.Width, smallBtnRect.Height));
			Assert.AreEqual(0, normalBtnRect.Left - smallBtnRect.Left);
			Assert.AreEqual(8, normalBtnRect.Right - smallBtnRect.Right);
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

		[TestMethod]
		[DataRow(typeof(Button))]
		[DataRow(typeof(ToggleButton))]
		[DataRow(typeof(RepeatButton))]
#if !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_BorderThickness_Zero(Type type)
		{
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

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/7888")]
#if !__SKIA__
		[Ignore("Requires InputInjector which is only available on Skia")]
#endif
		public async Task When_ClickMode_Press_Then_ClickFiredOnPointerPressed()
		{
			// Issue #7888: Button events are improperly raised for ClickMode.Press.
			// In WinUI, ClickMode.Press should fire Click on PointerPressed (not PointerReleased).

			var button = new Button
			{
				Content = "Press Me",
				ClickMode = ClickMode.Press,
				Width = 200,
				Height = 50
			};

			var clickCount = 0;
			var clickFiredBeforeRelease = false;
			var pointerReleasedCount = 0;

			button.Click += (s, e) =>
			{
				clickCount++;
				if (pointerReleasedCount == 0)
				{
					clickFiredBeforeRelease = true;
				}
			};

			button.PointerReleased += (s, e) =>
			{
				pointerReleasedCount++;
			};

			WindowHelper.WindowContent = button;
			await WindowHelper.WaitForLoaded(button);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = button.GetAbsoluteBoundsRect();
			var center = new Point(bounds.GetMidX(), bounds.GetMidY());

			// Move to center of button, then press
			mouse.MoveTo(center);
			await WindowHelper.WaitForIdle();

			mouse.Press();
			await WindowHelper.WaitForIdle();

			// For ClickMode.Press, Click should have fired on press (before release)
			Assert.AreEqual(1, clickCount, "Click should fire once on PointerPressed for ClickMode.Press");
			Assert.IsTrue(clickFiredBeforeRelease,
				"Click should fire BEFORE PointerReleased for ClickMode.Press");

			mouse.Release();
			await WindowHelper.WaitForIdle();

			// Click should NOT fire again on release
			Assert.AreEqual(1, clickCount,
				"Click should not fire again on PointerReleased for ClickMode.Press");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/7888")]
#if !__SKIA__
		[Ignore("Requires InputInjector which is only available on Skia")]
#endif
		public async Task When_ClickMode_Hover_Then_ClickFiredOnPointerEnter()
		{
			// Issue #7888: Button events for ClickMode.Hover.
			// In WinUI, ClickMode.Hover should fire Click when pointer enters the button.

			var button = new Button
			{
				Content = "Hover Me",
				ClickMode = ClickMode.Hover,
				Width = 200,
				Height = 50
			};

			var clickCount = 0;
			button.Click += (s, e) => clickCount++;

			var container = new StackPanel { Spacing = 50 };
			container.Children.Add(new Border { Width = 200, Height = 50 }); // Spacer to move away from
			container.Children.Add(button);

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(container);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
			using var mouse = injector.GetMouse();

			// Start from the spacer (outside the button)
			var spacerBounds = ((FrameworkElement)container.Children[0]).GetAbsoluteBoundsRect();
			mouse.MoveTo(new Point(spacerBounds.GetMidX(), spacerBounds.GetMidY()));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, clickCount, "Click should not have fired yet");

			// Move into the button
			var buttonBounds = button.GetAbsoluteBoundsRect();
			mouse.MoveTo(new Point(buttonBounds.GetMidX(), buttonBounds.GetMidY()));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, clickCount,
				"Click should fire when pointer enters button for ClickMode.Hover");
		}
	}
}
