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

		internal static readonly Class Class;

		private TestClass NullTestClass = null;
		private TestClass NotNullTestClass = new();

		static Functions_Control()
		{
			Class = new Class()
			{
				SubClass1 = new SubClass1()
				{
					SubClass2 = new SubClass2()
					{
						SubClass3 = new SubClass3()
						{
							Message = "Hello world!"
						}
					}
				}
			};
		}

		public Functions_Control()
		{
			this.InitializeComponent();
		}

		private int PrivateInstanceProperty => 41;

		public int InstanceProperty => 42;

		public static int StaticProperty => 43;

		private static int StaticPrivateProperty => 44;

		private static readonly int StaticPrivateReadonlyField = 45;

		private const int PrivateConstField = 46;

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
		public const int MyConst = -5;
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
		public static int PublicStaticProperty => 47;

		public const int PublicConstField = 48;
	}

	internal class Class
	{
		public SubClass1 SubClass1 { get; set; }
	}
	internal class SubClass1
	{
		public SubClass2 SubClass2 { get; set; }
	}
	internal class SubClass2
	{
		public SubClass3 SubClass3 { get; set; }
	}
	internal class SubClass3
	{
		public string Message { get; set; }
	}

	public class TestClass
	{
		public string NullString { get; set; }
	}

	public class MyTextBox : TextBox
	{
		public MyTextBox()
		{
			Text = "DefaultTextBoxText";
		}
	}
}
