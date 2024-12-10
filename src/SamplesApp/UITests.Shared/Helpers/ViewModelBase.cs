using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Disposables;
using Windows.UI.Core;
using Private.Infrastructure;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;
using Windows.UI.Xaml;
using SamplesApp;

namespace Uno.UI.Samples.UITests.Helpers
{
	[Windows.UI.Xaml.Data.Bindable]
	internal class ViewModelBase : INotifyPropertyChanged, IDisposable
	{
		public UnitTestDispatcherCompat Dispatcher { get; }

		public event PropertyChangedEventHandler PropertyChanged;

		protected readonly CompositeDisposable Disposables = new CompositeDisposable();
		protected readonly CancellationToken CT;

		public ViewModelBase() : this(UnitTestDispatcherCompat.Instance)
		{
		}

		public ViewModelBase(UnitTestDispatcherCompat dispatcher)
		{
			Dispatcher = dispatcher;

			var cts = new CancellationTokenSource();
			CT = cts.Token;

			Disposables.Add(Disposable.Create(() => cts.Cancel()));
		}

		protected void Set<T>(ref T backingField, T value, [CallerMemberName] string propertyName = "")
		{
			if (!Equals(backingField, value))
			{
				backingField = value;
				RaisePropertyChanged(propertyName);
			}
		}

		protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (Dispatcher.HasThreadAccess)
			{
				RaiseEvent();
			}
			else
			{
				var _ = Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, RaiseEvent);
			}

			void RaiseEvent()
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[{propertyName}]"));
			}
		}

		private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

		protected Command GetOrCreateCommand<T>(Action<T> action, [CallerMemberName] string commandName = null)
		{
			if (!_commands.TryGetValue(commandName, out var command))
			{
				_commands[commandName] = command = new Command(x => action((T)x));
			}
			return command;
		}

		protected Command GetOrCreateCommand(Action action, [CallerMemberName] string commandName = null)
		{
			if (!_commands.TryGetValue(commandName, out var command))
			{
				_commands[commandName] = command = new Command(_ => action());
			}
			return command;
		}

		public void Dispose()
		{
			Disposables.Dispose();
		}

		public object this[string propertyName]
		{
			// This is a hack to adapt samples to an old way to build viewmodels.
			// You don't need to use this for new samples.
			get
			{
				var type = GetType();
				var pi = type.GetProperty(propertyName,
					BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);
				var value = pi?.GetValue(this);
				return value;
			}
			set
			{
				var type = GetType();
				var pi = type.GetProperty(propertyName,
					BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);
				pi?.SetValue(this, value);
			}
		}

		[Windows.UI.Xaml.Data.Bindable]
		public class Command : ICommand
		{
			private readonly Action<object> _action;
			private readonly Func<object, Task> _actionTask;
			private readonly Func<object, bool> _canExecute;
			private bool _manualCanExecute = true;

			public bool ManualCanExecute
			{
				get => _manualCanExecute;
				set
				{
					_manualCanExecute = value;
					CanExecuteChanged?.Invoke(this, null);
				}
			}

			private object _isExecuting = null;
			private static object _isExecutingNull = new object(); // this represent a null parameter when something is executing

			public Command(Action<object> action, Func<object, bool> canExecute = null)
			{
				_action = action ?? throw new ArgumentNullException(nameof(action));
				_canExecute = canExecute;
			}

			public Command(Func<object, Task> actionTask, Func<object, bool> canExecute = null)
			{
				_actionTask = actionTask ?? throw new ArgumentNullException(nameof(actionTask));
				_canExecute = canExecute;
			}

			public bool CanExecute(object parameter)
			{
				var canExecuteParameter = parameter ?? _isExecutingNull;

				return (_isExecuting != canExecuteParameter)
					&& (_canExecute?.Invoke(parameter) ?? true)
					&& _manualCanExecute;
			}

			public async void Execute(object parameter)
			{
				var isExecutingParameter = parameter ?? _isExecutingNull;

				if (_isExecuting == isExecutingParameter)
				{
					// This parameter is executing
					return;
				}

				try
				{
					_isExecuting = isExecutingParameter;
					CanExecuteChanged?.Invoke(this, null);
					if (_action != null)
					{
						_action(parameter);
					}
					else
					{
						await _actionTask(parameter);
					}
				}
				finally
				{
					_isExecuting = null;
					CanExecuteChanged?.Invoke(this, null);
				}
			}

			public event EventHandler CanExecuteChanged;
		}
	}
}
