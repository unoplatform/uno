#if NETFX_CORE
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
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			if (parameter is T t)
			{
				_action?.Invoke(t);
			}
			else if (parameter == null && !typeof(T).IsValueType)
			{
				_action?.Invoke(default(T));
			}
			else
			{
				throw new InvalidCastException($"parameter must be a {typeof(T)}");
			}
		}

		private void OnCanExecuteChanged(bool canExecute)
		{
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}
	}
}
#endif
