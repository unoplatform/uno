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

		[TestMethod]
		public void When_Converter()
		{
			var SUT = new Binding_Converter();

			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.ForceLoaded();

			Assert.AreEqual("v:0 p:test", SUT.myTextBlock.Text);
			Assert.AreEqual("v:Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls.Binding_Converter p:test", SUT.myTextBlock2.Text);
		}

		[TestMethod]
		public void When_Converter_DataTemplate()
		{
			var SUT = new Binding_Converter_DataTemplate();
			var model = new Binding_Converter_DataTempate_Model();
			SUT.root.Content = model;

			Assert.AreEqual(0, model.MyIntProperty);

			SUT.ForceLoaded();

			var myTextBlock = SUT.FindName("myTextBlock") as Windows.UI.Xaml.Controls.TextBlock;
			var myTextBlock2 = SUT.FindName("myTextBlock2") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.AreEqual("v:0 p:test", myTextBlock.Text);
			Assert.AreEqual("v:Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls.Binding_Converter_DataTempate_Model p:test", myTextBlock2.Text);
		}
	}
}
