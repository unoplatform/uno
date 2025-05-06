using System;
using System.Windows.Input;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Common;

internal sealed class MenuCommand : ICommand
{
	private Action<object> _executeDelegate;
	private string _expectedParam;

	public MenuCommand(Action<object> executeDelegate, bool canExecuteFlag, string expectedParam)
	{
		_executeDelegate = executeDelegate;
		CanExecuteFlag = canExecuteFlag;
		_expectedParam = expectedParam;
	}

#pragma warning disable CS0067 // The event is never used
	public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067 // The event is never used

	public void Execute(object parameter)
	{
		LOG_OUTPUT("MenuCommand: Invoke Execute()!");
		VERIFY_IS_TRUE(_expectedParam == parameter.ToString());

		_executeDelegate(parameter);
	}

	public bool CanExecute(object parameter) => CanExecuteFlag;

	public bool CanExecuteFlag { get; set; }
}
