using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;
using Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests
{
	[TestClass]
	public class Given_xBind_Binding
	{
		private const int V = 42;
		private bool _previousPoolingEnabled;

		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();

			_previousPoolingEnabled = FrameworkTemplatePool.IsPoolingEnabled;
			FrameworkTemplatePool.IsPoolingEnabled = false;
		}

		[TestCleanup]
		public void Cleanup()
		{
			FrameworkTemplatePool.IsPoolingEnabled = _previousPoolingEnabled;
			FrameworkTemplatePool.Scavenge();
		}

		[TestMethod]
		public void When_Initial_Value()
		{
			var SUT = new Binding_Control();

			Assert.AreEqual("", SUT._StringField.Text);

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

			Assert.AreEqual(string.Empty, SUT._StringField.Text);
			Assert.IsFalse(SUT.myTrigger.IsActive);

			SUT.ForceLoaded();

			SUT.MyState = MyState.Full;

			Assert.IsTrue(SUT.myTrigger.IsActive);
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
		public void When_Converter_TwoWay()
		{
			var SUT = new Binding_Converter_TwoWay();

			SUT.ForceLoaded();

			ListView list = SUT.ViewToggleListView;

			CheckBox cb = SUT.BoundCheckBox;

			Assert.AreEqual(0, list.SelectedIndex);
			Assert.IsTrue(cb.IsChecked.Value);

			list.SelectedItem = list.Items[1];

			Assert.AreEqual(1, list.SelectedIndex);
			Assert.IsFalse(cb.IsChecked.Value);

			list.SelectedItem = list.Items[0];

			Assert.AreEqual(0, list.SelectedIndex);
			Assert.IsTrue(cb.IsChecked.Value);
		}

		[TestMethod]
		public void When_ConverterParameter()
		{
			var SUT = new Binding_Converter_Parameter();

			SUT.ForceLoaded();

			Assert.AreEqual("Started: Jan 01, 2020 01:01 AM", SUT._StringField.Text);
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
		public void When_DefaultBindingMode_DataTemplate_Undefined()
		{
			var SUT = new Binding_DefaultBindMode_DataTemplate();
			var model = new Binding_DefaultBindMode_DataTemplate_Model();

			var default_undefined = (TextBlock)SUT.FindName("Default_undefined");
			var default_undefined_OneWay = (TextBlock)SUT.FindName("Default_undefined_OneWay");
			var default_undefined_TwoWay = (TextBlock)SUT.FindName("Default_undefined_TwoWay");

			Assert.IsNull(model.Default_undefined_Property);
			Assert.IsNull(model.Default_undefined_OneWay_Property);
			Assert.IsNull(model.Default_undefined_TwoWay_Property);

			model.Default_undefined_Property = "undefined updated 1";
			model.Default_undefined_OneWay_Property = "undefined updated 2";
			model.Default_undefined_TwoWay_Property = "undefined updated 3";

			SUT.DataContext = model;

			SUT.ForceLoaded();

			Assert.AreEqual("undefined updated 1", default_undefined.Text);
			Assert.AreEqual("undefined updated 2", default_undefined_OneWay.Text);
			Assert.AreEqual("undefined updated 3", default_undefined_TwoWay.Text);

			model.Default_undefined_Property = "undefined updated 4";
			model.Default_undefined_OneWay_Property = "undefined updated 5";
			model.Default_undefined_TwoWay_Property = "undefined updated 6";

			Assert.AreEqual("undefined updated 4", default_undefined.Text);
			Assert.AreEqual("undefined updated 5", default_undefined_OneWay.Text);
			Assert.AreEqual("undefined updated 6", default_undefined_TwoWay.Text);

			default_undefined.Text = "undefined updated 7";
			default_undefined_OneWay.Text = "undefined updated 8";
			default_undefined_TwoWay.Text = "undefined updated 9";

			Assert.AreEqual("undefined updated 4", model.Default_undefined_Property);
			Assert.AreEqual("undefined updated 5", model.Default_undefined_OneWay_Property);
			Assert.AreEqual("undefined updated 9", model.Default_undefined_TwoWay_Property);
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

			var inner = SUT.root.FindName("inner") as TextBlock;

			Assert.AreEqual(string.Empty, inner.Text);

			SUT.root.Content = "hello!";

			Assert.AreEqual("hello!", inner.Text);
		}

		[TestMethod]
		public void When_TypeMismatch()
		{
			var SUT = new Binding_TypeMismatch();

			SUT.ForceLoaded();

			var slider = SUT.FindName("mySlider") as Slider;
			var textBlock = SUT.FindName("myTextBlock") as TextBlock;

			Assert.AreEqual(0.0, slider.Value);
			Assert.AreEqual("0", textBlock.Text);
			Assert.AreEqual(0, SUT.MyInteger);

			slider.Minimum = 10.0;

			Assert.AreEqual(10.0, slider.Value);
			Assert.AreEqual(10, SUT.MyInteger);
			Assert.AreEqual("10", textBlock.Text);
		}

		[TestMethod]
		public void When_TypeMismatch_DataTemplate()
		{
			var SUT = new Binding_TypeMismatch_DataTemplate();

			var rootData = new Binding_TypeMismatch_DataTemplate_Data();
			SUT.root.Content = rootData;

			Assert.AreEqual(0, rootData.MyInteger);

			SUT.ForceLoaded();

			var slider = SUT.FindName("mySlider") as Slider;
			var textBlock = SUT.FindName("myTextBlock") as TextBlock;

			Assert.AreEqual(0.0, slider.Value);
			Assert.AreEqual(0, rootData.MyInteger);
			Assert.AreEqual("0", textBlock.Text);

			slider.Minimum = 10.0;

			Assert.AreEqual(10.0, slider.Value);
			Assert.AreEqual(10, rootData.MyInteger);
			Assert.AreEqual("10", textBlock.Text);
		}

		[TestMethod]
		public void When_Event()
		{
			var SUT = new Binding_Event();

			SUT.ForceLoaded();

			var checkBox = SUT.FindName("myCheckBox") as CheckBox;

			Assert.AreEqual(0, SUT.CheckedRaised);
			Assert.AreEqual(0, SUT.UncheckedRaised);

			checkBox.IsChecked = true;

			Assert.AreEqual(1, SUT.CheckedRaised);
			Assert.AreEqual(0, SUT.UncheckedRaised);

			checkBox.IsChecked = false;

			Assert.AreEqual(1, SUT.CheckedRaised);
			Assert.AreEqual(1, SUT.UncheckedRaised);
		}

		[TestMethod]
		public void When_Static_Event()
		{
			var SUT = new Binding_Static_Event();

			SUT.ForceLoaded();

			var checkBox = SUT.FindName("myCheckBox") as CheckBox;

			Assert.AreEqual(0, Binding_Static_Event_Class.CheckedRaised);
			Assert.AreEqual(0, Binding_Static_Event_Class.UncheckedRaised);

			checkBox.IsChecked = true;

			Assert.AreEqual(1, Binding_Static_Event_Class.CheckedRaised);
			Assert.AreEqual(0, Binding_Static_Event_Class.UncheckedRaised);

			checkBox.IsChecked = false;

			Assert.AreEqual(1, Binding_Static_Event_Class.CheckedRaised);
			Assert.AreEqual(1, Binding_Static_Event_Class.UncheckedRaised);
		}

		[TestMethod]
		public void When_Event_Nested()
		{
			var SUT = new Binding_Event_Nested();

			SUT.ForceLoaded();

			var checkBox = SUT.FindName("myCheckBox") as CheckBox;

			Assert.AreEqual(0, SUT.ViewModel.CheckedRaised);
			Assert.AreEqual(0, SUT.ViewModel.UncheckedRaised);

			checkBox.IsChecked = true;

			Assert.AreEqual(1, SUT.ViewModel.CheckedRaised);
			Assert.AreEqual(0, SUT.ViewModel.UncheckedRaised);

			checkBox.IsChecked = false;

			Assert.AreEqual(1, SUT.ViewModel.CheckedRaised);
			Assert.AreEqual(1, SUT.ViewModel.UncheckedRaised);
		}

		[TestMethod]
		public void When_Event_DataTemplate()
		{
			var SUT = new Binding_Event_DataTemplate();

			SUT.ForceLoaded();

			var root = SUT.FindName("root") as FrameworkElement;
			var dc = new Binding_Event_DataTemplate_Model();
			root.DataContext = dc;

			var checkBox = SUT.FindName("myCheckBox") as CheckBox;

			Assert.AreEqual(0, dc.CheckedRaised);
			Assert.AreEqual(0, dc.UncheckedRaised);

			checkBox.IsChecked = true;

			Assert.AreEqual(1, dc.CheckedRaised);
			Assert.AreEqual(0, dc.UncheckedRaised);

			checkBox.IsChecked = false;

			Assert.AreEqual(1, dc.CheckedRaised);
			Assert.AreEqual(1, dc.UncheckedRaised);
		}


		[TestMethod]
		public void When_Static_Event_DataTemplate()
		{
			var SUT = new Binding_Static_Event_DataTemplate();

			SUT.ForceLoaded();

			var root = SUT.FindName("root") as FrameworkElement;
			root.DataContext = new object();

			var checkBox = SUT.FindName("myCheckBox") as CheckBox;

			Assert.AreEqual(0, Binding_Static_Event_DataTemplate_Model_Class.CheckedRaised);
			Assert.AreEqual(0, Binding_Static_Event_DataTemplate_Model_Class.UncheckedRaised);

			checkBox.IsChecked = true;

			Assert.AreEqual(1, Binding_Static_Event_DataTemplate_Model_Class.CheckedRaised);
			Assert.AreEqual(0, Binding_Static_Event_DataTemplate_Model_Class.UncheckedRaised);

			checkBox.IsChecked = false;

			Assert.AreEqual(1, Binding_Static_Event_DataTemplate_Model_Class.CheckedRaised);
			Assert.AreEqual(1, Binding_Static_Event_DataTemplate_Model_Class.UncheckedRaised);
		}

		[TestMethod]
		public void When_Event_Nested_DataTemplate()
		{
			var SUT = new Binding_Event_Nested_DataTemplate();

			var root = SUT.FindName("root") as FrameworkElement;
			var dc = new Binding_Event_Nested_DataTemplate_Model();
			root.DataContext = dc;

			SUT.ForceLoaded();
			root.ForceLoaded();

			var checkBox = SUT.FindName("myCheckBox") as CheckBox;

			Assert.AreEqual(0, dc.ViewModel.CheckedRaised);
			Assert.AreEqual(0, dc.ViewModel.UncheckedRaised);

			checkBox.IsChecked = true;

			Assert.AreEqual(1, dc.ViewModel.CheckedRaised);
			Assert.AreEqual(0, dc.ViewModel.UncheckedRaised);

			checkBox.IsChecked = false;

			Assert.AreEqual(1, dc.ViewModel.CheckedRaised);
			Assert.AreEqual(1, dc.ViewModel.UncheckedRaised);

			var checkBox2 = SUT.FindName("myCheckBox2") as CheckBox;

			Assert.AreEqual(1, dc.ViewModel.CheckedRaised);
			Assert.AreEqual(1, dc.ViewModel.UncheckedRaised);

			checkBox2.IsChecked = true;

			Assert.AreEqual(2, dc.ViewModel.CheckedRaised);
			Assert.AreEqual(1, dc.ViewModel.UncheckedRaised);

			checkBox2.IsChecked = false;

			Assert.AreEqual(2, dc.ViewModel.CheckedRaised);
			Assert.AreEqual(2, dc.ViewModel.UncheckedRaised);
		}

		[TestMethod]
		public void When_xLoad()
		{
			var SUT = new Binding_xLoad();

			SUT.ForceLoaded();

			Assert.IsNull(SUT.topLevelContent);
			Assert.IsNull(SUT.innerTextBlock);

			SUT.TopLevelVisiblity = true;

			Assert.IsNotNull(SUT.topLevelContent);
			Assert.IsNotNull(SUT.innerTextBlock);
			Assert.AreEqual("My inner text", SUT.innerTextBlock.Text);

			var topLevelContent = SUT.FindName("topLevelContent") as FrameworkElement;
			Assert.AreEqual(Visibility.Visible, topLevelContent.Visibility);

			SUT.InnerText = "Updated !";

			Assert.AreEqual("Updated !", SUT.innerTextBlock.Text);

			SUT.TopLevelVisiblity = false;
			Assert.AreEqual(Visibility.Collapsed, topLevelContent.Visibility);
		}

		[TestMethod]
		public void When_xLoad_DataTemplate()
		{
			var SUT = new Binding_xLoad_DataTemplate();

			SUT.ForceLoaded();

			var data = new Binding_xLoad_DataTemplate_Data()
			{
				InnerText = "Salsepareille"
			};

			SUT.root.Content = data;

			var innerRoot = SUT.FindName("innerRoot") as Grid;
			Assert.IsNotNull(innerRoot);

			Assert.AreEqual(1, innerRoot.EnumerateAllChildren().OfType<ElementStub>().Count());

			data.TopLevelVisiblity = true;

			Assert.AreEqual(0, innerRoot.EnumerateAllChildren().OfType<ElementStub>().Count());

			var innerTextBlock = SUT.FindName("innerTextBlock") as TextBlock;
			Assert.IsNotNull(innerTextBlock);
			Assert.AreEqual(data.InnerText, innerTextBlock.Text);

			data.TopLevelVisiblity = false;

			var topLevelContent = SUT.FindName("topLevelContent") as FrameworkElement;
			Assert.AreEqual(Visibility.Collapsed, topLevelContent.Visibility);
		}

		[TestMethod]
		public void When_xLoad_Event()
		{
			var SUT = new Binding_xLoad_Event();

			SUT.ForceLoaded();

			Assert.IsNull(SUT.myCheckBox);
			Assert.IsNull(SUT.rootGrid);

			SUT.TopLevelVisiblity = true;

			Assert.IsNotNull(SUT.myCheckBox);
			Assert.IsNotNull(SUT.rootGrid);

			var checkBox = SUT.FindName("myCheckBox") as CheckBox;

			Assert.AreEqual(0, SUT.ViewModel.CheckedRaised);
			Assert.AreEqual(0, SUT.ViewModel.UncheckedRaised);

			checkBox.IsChecked = true;

			Assert.AreEqual(1, SUT.ViewModel.CheckedRaised);
			Assert.AreEqual(0, SUT.ViewModel.UncheckedRaised);

			checkBox.IsChecked = false;

			Assert.AreEqual(1, SUT.ViewModel.CheckedRaised);
			Assert.AreEqual(1, SUT.ViewModel.UncheckedRaised);

			SUT.TopLevelVisiblity = false;

			// After reload
			SUT.TopLevelVisiblity = true;

			Assert.IsNotNull(SUT.myCheckBox);
			Assert.IsNotNull(SUT.rootGrid);

			var checkBox2 = SUT.FindName("myCheckBox") as CheckBox;

			Assert.AreNotEqual(checkBox, checkBox2);

			Assert.AreEqual(1, SUT.ViewModel.CheckedRaised);
			Assert.AreEqual(1, SUT.ViewModel.UncheckedRaised);

			checkBox2.IsChecked = true;

			Assert.AreEqual(2, SUT.ViewModel.CheckedRaised);
			Assert.AreEqual(1, SUT.ViewModel.UncheckedRaised);

			checkBox2.IsChecked = false;

			Assert.AreEqual(2, SUT.ViewModel.CheckedRaised);
			Assert.AreEqual(2, SUT.ViewModel.UncheckedRaised);
		}

		[TestMethod]
		public void When_xLoad_FallbackValue()
		{
			var SUT = new Binding_xLoad_FallbackValue();

			SUT.ForceLoaded();

			Assert.AreEqual(Visibility.Collapsed, SUT.topLevelContent.Visibility);

			SUT.Model = new Binding_xLoad_FallbackValue_Model();

			Assert.AreEqual(Visibility.Collapsed, SUT.topLevelContent.Visibility);

			SUT.Model.Visible = true;

			Assert.AreEqual(Visibility.Visible, SUT.topLevelContent.Visibility);
		}

		[TestMethod]
		public async Task When_xLoad_FallbackValue_Converter()
		{
			var SUT = new Binding_xLoad_FallbackValue_Converter();

			SUT.ForceLoaded();

			Assert.IsNull(SUT.topLevelContent);
			Assert.IsNull(SUT.innerTextBlock);

			SUT.Model = new Binding_xLoad_FallbackValue_Model();

			Assert.IsNotNull(SUT.topLevelContent);
			Assert.IsNotNull(SUT.innerTextBlock);

			SUT.Model = null;

			await AssertIsNullAsync(() => SUT.topLevelContent);
			await AssertIsNullAsync(() => SUT.innerTextBlock);
		}

		[TestMethod]
		public void When_PropertyChanged_Empty()
		{
			var SUT = new Binding_PropertyChangedAll();

			SUT.ForceLoaded();

			Assert.AreEqual(SUT.Model.Value.ToString(), SUT.ValueView.Text);
			Assert.AreEqual(SUT.Model.Text, SUT.TextView.Text);

			SUT.Model.Value = 42;
			SUT.Model.Text = "World";

			SUT.Model.RaisePropertyChanged(string.Empty);

			Assert.AreEqual(SUT.Model.Value.ToString(), SUT.ValueView.Text);
			Assert.AreEqual(SUT.Model.Text, SUT.TextView.Text);
		}

		[TestMethod]
		public void When_PropertyChanged_Null()
		{
			var SUT = new Binding_PropertyChangedAll();

			SUT.ForceLoaded();

			Assert.AreEqual(SUT.Model.Value.ToString(), SUT.ValueView.Text);
			Assert.AreEqual(SUT.Model.Text, SUT.TextView.Text);

			SUT.Model.Value = 42;
			SUT.Model.Text = "World";

			SUT.Model.RaisePropertyChanged(null);

			Assert.AreEqual(SUT.Model.Value.ToString(), SUT.ValueView.Text);
			Assert.AreEqual(SUT.Model.Text, SUT.TextView.Text);
		}

		[TestMethod]
		public async Task When_xLoad_StaticResource()
		{
			var SUT = new Binding_xLoad_StaticResources();

			SUT.ForceLoaded();

			Assert.IsNull(SUT.TestGrid);
			SUT.IsTestGridLoaded = true;

			Assert.IsNotNull(SUT.TestGrid);
			Assert.IsNotNull(SUT.contentControl);
			Assert.IsNotNull(SUT.contentControl.ContentTemplate);

			SUT.IsTestGridLoaded = false;

			await AssertIsNullAsync(() => SUT.TestGrid);
			await AssertIsNullAsync(() => SUT.contentControl);

			SUT.IsTestGridLoaded = true;

			Assert.IsNotNull(SUT.TestGrid);
			Assert.IsNotNull(SUT.contentControl);
			Assert.IsNotNull(SUT.contentControl.ContentTemplate);
		}

		[TestMethod]
		public async Task When_xLoad_Setter()
		{
			var SUT = new Binding_xLoad_Setter();

			SUT.ForceLoaded();

			Assert.IsNull(SUT.ellipse);
			Assert.IsNotNull(SUT.square);
			Assert.AreEqual(4, SUT.square.StrokeThickness);

			SUT.IsEllipseLoaded = true;

			Assert.IsNotNull(SUT.ellipse);
			await AssertIsNullAsync(() => SUT.square);
			Assert.AreEqual(4, SUT.ellipse.StrokeThickness);

			SUT.IsEllipseLoaded = false;

			await AssertIsNullAsync(() => SUT.ellipse);
			Assert.IsNotNull(SUT.square);
			Assert.AreEqual(4, SUT.square.StrokeThickness);

			SUT.IsEllipseLoaded = true;

			Assert.IsNotNull(SUT.ellipse);
			await AssertIsNullAsync(() => SUT.square);
			Assert.AreEqual(4, SUT.ellipse.StrokeThickness);
		}

		[TestMethod]
		[Ignore("https://github.com/unoplatform/uno/issues/5836")]
		public async Task When_xLoad_Setter_Order()
		{
			var SUT = new Binding_xLoad_Setter_Order();

			SUT.ForceLoaded();

			Assert.IsNull(SUT.ellipse);
			Assert.IsNotNull(SUT.square);
			Assert.AreEqual(4, SUT.square.StrokeThickness);

			SUT.IsEllipseLoaded = true;

			Assert.IsNotNull(SUT.ellipse);
			await AssertIsNullAsync(() => SUT.square);
			Assert.AreEqual(4, SUT.ellipse.StrokeThickness);

			SUT.IsEllipseLoaded = false;

			await AssertIsNullAsync(() => SUT.ellipse);
			Assert.IsNotNull(SUT.square);
			Assert.AreEqual(4, SUT.square.StrokeThickness);

			SUT.IsEllipseLoaded = true;

			Assert.IsNotNull(SUT.ellipse);
			await AssertIsNullAsync(() => SUT.square);
			Assert.AreEqual(4, SUT.ellipse.StrokeThickness);
		}

		[TestMethod]
		public void When_xLoad_xBind_xLoad_Initial()
		{
			var grid = new Grid();
			grid.ForceLoaded();

			var SUT = new When_xLoad_xBind_xLoad_Initial();
			grid.Children.Add(SUT);

			Assert.IsNotNull(SUT.tb01);
			Assert.AreEqual(1, SUT.tb01.Tag);

			SUT.Model.MyValue = 42;

			Assert.AreEqual(42, SUT.tb01.Tag);
		}

		[TestMethod]
		public async Task When_Binding_xLoad_Twice()
		{
			var SUT = new Binding_xLoad_Twice();
			Assert.IsNull(SUT.tb01);
			Assert.IsNull(SUT.tb02);

			Assert.AreEqual(0, SUT.TopLevelVisiblityGetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblitySetCount);

			var grid = new Grid();
			grid.ForceLoaded();
			grid.Children.Add(SUT);

			Assert.IsNull(SUT.tb01);
			Assert.IsNull(SUT.tb02);

			Assert.AreEqual(2, SUT.TopLevelVisiblityGetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblitySetCount);

			MakeVisible();

			Assert.AreEqual(4, SUT.TopLevelVisiblityGetCount);
			Assert.AreEqual(1, SUT.TopLevelVisiblitySetCount);

			await MakeInvisible();

			Assert.AreEqual(6, SUT.TopLevelVisiblityGetCount);
			Assert.AreEqual(2, SUT.TopLevelVisiblitySetCount);

			MakeVisible();

			Assert.AreEqual(8, SUT.TopLevelVisiblityGetCount);
			Assert.AreEqual(3, SUT.TopLevelVisiblitySetCount);

			await MakeInvisible();

			Assert.AreEqual(10, SUT.TopLevelVisiblityGetCount);
			Assert.AreEqual(4, SUT.TopLevelVisiblitySetCount);

			void MakeVisible()
			{
				SUT.TopLevelVisiblity = true;

				Assert.IsNotNull(SUT.tb01);
				Assert.IsNotNull(SUT.tb02);
			}

			async Task MakeInvisible()
			{
				SUT.TopLevelVisiblity = false;

				await AssertIsNullAsync(() => SUT.tb01);
				await AssertIsNullAsync(() => SUT.tb02);
			}
		}

		[TestMethod]
		public void When_Binding_xNull()
		{
			var SUT = new Binding_xNull();

			SUT.ForceLoaded();

			Assert.IsNotNull(SUT.tb01);
			Assert.AreEqual("Jan 1", SUT.tb01.Text);

			Assert.IsNotNull(SUT.tb02);
			Assert.AreEqual("MMM d <null>", SUT.tb02.Text);

			Assert.IsNotNull(SUT.tb03);
			Assert.AreEqual("MMM d <null>", SUT.tb03.Text);
		}

		[TestMethod]
		public void When_NullableRecordStruct()
		{
			var SUT = new xBind_NullableRecordStruct();

			SUT.ForceLoaded();

			Assert.AreEqual("", SUT.tb1.Text);

			SUT.MyProperty = new xBind_NullableRecordStruct.MyRecord("42");

			Assert.AreEqual("42", SUT.tb1.Text);
		}

		[TestMethod]
		public void When_TypeCast()
		{
			var SUT = new Binding_TypeCast();

			SUT.ForceLoaded();

			Assert.AreEqual("42", SUT.tb01.Text);
			Assert.AreEqual("42", SUT.tb02.Text);
			Assert.AreEqual("4242", SUT.tb03.Text);
			Assert.AreEqual(2, SUT.tb04.Tag);
		}

		[TestMethod]
		public void When_TypeCast_DataTemplate()
		{
			var SUT = new Binding_TypeCast_DataTemplate();

			var root = SUT.FindName("root") as FrameworkElement;
			var dc = new Binding_TypeCast_DataTemplate_Data();
			root.DataContext = dc;

			SUT.ForceLoaded();
			root.ForceLoaded();

			var tb01 = SUT.FindName("tb01") as TextBlock;
			var tb02 = SUT.FindName("tb02") as TextBlock;
			var tb03 = SUT.FindName("tb03") as TextBlock;
			var tb04 = SUT.FindName("tb04") as TextBlock;

			Assert.AreEqual("42", tb01.Text);
			Assert.AreEqual("42", tb02.Text);
			Assert.AreEqual("4242", tb03.Text);
			Assert.AreEqual(2, tb04.Tag);
		}

		[TestMethod]
		public void When_PathLessCasting()
		{
			var SUT = new xBind_PathLessCasting();

			SUT.ForceLoaded();
			const string CastResult = "ExplicitConversion_Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls.xBind_PathLessCasting";
			Assert.AreEqual(SUT, SUT.tb1.Tag);
			Assert.AreEqual(CastResult, SUT.tb2.Text);
			Assert.AreEqual($"{CastResult}-{CastResult}", SUT.tb3.Text);
		}

		[TestMethod]
		public void When_PathLessCasting_Template()
		{
			var SUT = new xBind_PathLessCasting_Template();

			SUT.ForceLoaded();

			var rootData = new xBind_PathLessCasting_Template_Model();
			SUT.root.Content = rootData;

			var myObject = SUT.FindName("tb1") as TextBlock;

			Assert.AreEqual(rootData, myObject.Tag);
		}

		[TestMethod]
		public void When_AttachedProperty()
		{
			var SUT = new xBind_AttachedProperty();

			SUT.ForceLoaded();

			Assert.AreEqual(42, SUT.tb1.Tag);
			Assert.AreEqual("TextBlockTag", SUT.tb3.Tag);
			Assert.AreEqual("Formatted TextBlockTag", SUT.tb4.Tag);
			Assert.AreEqual("Hello World!!!", SUT.tb5.Text);
		}

		[TestMethod]
		public void When_ValueType()
		{
			var SUT = new xBind_ValueType();
			var date1 = new DateTime(2022, 10, 01);

			SUT.VM.Model2 = new() { MyDateTime = date1 };

			SUT.ForceLoaded();

			Assert.AreEqual(date1, SUT.tb1.Tag);

			SUT.VM.Model2 = null;

			Assert.IsNull(SUT.tb1.Tag);
		}

		[TestMethod]
		public void When_Indexer()
		{
			var SUT = new XBind_Indexer();

			SUT.ForceLoaded();

			Assert.AreEqual("ListFirstItem", SUT.tbList.Text);
			Assert.AreEqual("DictionaryValue", SUT.tbDict.Text);

			SUT.VM.List = new ObservableCollection<string>() { "UpdatedList" };
			SUT.VM.Dict = new PropertySet() { ["Key"] = "UpdatedDic" };

			Assert.AreEqual("UpdatedList", SUT.tbList.Text);
			Assert.AreEqual("UpdatedDic", SUT.tbDict.Text);
		}

		[TestMethod]
		public void When_Indexer_Update_Collection()
		{
			var SUT = new XBind_Indexer();

			SUT.ForceLoaded();

			Assert.AreEqual("ListFirstItem", SUT.tbList.Text);
			Assert.AreEqual("DictionaryValue", SUT.tbDict.Text);

			SUT.VM.List[0] = "Updated1";
			SUT.VM.Dict["Key"] = "Updated2";

			Assert.AreEqual("Updated1", SUT.tbList.Text);
			Assert.AreEqual("DictionaryValue", SUT.tbDict.Text);
		}

		[TestMethod]
		public void When_Indexer_Then_Property_Access()
		{
			var SUT = new XBind_Indexer();

			SUT.ForceLoaded();

			Assert.AreEqual("ListFirstItem", SUT.tbList2.Text);
			Assert.AreEqual("DictionaryValue", SUT.tbDict2.Text);

			SUT.VM.List2 = new ObservableCollection<PersonViewModel>() { new PersonViewModel() { Name = "UpdatedList" } };
			SUT.VM.Dict2 = new MyCustomMap() { ["Key"] = new PersonViewModel() { Name = "UpdatedDic" } };

			Assert.AreEqual("UpdatedList", SUT.tbList2.Text);
			Assert.AreEqual("UpdatedDic", SUT.tbDict2.Text);
		}

		[TestMethod]
		public void When_Indexer_Then_Property_Access_Update_Collection()
		{
			var SUT = new XBind_Indexer();

			SUT.ForceLoaded();

			Assert.AreEqual("ListFirstItem", SUT.tbList2.Text);
			Assert.AreEqual("DictionaryValue", SUT.tbDict2.Text);

			SUT.VM.List2[0] = new PersonViewModel() { Name = "Updated1" };
			SUT.VM.Dict2["Key"] = new PersonViewModel() { Name = "Updated2" };

			Assert.AreEqual("Updated1", SUT.tbList2.Text);
			Assert.AreEqual("DictionaryValue", SUT.tbDict2.Text);
		}

		[TestMethod]
		public void When_Indexer_Then_Property_Access_Update_Collection_Element()
		{
			var SUT = new XBind_Indexer();

			SUT.ForceLoaded();

			Assert.AreEqual("ListFirstItem", SUT.tbList2.Text);
			Assert.AreEqual("DictionaryValue", SUT.tbDict2.Text);

			SUT.VM.List2[0].Name = "Updated1";
			SUT.VM.Dict2["Key"].Name = "Updated2";

			Assert.AreEqual("Updated1", SUT.tbList2.Text);
			Assert.AreEqual("Updated2", SUT.tbDict2.Text);
		}

		[TestMethod]
		public void When_XBind_In_ResourceDictionary()
		{
			var SUT = new XBind_ResourceDictionary_Control();
			SUT.ForceLoaded();

			Assert.IsTrue(SUT.ElementLoadedInvoked);
		}

		private async Task AssertIsNullAsync<T>(Func<T> getter, TimeSpan? timeout = null) where T : class
		{
			timeout ??= TimeSpan.FromSeconds(1);

			var sw = Stopwatch.StartNew();

			while (sw.Elapsed < timeout)
			{
				void validate()
				{
					var value = getter();

					if (value == null)
					{
						return;
					}

					value = null;
				}

				validate();

				await Task.Yield();

				// Wait for the ElementNameSubject and ComponentHolder
				// instances to release their references.
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
			}

			{
				var value2 = getter();
				Assert.IsNull(value2);
				value2 = null;
			}
		}

		private async Task AssertIsNoNullAsync<T>(Func<T> getter, TimeSpan? timeout)
		{
			timeout ??= TimeSpan.FromSeconds(1);

			var sw = Stopwatch.StartNew();

			while (sw.Elapsed < timeout && getter() == null)
			{
				await Task.Delay(100);

				// Wait for the ElementNameSubject and ComponentHolder
				// instances to release their references.
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			Assert.IsNotNull(getter());
		}
	}
}
