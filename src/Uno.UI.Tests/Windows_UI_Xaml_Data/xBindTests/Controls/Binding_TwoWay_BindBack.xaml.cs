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
	public sealed partial class Binding_TwoWay_BindBack : Page
	{
		public Binding_TwoWay_BindBack()
		{
			this.InitializeComponent();
		}

		public TwoWay_BindBack_Model Model { get; } = new TwoWay_BindBack_Model();

		public void MyBindBack(string value) => MyIntProperty = int.Parse(value);

		public int MyIntProperty
		{
			get { return (int)GetValue(MyIntPropertyProperty); }
			set { SetValue(MyIntPropertyProperty, value); }
		}

		public static readonly DependencyProperty MyIntPropertyProperty =
			DependencyProperty.Register("MyIntProperty", typeof(int), typeof(Binding_TwoWay_BindBack), new FrameworkPropertyMetadata(0));
	}

	public class TwoWay_BindBack_TestObject : Border
	{
		public string MyProperty
		{
			get { return (string)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(string), typeof(TwoWay_BindBack_TestObject), new FrameworkPropertyMetadata(0));
	}

	public class TwoWay_BindBack_Model : System.ComponentModel.INotifyPropertyChanged
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
