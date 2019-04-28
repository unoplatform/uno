using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace SamplesApp
{
	/// <summary>
	/// 可用于自身或导航至 Frame 内部的空白页。
	/// </summary>
	public sealed partial class ListViewBindingPage : Page
	{
		public ListViewBindingPage()
		{
			this.InitializeComponent();
			this.DataContext = new ListViewBindingViewModel();
		}


	}


	public class ListViewBindingViewModel : INotifyPropertyChanged
	{
		public ListViewBindingViewModel()
		{

			TestCommand = new TestListBindingDelegateCommand(() =>
			{
				System.Diagnostics.Debug.WriteLine("test");
			});
		}


		private ICommand _testCommand;

		public ICommand TestCommand
		{
			get { return _testCommand; }
			set { _testCommand = value; }
		}

		public virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (object.Equals((object)storage, (object)value))
			{
				return false;
			}
			storage = value;
			RaisePropertyChanged(propertyName);
			return true;
		}
		protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged.Invoke((object)this, new PropertyChangedEventArgs(propertyName));
			}

		}

		public event PropertyChangedEventHandler PropertyChanged;
	}


	public class TestListBindingDelegateCommand : ICommand
	{
		public TestListBindingDelegateCommand(Action action)
		{
			_action = action;
			CanExecuteChanged = null;

		}
		private Action _action;
		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_action.Invoke();
		}
	}
}
