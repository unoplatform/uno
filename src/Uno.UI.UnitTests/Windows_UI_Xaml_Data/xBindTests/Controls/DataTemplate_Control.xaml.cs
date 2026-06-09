using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class DataTemplate_Control : Page
	{
		public DataTemplate_Control()
		{
			this.InitializeComponent();
			root.Content = new MyDataTemplateClass();
		}
	}

	public class MyDataTemplateClass : System.ComponentModel.INotifyPropertyChanged
	{
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		private string _myproperty = "Initial";
		private int _myIntProperty = -3;

		public int MyPropertyGetCounter { get; set; } = 0;

		internal bool HasPropertyChangedListeners => PropertyChanged != null;

		public string MyProperty
		{
			get
			{
				MyPropertyGetCounter++;

				return _myproperty;
			}
			set
			{
				_myproperty = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(MyProperty)));
			}
		}

		public int MyIntProperty
		{
			get { return _myIntProperty; }
			set
			{
				_myIntProperty = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(MyIntProperty)));
			}
		}
	}

}
