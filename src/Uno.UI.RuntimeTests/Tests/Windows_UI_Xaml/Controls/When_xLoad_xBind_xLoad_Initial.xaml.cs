using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public sealed partial class When_xLoad_xBind_xLoad_Initial : UserControl
	{
		public When_xLoad_xBind_xLoad_Initial()
		{
			Model = new When_xLoad_xBind_xLoad_Initial_ViewModel();
			this.InitializeComponent();
		}

		public bool IsLoad() => true;

		public When_xLoad_xBind_xLoad_Initial_ViewModel Model { get; set; }
	}

	public class When_xLoad_xBind_xLoad_Initial_ViewModel : System.ComponentModel.INotifyPropertyChanged
	{
		private int myValue = 1;

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		public int MyValue
		{
			get => myValue; set
			{
				myValue = value;

				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("MyValue"));
			}
		}
	}
}
