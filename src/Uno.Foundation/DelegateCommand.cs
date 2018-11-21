using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Uno.UI.Common
{
	public class DelegateCommand : ICommand
	{
		private Action _action;

		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action action)
		{
			_action = action;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_action?.Invoke();
		}

		private void OnCanExecuteChanged(bool canExecute)
		{
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}
	}
}
