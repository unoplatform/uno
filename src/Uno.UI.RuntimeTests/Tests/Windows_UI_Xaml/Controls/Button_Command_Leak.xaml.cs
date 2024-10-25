using System;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public partial class Button_Command_Leak : Page
	{
		public Button_Command_Leak()
		{
			InitializeComponent();

			InTreeButton.Command = ButtonCommand_Leak_Owner.TestCommand;
		}
	}

	public static class ButtonCommand_Leak_Owner
	{
		public static ICommand TestCommand { get; } = new Command();

		public class Command : ICommand
		{
			public Command()
			{
			}

			public event EventHandler CanExecuteChanged { add { } remove { } }

			public bool CanExecute(object parameter) => true;

			public void Execute(object parameter)
			{
			}
		}
	}
}
