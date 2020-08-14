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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Binding_TwoWay_BindBack_DataTemplate : Page
	{
		public Binding_TwoWay_BindBack_DataTemplate()
		{
			this.InitializeComponent();
		}
	}

	public class Binding_TwoWay_BindBack_DataTemplate_TestObject : Border
	{
		public string MyProperty
		{
			get { return (string)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(string), typeof(Binding_TwoWay_BindBack_DataTemplate_TestObject), new FrameworkPropertyMetadata(0));
	}

	public partial class Binding_TwoWay_BindBack_DataTemplate_Base : DependencyObject
	{
		public Binding_TwoWay_BindBack_DataTemplate_Data Model { get; } = new Binding_TwoWay_BindBack_DataTemplate_Data();

		public void MyBindBack(string value) => MyIntProperty = int.Parse(value);

		public int MyIntProperty
		{
			get { return (int)GetValue(MyIntPropertyProperty); }
			set { SetValue(MyIntPropertyProperty, value); }
		}

		public static readonly DependencyProperty MyIntPropertyProperty =
			DependencyProperty.Register("MyIntProperty", typeof(int), typeof(Binding_TwoWay_BindBack_DataTemplate_Base), new FrameworkPropertyMetadata(0));
	}

	public class Binding_TwoWay_BindBack_DataTemplate_Data : System.ComponentModel.INotifyPropertyChanged
	{
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		private int _myIntProperty;

		public int MyIntProperty
		{
			get => _myIntProperty;
			set
			{
				_myIntProperty = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(MyIntProperty)));
			}
		}

		public void MyBindBack(string value) => MyIntProperty = int.Parse(value);
	}
}
