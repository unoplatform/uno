using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Button
	{
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

				var popups = VisualTreeHelper.GetOpenPopups(Microsoft.UI.Xaml.Window.Current);

				var innerFlyoutItem = popups.Select(p
					=> ((p.Child as MenuFlyoutPresenter)?.TemplatedRoot as FrameworkElement)?.FindName("innerFlyoutItem") as MenuFlyoutItem).Trim().FirstOrDefault();

				Assert.IsNotNull(innerFlyoutItem);

				Assert.AreEqual("42", innerFlyoutItem.Text);
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups();
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
			var secondButton = new Button()
			{
				Content = "Do not focus me!"
			};

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
