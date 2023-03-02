using CommonServiceLocator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Uno.Disposables;
using System.ComponentModel;
using Uno.UI;
using Windows.UI.Xaml;
using System.Threading;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.BinderTests_Weak
{
	[TestClass]
	public partial class Given_DependencyProperty_Weak
	{
		[TestMethod]
		public void When_SimpleInheritance()
		{
			var SUT = new MyObject();

			SUT.SetValue(MyObject.ValueProperty, 42, DependencyPropertyValuePrecedences.Inheritance);
			Assert.AreEqual(42, SUT.GetValue(MyObject.ValueProperty));

			SUT.SetValue(MyObject.ValueProperty, 43, DependencyPropertyValuePrecedences.Local);
			Assert.AreEqual(43, SUT.GetValue(MyObject.ValueProperty));

			SUT.ClearValue(MyObject.ValueProperty, DependencyPropertyValuePrecedences.Local);
			Assert.AreEqual(42, SUT.GetValue(MyObject.ValueProperty));

			SUT.ClearValue(MyObject.ValueProperty, DependencyPropertyValuePrecedences.Inheritance);
			Assert.AreEqual(null, SUT.GetValue(MyObject.ValueProperty));
		}

		[TestMethod]
		public void When_Native()
		{
			var SUT = new MyNativeObject();
			var source = new MyObject();
			SUT.SetBinding(
					MyNativeObject.MyValueProperty,
					new Binding
					{
						Path = new PropertyPath("Value"),
						Source = source,
						Mode = BindingMode.OneWay
					}
				);

			Assert.AreEqual(0, SUT.MyValue);

			source.Value = 22;
			Assert.AreEqual(22, SUT.MyValue);
		}

		[TestMethod]
		public void When_Native_And_Collected()
		{
			var SUT = new MyNativeObject();
			var source = new MyObject();
			SUT.SetBinding(
					MyNativeObject.MyValueProperty,
					new Binding
					{
						Path = new PropertyPath("Value"),
						Source = source,
						Mode = BindingMode.OneWay
					}
				);

			Assert.AreEqual(0, SUT.MyValue);

			source.Value = 22;
			Assert.AreEqual(22, SUT.MyValue);

			SUT.Dispose();
			source.Value = 99;
			Assert.AreEqual(22, SUT.MyValue); //Binding shouldn't be updated after target is natively collected
		}
	}

	public partial class MyNativeObject : DependencyObject, INativeObject, IDisposable
	{
		public IntPtr Handle { get; private set; }

		public MyNativeObject()
		{
			Handle = new IntPtr(1);
		}

		public int MyValue
		{
			get { return (int)GetValue(MyValueProperty); }
			set { SetValue(MyValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyValueProperty =
			DependencyProperty.Register("MyValue", typeof(int), typeof(MyNativeObject), new FrameworkPropertyMetadata(0));



		public void Dispose()
		{
			Handle = IntPtr.Zero;
		}
	}

	public partial class MyObject : DependencyObject
	{
		public object Value
		{
			get => (MyObject)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		// Using a DependencyProperty as the backing store for InnerObject.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(
				name: "Value",
				propertyType: typeof(object),
				ownerType: typeof(MyObject),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.WeakStorage,
					propertyChangedCallback: (s, e) => ((MyObject)s)?.OnValueChanged(e)
				)
			);


		private void OnValueChanged(DependencyPropertyChangedEventArgs e)
		{
		}
	}
}
