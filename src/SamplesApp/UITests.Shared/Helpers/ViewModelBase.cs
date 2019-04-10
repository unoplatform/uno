using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Input;
using Uno.Disposables;
using Windows.UI.Core;

namespace Uno.UI.Samples.UITests.Helpers
{
	[Windows.UI.Xaml.Data.Bindable]
	public class ViewModelBase : INotifyPropertyChanged, IDisposable
	{
		public CoreDispatcher Dispatcher { get; }

		public event PropertyChangedEventHandler PropertyChanged;

		protected readonly CompositeDisposable Disposables = new CompositeDisposable();
		protected readonly CancellationToken CT;

		public ViewModelBase(CoreDispatcher dispatcher)
		{
			Dispatcher = dispatcher;

			var cts = new CancellationTokenSource();
			CT = cts.Token;

			Disposables.Add(Disposable.Create(() => cts.Cancel()));
		}

		protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (Dispatcher.HasThreadAccess)
			{
				RaiseEvent();
			}
			else
			{
				var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, RaiseEvent);
			}

			void RaiseEvent()
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[{propertyName}]"));
			}
		}

		protected static Command CreateCommand<T>(Action<T> action)
		{
			return new Command(x => action((T)x));
		}

		protected static Command CreateCommand(Action action)
		{
			return new Command(_ => action());
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
			private bool _manualCanExecute = true;

			public bool ManualCanExecute
			{
				get => _manualCanExecute;
				set
				{
					_manualCanExecute = value;
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
				}
			}

			private bool _isExecuting = false;

			public Command(Action<object> action)
			{
				_action = action;
			}

			public bool CanExecute(object parameter)
			{
				return !_isExecuting && _manualCanExecute;
			}

			public void Execute(object parameter)
			{
				if (_isExecuting)
				{
					return;
				}

				try
				{
					_isExecuting = true;
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					_action(parameter);
				}
				finally
				{
					_isExecuting = false;
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
				}
			}

			public event EventHandler CanExecuteChanged;
		}
	}
}
