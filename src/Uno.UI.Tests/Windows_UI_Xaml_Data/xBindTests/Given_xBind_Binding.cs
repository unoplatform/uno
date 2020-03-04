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
		private const int V = 42;

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

		[TestMethod]
		public void When_DefaultBindingMode_Undefined()
		{
			var SUT = new Binding_DefaultBindMode();

			Assert.IsNull(SUT.Default_undefined_Property);
			Assert.IsNull(SUT.Default_undefined_OneWay_Property);
			Assert.IsNull(SUT.Default_undefined_TwoWay_Property);

			SUT.Default_undefined_Property = "undefined updated 1";
			SUT.Default_undefined_OneWay_Property = "undefined updated 2";
			SUT.Default_undefined_TwoWay_Property = "undefined updated 3";

			SUT.ForceLoaded();

			Assert.AreEqual("undefined updated 1", SUT.Default_undefined.Text);
			Assert.AreEqual("undefined updated 2", SUT.Default_undefined_OneWay.Text);
			Assert.AreEqual("undefined updated 3", SUT.Default_undefined_TwoWay.Text);

			SUT.Default_undefined_Property = "undefined updated 4";
			SUT.Default_undefined_OneWay_Property = "undefined updated 5";
			SUT.Default_undefined_TwoWay_Property = "undefined updated 6";

			Assert.AreEqual("undefined updated 1", SUT.Default_undefined.Text);
			Assert.AreEqual("undefined updated 5", SUT.Default_undefined_OneWay.Text);
			Assert.AreEqual("undefined updated 6", SUT.Default_undefined_TwoWay.Text);

			SUT.Default_undefined.Text = "undefined updated 7";
			SUT.Default_undefined_OneWay.Text = "undefined updated 8";
			SUT.Default_undefined_TwoWay.Text = "undefined updated 9";

			Assert.AreEqual("undefined updated 4", SUT.Default_undefined_Property);
			Assert.AreEqual("undefined updated 5", SUT.Default_undefined_OneWay_Property);
			Assert.AreEqual("undefined updated 9", SUT.Default_undefined_TwoWay_Property);
		}

		[TestMethod]
		public void When_DefaultBindingMode_OneWay()
		{
			var SUT = new Binding_DefaultBindMode();

			Assert.IsNull(SUT.Default_OneWay_Property);
			Assert.IsNull(SUT.Default_OneWay_OneWay_Property);
			Assert.IsNull(SUT.Default_OneWay_TwoWay_Property);

			SUT.Default_OneWay_Property = "OneWay updated 1";
			SUT.Default_OneWay_OneWay_Property = "OneWay updated 2";
			SUT.Default_OneWay_TwoWay_Property = "OneWay updated 3";

			SUT.ForceLoaded();

			Assert.AreEqual("OneWay updated 1", SUT.Default_OneWay.Text);
			Assert.AreEqual("OneWay updated 2", SUT.Default_OneWay_OneWay.Text);
			Assert.AreEqual("OneWay updated 3", SUT.Default_OneWay_TwoWay.Text);

			SUT.Default_OneWay_Property = "OneWay updated 4";
			SUT.Default_OneWay_OneWay_Property = "OneWay updated 5";
			SUT.Default_OneWay_TwoWay_Property = "OneWay updated 6";

			Assert.AreEqual("OneWay updated 4", SUT.Default_OneWay.Text);
			Assert.AreEqual("OneWay updated 5", SUT.Default_OneWay_OneWay.Text);
			Assert.AreEqual("OneWay updated 6", SUT.Default_OneWay_TwoWay.Text);

			SUT.Default_OneWay.Text = "OneWay updated 7";
			SUT.Default_OneWay_OneWay.Text = "OneWay updated 8";
			SUT.Default_OneWay_TwoWay.Text = "OneWay updated 9";

			Assert.AreEqual("OneWay updated 4", SUT.Default_OneWay_Property);
			Assert.AreEqual("OneWay updated 5", SUT.Default_OneWay_OneWay_Property);
			Assert.AreEqual("OneWay updated 9", SUT.Default_OneWay_TwoWay_Property);
		}

		[TestMethod]
		public void When_DefaultBindingMode_TwoWay()
		{
			var SUT = new Binding_DefaultBindMode();

			Assert.IsNull(SUT.Default_TwoWay_Property);
			Assert.IsNull(SUT.Default_TwoWay_OneWay_Property);
			Assert.IsNull(SUT.Default_TwoWay_TwoWay_Property);

			SUT.Default_TwoWay_Property = "TwoWay updated 1";
			SUT.Default_TwoWay_OneWay_Property = "TwoWay updated 2";
			SUT.Default_TwoWay_TwoWay_Property = "TwoWay updated 3";

			SUT.ForceLoaded();

			Assert.AreEqual("TwoWay updated 1", SUT.Default_TwoWay.Text);
			Assert.AreEqual("TwoWay updated 2", SUT.Default_TwoWay_OneWay.Text);
			Assert.AreEqual("TwoWay updated 3", SUT.Default_TwoWay_TwoWay.Text);

			SUT.Default_TwoWay_Property = "TwoWay updated 4";
			SUT.Default_TwoWay_OneWay_Property = "TwoWay updated 5";
			SUT.Default_TwoWay_TwoWay_Property = "TwoWay updated 6";

			Assert.AreEqual("TwoWay updated 4", SUT.Default_TwoWay.Text);
			Assert.AreEqual("TwoWay updated 5", SUT.Default_TwoWay_OneWay.Text);
			Assert.AreEqual("TwoWay updated 6", SUT.Default_TwoWay_TwoWay.Text);

			SUT.Default_TwoWay.Text = "TwoWay updated 7";
			SUT.Default_TwoWay_OneWay.Text = "TwoWay updated 8";
			SUT.Default_TwoWay_TwoWay.Text = "TwoWay updated 9";

			Assert.AreEqual("TwoWay updated 7", SUT.Default_TwoWay_Property);
			Assert.AreEqual("TwoWay updated 5", SUT.Default_TwoWay_OneWay_Property);
			Assert.AreEqual("TwoWay updated 9", SUT.Default_TwoWay_TwoWay_Property);
		}

		[TestMethod]
		public void When_DefaultBindingMode_Nested()
		{
			var SUT = new Binding_DefaultBindMode();

			Assert.IsNull(SUT.Nested_Default_1_Property);
			Assert.IsNull(SUT.Nested_Default_2_Property);
			Assert.IsNull(SUT.Nested_Default_OneWay_OneWay_Property);
			Assert.IsNull(SUT.Nested_Default_OneWay_TwoWay_Property);
			Assert.IsNull(SUT.Nested_Default_OneWay_OneTime_Property);

			SUT.Nested_Default_1_Property = "nested updated 1";
			SUT.Nested_Default_2_Property = "nested updated 2";
			SUT.Nested_Default_OneWay_OneWay_Property = "nested updated 3";
			SUT.Nested_Default_OneWay_TwoWay_Property = "nested updated 4";
			SUT.Nested_Default_OneWay_OneTime_Property = "nested updated 41";

			SUT.ForceLoaded();

			Assert.AreEqual("nested updated 1", SUT.Nested_Default_1.Text);
			Assert.AreEqual("nested updated 2", SUT.Nested_Default_2.Text);
			Assert.AreEqual("nested updated 3", SUT.Nested_Default_OneWay_OneWay.Text);
			Assert.AreEqual("nested updated 4", SUT.Nested_Default_OneWay_TwoWay.Text);
			Assert.AreEqual("nested updated 41", SUT.Nested_Default_OneWay_OneTime.Text);

			SUT.Nested_Default_1_Property = "nested updated 5";
			SUT.Nested_Default_2_Property = "nested updated 6";
			SUT.Nested_Default_OneWay_OneWay_Property = "nested updated 7";
			SUT.Nested_Default_OneWay_TwoWay_Property = "nested updated 8";
			SUT.Nested_Default_OneWay_OneTime_Property = "nested updated 81";

			Assert.AreEqual("nested updated 5", SUT.Nested_Default_1.Text);
			Assert.AreEqual("nested updated 6", SUT.Nested_Default_2.Text);
			Assert.AreEqual("nested updated 7", SUT.Nested_Default_OneWay_OneWay.Text);
			Assert.AreEqual("nested updated 8", SUT.Nested_Default_OneWay_TwoWay.Text);
			Assert.AreEqual("nested updated 41", SUT.Nested_Default_OneWay_OneTime.Text);

			SUT.Nested_Default_1.Text = "nested updated 9";
			SUT.Nested_Default_2.Text = "nested updated 10";
			SUT.Nested_Default_OneWay_OneWay.Text = "nested updated 11";
			SUT.Nested_Default_OneWay_TwoWay.Text = "nested updated 12";
			SUT.Nested_Default_OneWay_OneTime.Text = "nested updated 121";

			Assert.AreEqual("nested updated 9", SUT.Nested_Default_1_Property);
			Assert.AreEqual("nested updated 6", SUT.Nested_Default_2_Property);
			Assert.AreEqual("nested updated 7", SUT.Nested_Default_OneWay_OneWay_Property);
			Assert.AreEqual("nested updated 12", SUT.Nested_Default_OneWay_TwoWay_Property);
			Assert.AreEqual("nested updated 81", SUT.Nested_Default_OneWay_OneTime_Property);
		}

		[TestMethod]
		public void When_TwoWay_NamedElement()
		{
			var SUT = new Binding_TwoWay_NamedElement();

			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.ForceLoaded();

			Assert.AreEqual(0, SUT.myObject.MyProperty);
			Assert.AreEqual(0, SUT.myObject2.MyProperty);
			Assert.AreEqual(0, SUT.MyIntProperty);

			SUT.MyIntProperty = 1;

			Assert.AreEqual(1, SUT.myObject.MyProperty);
			Assert.AreEqual(1, SUT.myObject2.MyProperty);

			SUT.myObject.MyProperty = 2;

			Assert.AreEqual(2, SUT.MyIntProperty);
			Assert.AreEqual(2, SUT.myObject2.MyProperty);

			SUT.myObject2.MyProperty = 3;

			Assert.AreEqual(3, SUT.MyIntProperty);
			Assert.AreEqual(3, SUT.myObject.MyProperty);
		}

		[TestMethod]
		public void When_TwoWay_InheritedProperty()
		{
			var SUT = new Binding_TwoWay_InheritedProperty();

			Assert.AreEqual(0, SUT.mySlider.Value);
			Assert.AreEqual(0, SUT.mySlider2.Value);

			SUT.ForceLoaded();

			Assert.AreEqual(0, SUT.mySlider.Value);
			Assert.AreEqual(0, SUT.mySlider2.Value);

			SUT.mySlider.Value = 42;
			Assert.AreEqual(42, SUT.mySlider2.Value);

			SUT.mySlider2.Value = 43;
			Assert.AreEqual(43, SUT.mySlider.Value);
		}

		[TestMethod]
		public void When_Primitive_DataTemplate()
		{
			var SUT = new Binding_Primitive_DataTemplate();

			SUT.ForceLoaded();

			var inner = SUT.root.FindName("inner") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.IsNull(inner.Text);

			SUT.root.Content = "hello!";

			Assert.AreEqual("hello!", inner.Text);
		}
	}
}
