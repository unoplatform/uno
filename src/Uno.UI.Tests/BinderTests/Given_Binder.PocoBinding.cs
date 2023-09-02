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

namespace Uno.UI.Tests.BinderTests.DependencyPropertyPath
{
	[TestClass]
	public partial class Given_Binder_PocoBinding
	{
		[TestMethod]
		public void When_PropertyChanged_And_SetBinding()
		{
			var SUT = new MyObject();

			SUT.MyProperty = "41";

			SUT.SetBinding("MyProperty", new Binding { Path = "Value", Mode = BindingMode.TwoWay });

			SUT.DataContext = new { Value = "42" };

			Assert.AreEqual("42", SUT.MyProperty);
		}

		[TestMethod]
		public void When_SetBinding_And_PropertyChanged()
		{
			var SUT = new MyObject();

			SUT.SetBinding("MyProperty", new Binding { Path = "Value", Mode = BindingMode.TwoWay });

			SUT.MyProperty = "41";

			SUT.DataContext = new { Value = "42" };

			Assert.AreEqual("42", SUT.MyProperty);
		}

		public partial class MyObject : DependencyObject
		{
			public MyObject()
			{
			}

			private string _myProperty;

			public string MyProperty
			{
				get { return _myProperty; }
				set
				{
					_myProperty = value;

					SetBindingValue(value);
				}
			}
		}
	}
}
