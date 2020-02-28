using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests
{
	[TestClass]
	public class Given_xBind_Binding
	{
		[TestMethod]
		[Ignore]
		public void When_Initial_Value()
		{
			var SUT = new Binding_Control();

			Assert.IsNull(SUT._StringField.Text);

			SUT.ForceLoaded();

			Assert.AreEqual("initial", SUT._StringField.Text);

			SUT.stringField = "updated";

			SUT.DoUpdate();

			Assert.AreEqual("updated", SUT._StringField.Text);
		}

		[TestMethod]
		public void When_StateTrigger()
		{
			var SUT = new Binding_StateTrigger();

			Assert.IsNull(SUT._StringField.Text);

			SUT.ForceLoaded();

			SUT.MyState = MyState.Full;

			Assert.AreEqual("Updated!", SUT._StringField.Text);
		}

		[TestMethod]
		public void When_Simple_TwoWay()
		{
			var SUT = new Binding_TwoWay_Simple();

			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.ForceLoaded();

			Assert.AreEqual(0, SUT.myObject.MyProperty);
			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.MyIntProperty = 1;

			Assert.AreEqual(1, SUT.myObject.MyProperty);

			Console.WriteLine("SUT.myObject.MyProperty = 2");
			SUT.myObject.MyProperty = 2;

			Assert.AreEqual(2, SUT.MyIntProperty);
		}

		[TestMethod]
		public void When_Simple_TwoWay_Nested()
		{
			var SUT = new Binding_TwoWay_Simple();

			Assert.AreEqual(0, SUT.Model.MyIntProperty);

			SUT.ForceLoaded();

			Assert.AreEqual(0, SUT.myObjectNestedProperty.MyProperty);
			Assert.AreEqual(0, SUT.Model.MyIntProperty);

			SUT.Model.MyIntProperty = 1;

			Assert.AreEqual(1, SUT.myObjectNestedProperty.MyProperty);

			Console.WriteLine("SUT.myObject.MyProperty = 2");
			SUT.myObjectNestedProperty.MyProperty = 2;

			Assert.AreEqual(2, SUT.Model.MyIntProperty);
		}

		[TestMethod]
		public void When_DataTemplate_TwoWay()
		{
			var SUT = new Binding_DataTemplate_TwoWay();

			var rootData = new Binding_DataTemplate_TwoWay_Base();
			SUT.root.Content = rootData;

			Assert.AreEqual(0, rootData.MyIntProperty);

			SUT.ForceLoaded();

			var myObject = SUT.FindName("myObject") as Binding_DataTemplate_TwoWayTestObject;

			Assert.AreEqual(0, myObject.MyProperty);
			Assert.AreEqual(0, rootData.MyIntProperty);

			rootData.MyIntProperty = 1;

			Assert.AreEqual(1, myObject.MyProperty);

			Console.WriteLine("myObject.MyProperty = 2");
			myObject.MyProperty = 2;

			Assert.AreEqual(2, rootData.MyIntProperty);
		}

		[TestMethod]
		public void When_DataTemplate_TwoWay_Nested()
		{
			var SUT = new Binding_DataTemplate_TwoWay();

			var rootData = new Binding_DataTemplate_TwoWay_Base();
			SUT.root.Content = rootData;

			Assert.AreEqual(0, rootData.Model.MyIntProperty);

			SUT.ForceLoaded();

			var myObjectNestedProperty = SUT.FindName("myObjectNestedProperty") as Binding_DataTemplate_TwoWayTestObject;

			Assert.AreEqual(0, myObjectNestedProperty.MyProperty);
			Assert.AreEqual(0, rootData.Model.MyIntProperty);

			rootData.Model.MyIntProperty = 1;

			Assert.AreEqual(1, myObjectNestedProperty.MyProperty);

			Console.WriteLine("myObjectNestedProperty.MyProperty = 2");
			myObjectNestedProperty.MyProperty = 2;

			Assert.AreEqual(2, rootData.Model.MyIntProperty);
		}

		[TestMethod]
		public void When_Object_TwoWay()
		{
			var SUT = new Binding_TwoWay_Object();

			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.ForceLoaded();

			Assert.AreEqual(0, SUT.myObject.MyProperty);
			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.MyIntProperty = 1;

			Assert.AreEqual(1, SUT.myObject.MyProperty);

			Console.WriteLine("SUT.myObject.MyProperty = 2");
			SUT.myObject.MyProperty = 2;

			Assert.AreEqual(2, SUT.MyIntProperty);
		}

		[TestMethod]
		public void When_Object_TwoWay_Nested()
		{
			var SUT = new Binding_TwoWay_Object();

			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.ForceLoaded();

			Assert.AreEqual(0, SUT.myObjectNestedProperty.MyProperty);
			Assert.AreEqual(0, SUT.Model.MyIntProperty);

			SUT.Model.MyIntProperty = 1;

			Assert.AreEqual(1, SUT.myObjectNestedProperty.MyProperty);

			Console.WriteLine("SUT.myObjectNestedProperty.MyProperty = 2");
			SUT.myObjectNestedProperty.MyProperty = 2;

			Assert.AreEqual(2, SUT.Model.MyIntProperty);
		}

		[TestMethod]
		public void When_TwoWay_Object_DataTemplate()
		{
			var SUT = new Binding_TwoWay_Object_DataTemplate();

			var rootData = new Binding_TwoWay_Object_DataTemplate_Base();
			SUT.root.Content = rootData;

			Assert.AreEqual(0, rootData.MyIntProperty);

			SUT.ForceLoaded();

			var myObject = SUT.FindName("myObject") as Binding_TwoWay_Object_DataTemplate_TestObject;

			Assert.AreEqual(0, myObject.MyProperty);
			Assert.AreEqual(0, rootData.MyIntProperty);

			rootData.MyIntProperty = 1;

			Assert.AreEqual(1, myObject.MyProperty);

			Console.WriteLine("myObject.MyProperty = 2");
			myObject.MyProperty = 2;

			Assert.AreEqual(2, rootData.MyIntProperty);
		}

		[TestMethod]
		public void When_TwoWay_Object_DataTemplate_Nested()
		{
			var SUT = new Binding_TwoWay_Object_DataTemplate();

			var rootData = new Binding_TwoWay_Object_DataTemplate_Base();
			SUT.root.Content = rootData;

			Assert.AreEqual(0, rootData.Model.MyIntProperty);

			SUT.ForceLoaded();

			var myObjectNestedProperty = SUT.FindName("myObjectNestedProperty") as Binding_TwoWay_Object_DataTemplate_TestObject;

			Assert.AreEqual(0, myObjectNestedProperty.MyProperty);
			Assert.AreEqual(0, rootData.Model.MyIntProperty);

			rootData.Model.MyIntProperty = 1;

			Assert.AreEqual(1, myObjectNestedProperty.MyProperty);

			Console.WriteLine("myObjectNestedProperty.MyProperty = 2");
			myObjectNestedProperty.MyProperty = 2;

			Assert.AreEqual(2, rootData.Model.MyIntProperty);
		}

		[TestMethod]
		public void When_TwoWay_BindBack()
		{
			var SUT = new Binding_TwoWay_BindBack();

			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.ForceLoaded();

			Assert.AreEqual("0", SUT.myObject.MyProperty);
			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.MyIntProperty = 1;

			Assert.AreEqual("1", SUT.myObject.MyProperty);

			Console.WriteLine("SUT.myObject.MyProperty = 2");
			SUT.myObject.MyProperty = "2";

			Assert.AreEqual(2, SUT.MyIntProperty);
		}

		[TestMethod]
		public void When_TwoWay_BindBack_Nested()
		{
			var SUT = new Binding_TwoWay_BindBack();

			Assert.AreEqual(0, SUT.Model.MyIntProperty);

			SUT.ForceLoaded();

			Assert.AreEqual("0", SUT.myObjectNestedProperty.MyProperty);
			Assert.AreEqual(0, SUT.Model.MyIntProperty);

			SUT.Model.MyIntProperty = 1;

			Assert.AreEqual("1", SUT.myObjectNestedProperty.MyProperty);

			Console.WriteLine("SUT.myObjectNestedProperty.MyProperty = 2");
			SUT.myObjectNestedProperty.MyProperty = "2";

			Assert.AreEqual(2, SUT.Model.MyIntProperty);
		}

		[TestMethod]
		public void When_TwoWay_BindBack_DataTemplate()
		{
			var SUT = new Binding_TwoWay_BindBack_DataTemplate();

			var rootData = new Binding_TwoWay_BindBack_DataTemplate_Base();
			SUT.root.Content = rootData;

			Assert.AreEqual(0, rootData.MyIntProperty);

			SUT.ForceLoaded();

			var myObject = SUT.FindName("myObject") as Binding_TwoWay_BindBack_DataTemplate_TestObject;

			Assert.AreEqual("0", myObject.MyProperty);
			Assert.AreEqual(0, rootData.MyIntProperty);

			rootData.MyIntProperty = 1;

			Assert.AreEqual("1", myObject.MyProperty);

			myObject.MyProperty = "2";

			Assert.AreEqual(2, rootData.MyIntProperty);
		}

		[TestMethod]
		public void When_TwoWay_BindBack_DataTemplate_Nested()
		{
			var SUT = new Binding_TwoWay_BindBack_DataTemplate();

			var rootData = new Binding_TwoWay_BindBack_DataTemplate_Base();
			SUT.root.Content = rootData;

			Assert.AreEqual(0, rootData.Model.MyIntProperty);

			SUT.ForceLoaded();

			var myObjectNestedProperty = SUT.FindName("myObjectNestedProperty") as Binding_TwoWay_BindBack_DataTemplate_TestObject;

			Assert.AreEqual("0", myObjectNestedProperty.MyProperty);
			Assert.AreEqual(0, rootData.Model.MyIntProperty);

			rootData.Model.MyIntProperty = 1;

			Assert.AreEqual("1", myObjectNestedProperty.MyProperty);

			myObjectNestedProperty.MyProperty = "2";

			Assert.AreEqual(2, rootData.Model.MyIntProperty);
		}
	}
}
