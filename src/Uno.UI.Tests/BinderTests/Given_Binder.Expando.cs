using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_Binder_Expando
	{
		[TestMethod]
		public void When_ReadValue()
		{
			var target = new MyControl();
			var source = new ExpandoObject();

			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding()
				{
					Path = new PropertyPath("TestDynamicProperty")
				}
			);

			var proxy = (IDictionary<string, object>)source;

			proxy["TestDynamicProperty"] = "Dynamic Value";

			target.DataContext = source;

			Assert.AreEqual("Dynamic Value", target.MyProperty);
		}


		[TestMethod]
		public void When_ReadValue_And_Update()
		{
			var target = new MyControl();
			var source = new ExpandoObject();

			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding()
				{
					Path = new PropertyPath("TestDynamicProperty")
				}
			);

			var proxy = (IDictionary<string, object>)source;

			proxy["TestDynamicProperty"] = "Dynamic Value";

			target.DataContext = source;
			Assert.AreEqual("Dynamic Value", target.MyProperty);

			proxy["TestDynamicProperty"] = "Dynamic Value2";

			Assert.AreEqual("Dynamic Value2", target.MyProperty);
		}

		[TestMethod]
		public void When_TwoWay_Binding()
		{
			var target = new MyControl();
			var source = new ExpandoObject();

			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding()
				{
					Path = new PropertyPath("TestDynamicProperty")
					,
					Mode = BindingMode.TwoWay
				}
			);

			var proxy = (IDictionary<string, object>)source;

			proxy["TestDynamicProperty"] = "Dynamic Value";

			target.DataContext = source;
			Assert.AreEqual("Dynamic Value", target.MyProperty);

			target.MyProperty = "target value";

			Assert.AreEqual("target value", proxy["TestDynamicProperty"]);
		}

		[TestMethod]
		public void When_Unknown_Property()
		{
			var target = new MyControl();
			var source = new ExpandoObject();

			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding()
				{
					Path = new PropertyPath("UnknownProperty")
					,
					Mode = BindingMode.TwoWay
				}
			);

			target.DataContext = source;

			Assert.IsNull(target.MyProperty);

			target.MyProperty = "42";

			var proxy = (IDictionary<string, object>)source;

			Assert.AreEqual("42", proxy["UnknownProperty"]);
		}

		public partial class MyControl : DependencyObject
		{
			public MyControl(MyControl parent = null)
			{
				this.SetParent(parent);
			}

			public string MyProperty
			{
				get { return (string)GetValue(MyPropertyProperty); }
				set { SetValue(MyPropertyProperty, value); }
			}

			public static readonly DependencyProperty MyPropertyProperty =
				DependencyProperty.Register("MyProperty", typeof(string), typeof(MyControl), new FrameworkPropertyMetadata(null));
		}
	}
}
