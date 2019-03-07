#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Windows.Input;

namespace Windows.UI.Xaml.Input
{
	public  partial class XamlUICommand : DependencyObject, global::System.Windows.Input.ICommand
	{
		[global::Uno.NotImplemented]
		public bool CanExecute(object parameter)
			=> throw new NotImplementedException("The member double XamlUICommand.CanExecute is not implemented in Uno.");

		[global::Uno.NotImplemented]
		public void Execute(object parameter)
			=> throw new NotImplementedException("The member double XamlUICommand.Execute is not implemented in Uno.");

		[global::Uno.NotImplemented]
		public event EventHandler CanExecuteChanged
		{
			add
			{
				throw new NotImplementedException("The member double XamlUICommand.add_CanExecuteChanged is not implemented in Uno.");
			}

			remove
			{
				throw new NotImplementedException("The member double XamlUICommand.remove_CanExecuteChanged is not implemented in Uno.");
			}
		}

	}
}
