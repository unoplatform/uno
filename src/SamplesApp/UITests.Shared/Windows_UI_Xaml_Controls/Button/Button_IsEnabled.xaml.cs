using System;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Buttons", "Button_IsEnabled", typeof(ButtonTestsViewModel), ignoreInSnapshotTests: true)]
	public sealed partial class Button_IsEnabled : UserControl
	{
		public Button_IsEnabled()
		{
			this.InitializeComponent();
		}

		private void ButtonIsEnabled_Click(object sender, RoutedEventArgs args)
		{
			Button.IsEnabled = ButtonIsEnabled.IsChecked == true;
		}

		private void ButtonCommand_Click(object sender, RoutedEventArgs args)
		{
			Button.Command = ButtonCommand.IsChecked == true
				? new Command { IsEnabled = ButtonCommandIsEnabled.IsChecked == true }
				: null;
		}

		private void ButtonCommandParameter_Click(object sender, RoutedEventArgs args)
		{
			Button.CommandParameter = ButtonCommandParameter.IsChecked == true;
		}

		private void ButtonCommandIsEnabled_Click(object sender, RoutedEventArgs args)
		{
			if (Button.Command is Command command)
			{
				command.IsEnabled = ButtonCommandIsEnabled.IsChecked == true;
			}
		}

		private class Command : ICommand
		{
			private bool _isEnabled;

			public Command()
			{
				CanExecuteChanged += MyCommand_CanExecuteChanged;
			}

			public bool IsEnabled
			{
				get => _isEnabled;
				set
				{
					if (_isEnabled != value)
					{
						_isEnabled = value;
						CanExecuteChanged?.Invoke(this, null);
					}
				}
			}

			#region ICommand

			private void MyCommand_CanExecuteChanged(object sender, EventArgs e)
			{
			}

			public event EventHandler CanExecuteChanged;

			public bool CanExecute(object parameter)
			{
				return IsEnabled && parameter is bool b && b;
			}

			public void Execute(object parameter)
			{
			}

			#endregion
		}
	}
}
