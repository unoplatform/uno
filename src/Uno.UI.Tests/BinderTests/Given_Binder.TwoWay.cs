using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

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
			Assert.AreEqual(dp2.MyInt, 7);
			Assert.AreEqual(dp1.MyInt, 8);
			Assert.AreEqual(1, conv.ConvertCount);
			Assert.AreEqual(1, conv.ConvertBackCount);

			dp1.MyInt = 19;
			Assert.AreEqual(19, dp1.MyInt);
			Assert.AreEqual(20, dp2.MyInt);
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


		public partial class MyDP : DependencyObject
		{
			public int MyInt
			{
				get { return (int)GetValue(MyIntProperty); }
				set { SetValue(MyIntProperty, value); }
			}

			public static readonly DependencyProperty MyIntProperty =
				DependencyProperty.Register("MyInt", typeof(int), typeof(MyDP), new FrameworkPropertyMetadata(0));
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
	}
}
