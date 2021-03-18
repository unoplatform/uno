using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using System.Threading;
using System.Diagnostics;
using System.Globalization;

namespace Uno.UI.Tests.BinderTests.Inversion
{
	[TestClass]
	public partial class Given_DependencyProperty_Inversion
	{
		[TestMethod]
		public void When_TwoWay_Converted()
		{
			var a = new MyDependencyObject() { PropName = "a" };
			var b = new MyDependencyObject() { PropName = "b" };
			var converter = new MyConverter();

			BindingOperations.SetBinding(b,
				MyDependencyObject.MyIntegerProperty,
				new Windows.UI.Xaml.Data.Binding
				{
					Path = new PropertyPath(nameof(b.MyInteger)),
					Converter = converter,
					Source = a,
					Mode = BindingMode.TwoWay
				}
			);

			Assert.AreEqual(1, converter.ConvertCallCount);
			Assert.AreEqual(0, converter.ConvertBackCallCount);
			Assert.AreEqual(1, b.MyInteger);

			a.MyInteger = 1;
			Assert.AreEqual(2, converter.ConvertCallCount);
			Assert.AreEqual(0, converter.ConvertBackCallCount);
			Assert.AreEqual(1, a.MyInteger);
			Assert.AreEqual(2, b.MyInteger);

			b.MyInteger = 2;
			Assert.AreEqual(2, converter.ConvertCallCount);
			Assert.AreEqual(0, converter.ConvertBackCallCount);
			Assert.AreEqual(1, a.MyInteger);
			Assert.AreEqual(2, b.MyInteger);

			b.MyInteger = 3;

			Assert.AreEqual(2, converter.ConvertCallCount);
			Assert.AreEqual(1, converter.ConvertBackCallCount);
			Assert.AreEqual(3, a.MyInteger);
			Assert.AreEqual(3, b.MyInteger);

			a.MyInteger = 42;
			Assert.AreEqual(3, converter.ConvertCallCount);
			Assert.AreEqual(1, converter.ConvertBackCallCount);
			Assert.AreEqual(42, a.MyInteger);
			Assert.AreEqual(43, b.MyInteger);
		}
	}

	public class MyConverter : IValueConverter
	{
		public int ConvertBackCallCount { get; private set; }
		public int ConvertCallCount { get; private set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			ConvertCallCount++;

			Debug.WriteLine($"Convert({value})");

			if (value is int)
			{
				return ((int)value) + 1;
			}
			else
			{
				return null;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			ConvertBackCallCount++;

			Debug.WriteLine($"ConvertBack({value})");

			if (value is int)
			{
				return ((int)value);
			}
			else
			{
				return null;
			}
		}
	}

	public partial class MyDependencyObject : DependencyObject
	{
		public string PropName { get; set; }

		public int MyInteger
		{
			get { return (int)GetValue(MyIntegerProperty); }
			set { SetValue(MyIntegerProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyBoolean.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyIntegerProperty =
			DependencyProperty.Register("MyInteger", typeof(int), typeof(MyDependencyObject), new PropertyMetadata(0, OnMyIntegerChanged));

		private static void OnMyIntegerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Debug.WriteLine($"{((MyDependencyObject)d).PropName}: {e.NewValue}");
		}
	}
}
