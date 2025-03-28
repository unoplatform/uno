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
	public sealed partial class Binding_DataTemplate_TwoWay : Page
	{
		public Binding_DataTemplate_TwoWay()
		{
			this.InitializeComponent();
		}
	}

	public class Binding_DataTemplate_TwoWayTestObject : Border
	{
		public int MyProperty
		{
			get { return (int)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(int), typeof(Binding_DataTemplate_TwoWayTestObject), new FrameworkPropertyMetadata(0));
	}

	public partial class Binding_DataTemplate_TwoWay_Base : DependencyObject
	{

		public Binding_DataTemplate_TwoWay_Data Model { get; } = new Binding_DataTemplate_TwoWay_Data();

		public int MyIntProperty
		{
			get { return (int)GetValue(MyIntPropertyProperty); }
			set { SetValue(MyIntPropertyProperty, value); }
		}

		public static readonly DependencyProperty MyIntPropertyProperty =
			DependencyProperty.Register("MyIntProperty", typeof(int), typeof(Binding_DataTemplate_TwoWay_Base), new FrameworkPropertyMetadata(0));

	}

	public class Binding_DataTemplate_TwoWay_Data : System.ComponentModel.INotifyPropertyChanged
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
	}
}
