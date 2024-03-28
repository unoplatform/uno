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
	public partial class Given_Binder_DynamicObject
	{
		[TestMethod]
		public void When_ReadValue()
		{
			var target = new MyControl();
			dynamic source = new MyDynamicObject();

			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding()
				{
					Path = new PropertyPath("TestDynamicProperty")
				}
			);

			source.TestDynamicProperty = "Dynamic Value";

			target.DataContext = source;

			Assert.AreEqual("Dynamic Value", target.MyProperty);
		}


		[TestMethod]
		public void When_ReadValue_And_Update()
		{
			var target = new MyControl();
			dynamic source = new MyDynamicObject();

			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding()
				{
					Path = new PropertyPath("TestDynamicProperty")
				}
			);

			source.TestDynamicProperty = "Dynamic Value";

			target.DataContext = source;
			Assert.AreEqual("Dynamic Value", target.MyProperty);

			source.TestDynamicProperty = "Dynamic Value2";

			Assert.AreEqual("Dynamic Value2", target.MyProperty);
		}

		[TestMethod]
		public void When_TwoWay_Binding()
		{
			var target = new MyControl();
			dynamic source = new MyDynamicObject();

			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding()
				{
					Path = new PropertyPath("TestDynamicProperty")
					,
					Mode = BindingMode.TwoWay
				}
			);

			source.TestDynamicProperty = "Dynamic Value";

			target.DataContext = source;
			Assert.AreEqual("Dynamic Value", target.MyProperty);

			target.MyProperty = "target value";

			Assert.AreEqual("target value", source.TestDynamicProperty);
		}

		[TestMethod]
		public void When_Unknown_Property()
		{
			var target = new MyControl();
			dynamic source = new MyDynamicObject();

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

			Assert.AreEqual("42", source.UnknownProperty);
		}

		public class MyDynamicObject : DynamicObject, System.ComponentModel.INotifyPropertyChanged
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();

			public int Count => dictionary.Count;

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			public override bool TryGetMember(GetMemberBinder binder, out object result)
				=> dictionary.TryGetValue(binder.Name, out result);

			public override bool TrySetMember(SetMemberBinder binder, object value)
			{
				dictionary[binder.Name] = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(binder.Name));
				return true;
			}
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
