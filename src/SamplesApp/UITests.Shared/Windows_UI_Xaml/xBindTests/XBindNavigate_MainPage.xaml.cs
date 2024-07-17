using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.UITests.Helpers;

namespace UITests.Shared.Windows_UI_Xaml.xBindTests
{
	public sealed partial class XBindNavigate_MainPage : Page
	{
		public XBindNavigate_MainPage()
		{
			this.InitializeComponent();
		}

		internal MainViewModel ViewModel => MainViewModel.Instance;

		public void GoToSecond() => Frame.Navigate(typeof(XBindNavigate_SecondPage));
	}

	internal partial class MainViewModel : ViewModelBase
	{
		private NestedViewModel _nested;

		public static MainViewModel Instance { get; } = new MainViewModel();

		public NestedViewModel Nested { get; set; } = new NestedViewModel();

		public void Increment()
		{
			Nested.Value++;
		}
	}

	internal partial class NestedViewModel : ViewModelBase
	{
		private int _value = 0;

		public int Value
		{
			get => _value;
			set
			{
				_value = value;
				RaisePropertyChanged();
			}
		}
	}
}
