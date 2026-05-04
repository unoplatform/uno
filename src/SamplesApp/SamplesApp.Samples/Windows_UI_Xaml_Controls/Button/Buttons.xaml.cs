using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Buttons", Name = "Buttons")]
	public sealed partial class Buttons : UserControl
	{
		private ButtonsViewModel _viewModel;

		public Buttons()
		{
			this.InitializeComponent();

			Loading += (s, e) => DataContext = _viewModel = new ButtonsViewModel();
			Unloaded += (s, e) => DataContext = _viewModel = null;
		}

		private void OnClick(object sender, RoutedEventArgs e) => _viewModel.Log($"{sender}.Click");
		private void OnTapped(object sender, TappedRoutedEventArgs e) => _viewModel.Log($"{sender}.Tapped");

		protected override void OnTapped(TappedRoutedEventArgs e)
		{
			_viewModel.Log($"[page].Tapped, source={e.OriginalSource}");
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			_viewModel.Log($"[page].PointerPressed, source={e.OriginalSource}");
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			_viewModel.Log($"[page].PointerReleased, source={e.OriginalSource}");
		}
	}

	public class MyCollection : List<string>, INotifyCollectionChanged
	{
		NotifyCollectionChangedEventHandler _collectionChanged;

		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add => _collectionChanged += value;
			remove => _collectionChanged -= value;
		}
	}

	public class ButtonsViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public ButtonsViewModel()
		{
			Command = (ActionCommand)OnCommand;
			ClearCommand = (ActionCommand)Clear;
		}

		private bool _isEnabled = true;
		public bool IsEnabled
		{
			get { return _isEnabled; }
			set
			{
				_isEnabled = value;
				NotifyPropertyChanged();
			}
		}

		private bool _isChecked = false;
		public bool IsChecked
		{
			get { return _isChecked; }
			set
			{
				_isChecked = value;
				NotifyPropertyChanged();
			}
		}

		public System.Windows.Input.ICommand Command { get; private set; }
		public System.Windows.Input.ICommand ClearCommand { get; private set; }
		public MyCollection Output { get; } = new MyCollection();

		private void OnCommand(object sender) => Log($"{sender}.Command");

		public void Clear() => Output.Clear();
		public void Log(object parameter) => Output.Add(parameter.ToString());

		private void NotifyPropertyChanged([CallerMemberName] string property = "*") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
	}

	public class ActionCommand : System.Windows.Input.ICommand
	{
		private Action<object> _action;

		public ActionCommand(Action action)
		{
			_action = new Action<object>(_ => action());
		}

		public ActionCommand(Action<object> action)
		{
			_action = action;
		}

#pragma warning disable CS0067
		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter) => true;

		public void Execute(object parameter) => _action(parameter);

		public static implicit operator ActionCommand(Action action) => new ActionCommand(action);
		public static implicit operator ActionCommand(Action<object> action) => new ActionCommand(action);
	}
}
