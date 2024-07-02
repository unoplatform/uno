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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Button
	{
		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
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
			Assert.AreEqual(cc.IsEnabled, false);
			Assert.AreEqual(SUT.IsEnabled, false);

			// Once the outer control is enabled, the button can control its own IsEnabled
			cc.IsEnabled = true;
			Assert.AreEqual(cc.IsEnabled, true);
			Assert.AreEqual(SUT.IsEnabled, true);

			// We test once again to test behaviour when the value is set while running
			// instead of during initialization
			cc.IsEnabled = false;
			Assert.AreEqual(cc.IsEnabled, false);
			Assert.AreEqual(SUT.IsEnabled, false);

			// Now let's make sure Button.IsEnabled doesn't
			// affect the outer ContentControl.IsEnabled

			// cc.IsEnbaled is false
			SUT.IsEnabled = true;
			Assert.AreEqual(cc.IsEnabled, false);
			Assert.AreEqual(SUT.IsEnabled, false);

			SUT.IsEnabled = false;
			Assert.AreEqual(cc.IsEnabled, false);
			Assert.AreEqual(SUT.IsEnabled, false);

			cc.IsEnabled = true;
			Assert.AreEqual(cc.IsEnabled, true);
			Assert.AreEqual(SUT.IsEnabled, false);

			SUT.IsEnabled = true;
			Assert.AreEqual(cc.IsEnabled, true);
			Assert.AreEqual(SUT.IsEnabled, true);

			SUT.IsEnabled = false;
			Assert.AreEqual(cc.IsEnabled, true);
			Assert.AreEqual(SUT.IsEnabled, false);
		}

		[TestMethod]
		public async Task When_Command_Executing_IsEnabled()
		{
			var command = new IsExecutingCommand(true);
			await RunIsExecutingCommandCommon(command);
		}

		[TestMethod]
		public async Task When_Command_Executing_With_Delay_IsEnabled()
		{
			var command = new IsExecutingCommand(false);
			await RunIsExecutingCommandCommon(command);
		}

		[TestMethod]
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
		[Ignore("InputInjector is only supported on skia")]
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
				var secondPress = Finger.GetRelease(SUT.GetAbsoluteBounds().GetCenter());
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
		[Ignore("InputInjector is only supported on skia")]
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

#if HAS_UNO && !__MACOS__
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
	}
}
