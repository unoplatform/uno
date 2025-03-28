using System;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;

public sealed partial class Uno9059 : Page
{
	public Uno9059()
	{
		this.InitializeComponent();
		this.DataContext = new Uno9059_VM(() =>
		{
		});
	}
}

public class Uno9059_VM
{
	public Uno9059_VM(Action action)
	{
		CustomCommand = new Uno9059_RelayCommand(action);
	}

	public Uno9059_RelayCommand CustomCommand { get; set; }
}
public partial class Uno9059_CustomControl : Control
{
	#region DependencyProperty: Action1

	public static DependencyProperty Action1Property { get; } = DependencyProperty.Register(
		nameof(Action1),
		typeof(ICommand),
		typeof(Uno9059_CustomControl),
		new PropertyMetadata(default(ICommand)));

	public ICommand Action1
	{
		get => (ICommand)GetValue(Action1Property);
		set => SetValue(Action1Property, value);
	}

	#endregion
}
public class Uno9059_RelayCommand : ICommand
{
	public event EventHandler CanExecuteChanged;
	private Action _execute;

	public Uno9059_RelayCommand(Action execute)
	{
		_execute = execute;

		CanExecuteChanged?.ToString(); // error CS0067: The event 'Uno9059_RelayCommand.CanExecuteChanged' is never used
	}

	public bool CanExecute(object parameter) => true;

	public void Execute(object parameter) => _execute?.Invoke();
}
