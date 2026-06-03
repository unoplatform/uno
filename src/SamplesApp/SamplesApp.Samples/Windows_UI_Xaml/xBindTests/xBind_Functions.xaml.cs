using System;
using System.Collections.Generic;
using System.ComponentModel;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Xaml.xBindTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("x:Bind")]
	public sealed partial class xBind_Functions : Page
	{
		public xBind_Functions()
		{
			this.InitializeComponent();

			root.Content = new MyDataTemplateClass();
		}

		private void OnUpdateTemplate(object sender, object args)
		{
			if (root.Content is MyDataTemplateClass c)
			{
				c.MyProperty = "new update";
			}
		}

		private string Multiply(double a, double b)
			=> (a * b).ToString();
	}

	public static class StaticType
	{
		public static int PropertyIntValue { get; } = 42;

		public static string PropertyStringValue { get; } = "value 43";

		public static string Add(double a, double b)
			=> (a + b).ToString();
	}

	public class MyDataTemplateClass : System.ComponentModel.INotifyPropertyChanged
	{
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		private string _myproperty = "Initial";
		private int _myIntProperty = -3;

		public string MyProperty
		{
			get { return _myproperty; }
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
