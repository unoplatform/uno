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
	public sealed partial class Functions_Control : Page
	{
		public int OffsetCallCount;
		public int AddIntCallCount;
		public int AddDoubleCallCount;

		public Functions_Control()
		{
			this.InitializeComponent();
		}

		public int InstanceProperty => 42;

		public static int StaticProperty => 43;

		private static int StaticPrivateProperty => 44;

		private static readonly int StaticPrivateReadonlyField = 45;

		public int InstanceDP
		{
			get => (int)GetValue(InstanceDPProperty);
			set => SetValue(InstanceDPProperty, value);
		}

		public MyxBindClass MyxBindClassInstance { get; } = new MyxBindClass();

		public static readonly DependencyProperty InstanceDPProperty =
			DependencyProperty.Register("InstanceDP", typeof(int), typeof(Functions_Control), new FrameworkPropertyMetadata(-1));

		private string Offset(int value)
		{
			OffsetCallCount++;
			return (value + 10).ToString();
		}

		private string Parameterless() => "Parameter-less result";
		private static string StaticParameterless() => "Static Parameter-less result";
		private string BoolFunc(bool flag) => flag ? "Was true" : "Was false";

		private string Add(int left, int right)
		{
			AddIntCallCount++;
			return (left + right).ToString();
		}

		private string Add(double left, double right)
		{
			AddDoubleCallCount++;
			return (left + right).ToString();
		}
	}

	public class MyxBindClass : System.ComponentModel.INotifyPropertyChanged
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

	public static class StaticClass
	{
		public static int PublicStaticProperty => 46;
	}
}
