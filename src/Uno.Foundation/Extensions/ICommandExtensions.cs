using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Uno.Client;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ICommandExtensions
{
	/// <summary>
	/// Executes the command if CanExecute returns true.
	/// </summary>
	/// <param name="command">The command</param>
	/// <param name="parameter">The parameter to use with the execution</param>
	public static void ExecuteIfPossible(this ICommand command, object parameter = null)
	{
		try
		{
			if (command != null && command.CanExecute(parameter))
			{					
				command.Execute(parameter);
			}
		}
		catch (ObjectDisposedException) { } // Not possible when the object is disposed!
	}
}
