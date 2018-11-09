using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Uno.UI.Common
{
	public class DelegateCommand<T> : ICommand
	{
		private Action<T> _action;

		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action<T> action)
		{
			_action = action;
			OnCanExecuteChanged(true);
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_action?.Invoke((T)parameter);
		}

		private void OnCanExecuteChanged(bool canExecute)
		{
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}
	}
}
