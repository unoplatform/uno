using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_Binder_TwoWay
	{
		[TestMethod]
		public void When_TwoWay_And_ConvertBack()
		{
			var dp1 = new MyDP();
			var dp2 = new MyDP();

			var conv = new IncrementConverter();
			var binding = new Binding
			{
				Source = dp1,
				Path = new PropertyPath(nameof(MyDP.MyInt)),
				Mode = BindingMode.TwoWay,
				Converter = conv
			};

			BindingOperations.SetBinding(dp2, MyDP.MyIntProperty, binding);

			Assert.AreEqual(1, dp2.MyInt);
			Assert.AreEqual(1, conv.ConvertCount);
			Assert.AreEqual(0, conv.ConvertBackCount);

			dp2.MyInt = 7;
			Assert.AreEqual(7, dp2.MyInt);
			Assert.AreEqual(8, dp1.MyInt);
			Assert.AreEqual(1, conv.ConvertCount);
			Assert.AreEqual(1, conv.ConvertBackCount);

			dp1.MyInt = 19;
			Assert.AreEqual(19, dp1.MyInt);
			Assert.AreEqual(20, dp2.MyInt);
			Assert.AreEqual(2, conv.ConvertCount);
			Assert.AreEqual(1, conv.ConvertBackCount);
		}

		[TestMethod]
		public void When_TwoWay_And_ConvertBack_Normal_Binding()
		{
			var dp1 = new MyDP();
			var dp2 = new MyDP();

			var conv = new StringToDoubleConverter();
			var binding = new Binding
			{
				Source = dp1,
				Path = new PropertyPath(nameof(MyDP.MyDouble)),
				Mode = BindingMode.TwoWay,
				Converter = conv
			};

			BindingOperations.SetBinding(dp2, MyDP.MyStringProperty, binding);

			Assert.AreEqual("$0.00", dp2.MyString);
			Assert.AreEqual(1, conv.ConvertCount);
			Assert.AreEqual(0, conv.ConvertBackCount);

			dp2.MyString = "42";

			// For normal bindings, the source update through the converter
			// is ignored. Therefore, only the ConvertBack method is invoked as
			// the UpdateSource method is ignored because a two-way binding is
			// in progress. This behavior is different with x:Bind.
			Assert.AreEqual("42", dp2.MyString);
			Assert.AreEqual(42, dp1.MyDouble);
			Assert.AreEqual(1, conv.ConvertCount);
			Assert.AreEqual(1, conv.ConvertBackCount);
		}

		[TestMethod]
		public void When_TwoWay_And_ConvertBack_Normal_xBind()
		{
			var dp1 = new MyDP();
			var dp2 = new MyDP();

			var conv = new StringToDoubleConverter();
			var binding = new Binding
			{
				Path = new PropertyPath(nameof(MyDP.MyDouble)),
				Mode = BindingMode.TwoWay,
				Converter = conv,
				CompiledSource = dp1,
			};

			BindingOperations.SetBinding(dp2, MyDP.MyStringProperty, binding);

			dp2.ApplyXBind();

			Assert.AreEqual("$0.00", dp2.MyString);
			Assert.AreEqual(1, conv.ConvertCount);
			Assert.AreEqual(0, conv.ConvertBackCount);

			dp2.MyString = "42";

			// For x:Bind, the source update through the converter raises
			// a property change, which is in turn sent back to the target
			// after another Convert invocation.
			//
			// There is no loop happening because the binding engine is ignoring
			// the UpdateSource invocation as a two-way binding is still happening.
			//
			// This behavior is different with a normal binding.
			Assert.AreEqual("$42.00", dp2.MyString);
			Assert.AreEqual(42, dp1.MyDouble);
			Assert.AreEqual(2, conv.ConvertCount);
			Assert.AreEqual(1, conv.ConvertBackCount);
		}

		[TestMethod]
		public void When_Modified_From_Callback()
		{
			var dp = new MyDP();
			var callbackCount = 0;

			dp.RegisterPropertyChangedCallback(MyDP.MyIntProperty, OnMyIntChanged);

			dp.MyInt = 11;

			Assert.AreEqual(42, dp.MyInt);

			Assert.AreEqual(2, callbackCount);

			void OnMyIntChanged(DependencyObject sender, DependencyProperty dpInner)
			{
				callbackCount++;
				(sender as MyDP).MyInt = 42;
			}
		}

		[TestMethod]
		public void When_Modified_From_Callback_Repeated()
		{
			var dp = new MyDP();
			var callbackCount = 0;

			dp.RegisterPropertyChangedCallback(MyDP.MyIntProperty, OnMyIntChanged);

			dp.MyInt = 11;

			Assert.AreEqual(42, dp.MyInt);

			Assert.AreEqual(32, callbackCount);

			void OnMyIntChanged(DependencyObject sender, DependencyProperty dpInner)
			{
				callbackCount++;
				if ((sender as MyDP).MyInt < 42)
				{
					(sender as MyDP).MyInt = (sender as MyDP).MyInt + 1;
				}
			}
		}

		[TestMethod]
		public void When_Modified_From_Callback_And_TwoWay()
		{
			var dp1 = new MyDP();
			var dp2 = new MyDP();

			var binding = new Binding
			{
				Source = dp1,
				Path = new PropertyPath(nameof(MyDP.MyInt)),
				Mode = BindingMode.TwoWay
			};

			BindingOperations.SetBinding(dp2, MyDP.MyIntProperty, binding);
			var callbackCount = 0;

			dp1.RegisterPropertyChangedCallback(MyDP.MyIntProperty, OnMyIntChanged);

			dp2.MyInt = 11;

			Assert.AreEqual(42, dp1.MyInt);
			Assert.AreEqual(11, dp2.MyInt);

			Assert.AreEqual(2, callbackCount);

			void OnMyIntChanged(DependencyObject sender, DependencyProperty dpInner)
			{
				callbackCount++;
				(sender as MyDP).MyInt = 42;
			}
		}

		[TestMethod]
		public void When_TwoWay_Binding_Overwritten()
		{

			var oldDC = new MyDC(3);
			var newDC = new MyDC(7);

			var slider = new Slider();

			slider.SetBinding(Slider.ValueProperty, new Binding { Path = new PropertyPath(nameof(MyDC.MyValue)), Mode = BindingMode.TwoWay, Source = oldDC });

			Assert.AreEqual(3, slider.Value);

			// Should remove previous binding
			slider.SetBinding(Slider.ValueProperty, new Binding { Path = new PropertyPath(nameof(MyDC.MyValue)), Mode = BindingMode.TwoWay, Source = newDC });

			Assert.AreEqual(7, slider.Value);

			Assert.AreEqual(3, oldDC.MyValue);
			Assert.AreEqual(7, newDC.MyValue);

			slider.Value = 12;

			Assert.AreEqual(3, oldDC.MyValue);
			Assert.AreEqual(12, newDC.MyValue);
		}


		public partial class MyDP : DependencyObject
		{
			public int MyInt
			{
				get { return (int)GetValue(MyIntProperty); }
				set { SetValue(MyIntProperty, value); }
			}

			public static readonly DependencyProperty MyIntProperty =
				DependencyProperty.Register(
					"MyInt",
					typeof(int),
					typeof(MyDP),
					new PropertyMetadata(0, propertyChangedCallback: (s, e) => { })
				);

			public string MyString
			{
				get { return (string)GetValue(MyStringProperty); }
				set { SetValue(MyStringProperty, value); }
			}

			public static readonly DependencyProperty MyStringProperty =
				DependencyProperty.Register(
					"MyString",
					typeof(string),
					typeof(MyDP),
					new PropertyMetadata(
						"",
						propertyChangedCallback: (s, e) => { }
					)
				);

			public double MyDouble
			{
				get { return (double)GetValue(MyDoubleProperty); }
				set { SetValue(MyDoubleProperty, value); }
			}

			// Using a DependencyProperty as the backing store for MyDouble.  This enables animation, styling, binding, etc...
			public static readonly DependencyProperty MyDoubleProperty =
				DependencyProperty.Register("MyDouble", typeof(double), typeof(MyDP), new PropertyMetadata(0.0, propertyChangedCallback: (s, e) => { }));
		}

		public class IncrementConverter : IValueConverter
		{
			public int ConvertCount { get; private set; }
			public int ConvertBackCount { get; private set; }
			public object Convert(object value, Type targetType, object parameter, string language)
			{
				ConvertCount++;
				if (value is int i)
				{
					return ++i;
				}

				return value;
			}

			public object ConvertBack(object value, Type targetType, object parameter, string language)
			{
				ConvertBackCount++;
				if (value is int i)
				{
					return ++i;
				}

				return value;
			}
		}

		class StringToDoubleConverter : IValueConverter
		{
			public int ConvertCount { get; set; }
			public int ConvertBackCount { get; set; }

			public object Convert(object value, Type targetType, object parameter, string language)
			{
				ConvertCount++;
				if (value != null)
				{
					var result = (double)value;
					return result.ToString("C");
				}
				else
				{
					return null;
				}
			}

			public object ConvertBack(object value, Type targetType, object parameter, string language)
			{
				ConvertBackCount++;
				if (value is string stringValue)
				{
					if (double.TryParse(stringValue, out var numberValue))
					{
						return numberValue;

					}
					else
					{
						return 0.00;
					}
				}
				else
				{
					return 0.00;
				}
			}
		}

		public class MyDC : System.ComponentModel.INotifyPropertyChanged
		{
			private int _value;

			public int MyValue
			{
				get => _value;
				set
				{
					if (value != _value)
					{
						_value = value;
						PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(MyValue)));
					}
				}
			}

			public MyDC(int value)
			{
				MyValue = value;
			}

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
		}
	}
}
