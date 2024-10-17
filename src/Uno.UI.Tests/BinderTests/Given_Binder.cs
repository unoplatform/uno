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
using Windows.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Controls;
using System.Threading;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_Binder
	{
		[TestMethod]
		public void When_Inherited_data_Context()
		{
			var SUT = new Target1();
			var child = new Target2(SUT);

			child.SetBinding(Target2.DataContextProperty, new Binding("Child"));
			child.SetBinding("TargetValue", new Binding("Value"));
			SUT.ChildrenBinders.Add(child);

			var source = new MySource();
			SUT.DataContext = source;

			Assert.AreEqual(42, child.TargetValue);
			Assert.AreEqual(1, child.TargetValueSetCount);

			source.Child = new MySource2(43);
			Assert.AreEqual(43, child.TargetValue);
			Assert.AreEqual(2, child.TargetValueSetCount);
		}

		[TestMethod]
		public void When_Inherited_data_Context_And_Converter()
		{
			var SUT = new Target1();

			var child = new Target2(SUT);

			child.SetBinding(Target2.DataContextProperty, new Binding("Child"));
			child.SetBinding("TargetValue", new Binding("Value", converter: new OppositeConverter()));
			SUT.ChildrenBinders.Add(child);

			var source = new MySource();
			SUT.DataContext = source;

			Assert.AreEqual(-42, child.TargetValue);
			Assert.AreEqual(1, child.TargetValueSetCount);

			source.Child = new MySource2(43);
			Assert.AreEqual(-43, child.TargetValue);
			Assert.AreEqual(2, child.TargetValueSetCount);
		}

		[TestMethod]
		public void When_Inherited_data_Context_Sequence_And_Converter()
		{
			var SUT = new Target1();

			var child = new Target2(SUT);

			var converter = new OppositeConverter();

			child.SetBinding(Target2.DataContextProperty, new Binding("Item.List[0]"));
			child.SetBinding("TargetValue", new Binding("Details.Info", converter: converter) { FallbackValue = 10 });
			SUT.ChildrenBinders.Add(child);

			SUT.DataContext = new SourceLevel0();
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);

			SUT.DataContext = null;
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);

			SUT.DataContext = new SourceLevel0();
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);

			SUT.DataContext = null;
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);

			SUT.DataContext = new SourceLevel0() { Item = new SourceLevel1() { List = new SourceLevel2[] { new SourceLevel2() } } };
			Assert.AreEqual(-1000, child.TargetValue);
			Assert.AreEqual(1, converter.ConversionCount);
			Assert.AreEqual(1000, converter.LastValue);

			// It breaks here, when a broken binding would replace a fully functional one.
			SUT.DataContext = new SourceLevel0();
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(1, converter.ConversionCount);
			Assert.AreEqual(1000, converter.LastValue);

			SUT.DataContext = new SourceLevel0() { Item = new SourceLevel1() { List = new SourceLevel2[] { new SourceLevel2() } } };
			Assert.AreEqual(-1000, child.TargetValue);
			Assert.AreEqual(2, converter.ConversionCount);
			Assert.AreEqual(1000, converter.LastValue);

			SUT.DataContext = null;
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(2, converter.ConversionCount);
			Assert.AreEqual(1000, converter.LastValue);

			SUT.DataContext = new SourceLevel0() { Item = new SourceLevel1() { List = new SourceLevel2[] { new SourceLevel2() } } };
			Assert.AreEqual(-1000, child.TargetValue);
			Assert.AreEqual(3, converter.ConversionCount);
			Assert.AreEqual(1000, converter.LastValue);
		}

		[TestMethod]
		public void When_Inherited_data_Context_Sequence_And_Converter_DifferentTypes()
		{
			var SUT = new Target1();

			var child = new Target2(SUT);

			var converter = new BoolToNumber();

			child.SetBinding(Target2.DataContextProperty, new Binding("Item.List[0]"));
			child.SetBinding("TargetValue", new Binding("Details.InfoBoolean", converter: converter) { FallbackValue = 10 });
			SUT.ChildrenBinders.Add(child);

			SUT.DataContext = new SourceLevel0();
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);

			SUT.DataContext = null;
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);

			SUT.DataContext = new SourceLevel0();
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);

			SUT.DataContext = null;
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);

			SUT.DataContext = new SourceLevel0() { Item = new SourceLevel1() { List = new SourceLevel2[] { new SourceLevel2() } } };
			Assert.AreEqual(-1000, child.TargetValue);
			Assert.AreEqual(1, converter.ConversionCount);
			Assert.AreEqual(true, converter.LastValue);

			// It breaks here, when a broken binding would replace a fully functional one.
			SUT.DataContext = new SourceLevel0();
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(1, converter.ConversionCount);
			Assert.AreEqual(true, converter.LastValue);

			SUT.DataContext = new SourceLevel0() { Item = new SourceLevel1() { List = new SourceLevel2[] { new SourceLevel2() } } };
			Assert.AreEqual(-1000, child.TargetValue);
			Assert.AreEqual(2, converter.ConversionCount);
			Assert.AreEqual(true, converter.LastValue);

			SUT.DataContext = null;
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(2, converter.ConversionCount);
			Assert.AreEqual(true, converter.LastValue);

			SUT.DataContext = new SourceLevel0() { Item = new SourceLevel1() { List = new SourceLevel2[] { new SourceLevel2() } } };
			Assert.AreEqual(-1000, child.TargetValue);
			Assert.AreEqual(3, converter.ConversionCount);
			Assert.AreEqual(true, converter.LastValue);
		}

		[TestMethod]
		public void When_Inherited_data_Context_And_Converter_Different_Types_Invalid_Parent_Path()
		{
			var SUT = new Target1();

			var child = new Target2(SUT);

			var converter = new BoolToNumber();

			child.SetBinding("DataContext", new Binding("Item_zzz.List[0]"));
			child.SetBinding("TargetValue", new Binding("Details.InfoBoolean", converter: converter) { FallbackValue = 10 });
			SUT.ChildrenBinders.Add(child);

			// With the invalid path (Item_zzz instead of Item), the converter should be be called at all.
			// FallbackValue should be used
			SUT.DataContext = new SourceLevel0() { Item = new SourceLevel1() { List = new SourceLevel2[] { new SourceLevel2() } } };
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);
		}

		[TestMethod]
		public void When_Inherited_data_Context_And_Converter_Different_Types_Invalid_Child_Path()
		{
			var SUT = new Target1();

			var child = new Target2(SUT);

			var converter = new BoolToNumber();

			child.SetBinding("DataContext", new Binding("Item.List[0]"));
			child.SetBinding("TargetValue", new Binding("Details_zzz.InfoBoolean", converter: converter) { FallbackValue = 10 });
			SUT.ChildrenBinders.Add(child);

			// With the invalid path (Details_zzz instead of Details), the converter should not be called at all.
			// FallbackValue should be used
			SUT.DataContext = new SourceLevel0() { Item = new SourceLevel1() { List = new SourceLevel2[] { new SourceLevel2() } } };
			Assert.AreEqual(10, child.TargetValue);
			Assert.AreEqual(0, converter.ConversionCount);
			Assert.AreEqual(null, converter.LastValue);
		}

		[TestMethod]
		public void When_Inherited_data_Context_and_set_Null()
		{
			var SUT = new Target1();

			var child = new Target2(SUT);

			child.SetBinding(Target2.DataContextProperty, new Binding("Child"));
			child.SetBinding("TargetValue", new Binding("Value"));
			SUT.ChildrenBinders.Add(child);

			var source = new MySource();
			SUT.DataContext = source;

			Assert.AreEqual(42, child.TargetValue);
			Assert.AreEqual(1, child.TargetValueSetCount);
			Assert.AreEqual(1, SUT.DataContextChangedCount);
			Assert.AreEqual(1, child.DataContextChangedCount);

			source.Child = null;
			Assert.AreEqual(0, child.TargetValue);
			Assert.AreEqual(2, child.TargetValueSetCount);
			Assert.AreEqual(1, SUT.DataContextChangedCount);
			Assert.AreEqual(2, child.DataContextChangedCount);

			source.Child = new MySource2(44);
			Assert.AreEqual(44, child.TargetValue);
			Assert.AreEqual(3, child.TargetValueSetCount);
			Assert.AreEqual(1, SUT.DataContextChangedCount);
			Assert.AreEqual(3, child.DataContextChangedCount);
		}

		[TestMethod]
		public void When_Inherited_data_Context_and_Changed()
		{
			var SUT = new Target1();

			var child = new Target2(SUT);

			child.SetBinding(Target2.DataContextProperty, new Binding("Child"));
			child.SetBinding("TargetValue", new Binding("Value"));
			SUT.ChildrenBinders.Add(child);

			var source = new MySource();
			SUT.DataContext = source;

			Assert.AreEqual(42, child.TargetValue);
			Assert.AreEqual(1, child.TargetValueSetCount);
			Assert.AreEqual(1, SUT.DataContextChangedCount);
			Assert.AreEqual(1, child.DataContextChangedCount);

			source.Child = new MySource2(44);
			Assert.AreEqual(44, child.TargetValue);
			Assert.AreEqual(2, child.TargetValueSetCount);
			Assert.AreEqual(1, SUT.DataContextChangedCount);
			Assert.AreEqual(2, child.DataContextChangedCount);
		}

		[TestMethod]
		public void When_DataContext_Changed_With_TwoWay_Binding()
		{
			// Tests both Value and Reference types

			var targetA = new Target2() { TargetValue = 10, Brush = SolidColorBrushHelper.Tomato };
			var targetB = new Target2() { TargetValue = 20, Brush = SolidColorBrushHelper.Olive };

			var control = new MyControl();

			control.SetBinding("MyProperty", new Binding { Path = "TargetValue", Mode = BindingMode.TwoWay });
			control.SetBinding("MyBrushProperty", new Binding { Path = "Brush", Mode = BindingMode.TwoWay });

			control.DataContext = targetA;
			Assert.AreEqual(10, targetA.TargetValue);
			Assert.AreEqual(SolidColorBrushHelper.Tomato.Color, (targetA.Brush as SolidColorBrush).Color);

			// This test used to fail because changing the DataContext would dispose the previous PropertyChanged subscription,
			// which would clear the value and RaisePropertyChanged with null,
			// which would propagate that value up to the new DataContext (with TwoWay binding enabled).
			control.DataContext = targetB;
			Assert.AreEqual(20, targetB.TargetValue);
			Assert.AreEqual(SolidColorBrushHelper.Olive.Color, (targetB.Brush as SolidColorBrush).Color);

			// Making sure tat setting DataContext to null doesn't affect previous DataContexts
			control.DataContext = null;
			Assert.AreEqual(10, targetA.TargetValue);
			Assert.AreEqual(20, targetB.TargetValue);
			Assert.AreEqual(SolidColorBrushHelper.Tomato.Color, (targetA.Brush as SolidColorBrush).Color);
			Assert.AreEqual(SolidColorBrushHelper.Olive.Color, (targetB.Brush as SolidColorBrush).Color);

			// Making sure that TargetValue is only set once (object initializer)
			// There used to be issues where changing the DataContext would cause TwoWay binding to propagate invalid changes to the source.
			Assert.AreEqual(1, targetA.TargetValueSetCount);
			Assert.AreEqual(1, targetA.TargetValueSetCount);
			Assert.AreEqual(1, targetA.BrushSetCount);
			Assert.AreEqual(1, targetA.BrushSetCount);
		}

		[TestMethod]
		public void When_Direct_data_Context_binding()
		{
			var SUT = new Target1();

			var child = new Target2();

			// Bypass the inheritance
			child.SetBinding(Target1.DataContextProperty, new Binding("Child"));

			child.SetBinding(Target2.TargetValueProperty, new Binding("Value"));

			SUT.ChildrenBinders.Add(child);

			var source = new MySource();
			SUT.DataContext = source;

			Assert.AreEqual(42, child.TargetValue);
			Assert.AreEqual(1, child.TargetValueSetCount);
		}

		[TestMethod]
		public void When_Object_AttachedProperty_binding()
		{
			var SUT = new Target1();

			SUT.SetBinding(Attachable.MyValueProperty, new Binding("TargetValue"));

			var source = new Target2();
			source.TargetValue = 42;
			SUT.DataContext = source;

			Assert.AreEqual(42, Attachable.GetMyValue(SUT));

			Attachable.SetMyValue(SUT, 43);

			Assert.AreEqual(42, source.TargetValue);
		}

		[TestMethod]
		public void When_DependencyProperty_OneWay_binding()
		{
			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyPropertyProperty, new Binding("TargetValue"));

			var source = new Target2();
			source.TargetValue = 42;
			SUT.DataContext = source;

			Assert.AreEqual(42, SUT.MyProperty);

			SUT.MyProperty = 43;
			Assert.AreEqual(42, source.TargetValue);
		}

		[TestMethod]
		public void When_DependencyProperty_TWoWay_binding()
		{
			MyControl.MyPropertyProperty.ToString();

			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyPropertyProperty, new Binding("TargetValue") { Mode = BindingMode.TwoWay });

			var source = new Target2();
			source.TargetValue = 42;
			SUT.DataContext = source;

			Assert.AreEqual(42, SUT.MyProperty);

			SUT.MyProperty = 43;
			Assert.AreEqual(43, source.TargetValue);
		}

		[TestMethod]
		public void When_DependencyProperty_TargetNullValue_binding()
		{
			var tomato = SolidColorBrushHelper.Tomato;

			MyControl.MyPropertyProperty.ToString();

			var SUT = new MyControl();
			SUT.SetBinding(MyControl.MyBrushPropertyProperty, new Binding("Brush") { TargetNullValue = tomato });

			var source = new Target2();
			SUT.DataContext = source;

			source.Brush = null;
			Assert.AreEqual(tomato, SUT.MyBrushProperty);

			source.Brush = SolidColorBrushHelper.Olive;
			Assert.AreEqual(SolidColorBrushHelper.Olive.Color, (SUT.MyBrushProperty as SolidColorBrush)?.Color);

			source.Brush = null;
			Assert.AreEqual(tomato, SUT.MyBrushProperty);
		}

		[TestMethod]
		public void When_DependencyProperty_TargetNullValue_BrushStringConversion_binding()
		{
			MyControl.MyPropertyProperty.ToString();

			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyBrushPropertyProperty, new Binding("Brush") { TargetNullValue = "Tomato" });

			var source = new Target2();
			SUT.DataContext = source;

			source.Brush = null;
			Assert.AreEqual(SolidColorBrushHelper.Tomato.Color, (SUT.MyBrushProperty as SolidColorBrush).Color);

			source.Brush = SolidColorBrushHelper.Olive;
			Assert.AreEqual(SolidColorBrushHelper.Olive.Color, (SUT.MyBrushProperty as SolidColorBrush).Color);

			source.Brush = null;
			Assert.AreEqual(SolidColorBrushHelper.Tomato.Color, (SUT.MyBrushProperty as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_DependencyProperty_TargetNullValue_VisibilityStringConversion_binding()
		{
			MyControl.MyPropertyProperty.ToString();

			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyVisibilityPropertyProperty, new Binding("Object") { TargetNullValue = "Collapsed" });

			var source = new Target2();
			SUT.DataContext = source;

			source.Object = null;
			Assert.AreEqual(Visibility.Collapsed, SUT.MyVisibilityProperty);

			source.Object = Visibility.Visible;
			Assert.AreEqual(Visibility.Visible, SUT.MyVisibilityProperty);

			source.Object = null;
			Assert.AreEqual(Visibility.Collapsed, SUT.MyVisibilityProperty);
		}

		[TestMethod]
		public void When_DependencyProperty_FallbackValue_binding()
		{
			MyControl.MyPropertyProperty.ToString();

			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyPropertyProperty, new Binding("TargetValue") { FallbackValue = 42 });

			Assert.AreEqual(42, SUT.MyProperty);

			var source = new Target2();
			source.TargetValue = 10;
			SUT.DataContext = source;

			Assert.AreEqual(10, SUT.MyProperty);

			SUT.DataContext = null;

			Assert.AreEqual(42, SUT.MyProperty);

			SUT.DataContext = source;

			Assert.AreEqual(10, SUT.MyProperty);
		}

		[TestMethod]
		public void When_DependencyProperty_FallbackValue_EmptyBindingPath_binding()
		{
			MyControl.MyPropertyProperty.ToString();

			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyPropertyProperty, new Binding() { FallbackValue = 42 });

			Assert.AreEqual(42, SUT.MyProperty);

			SUT.DataContext = 10;

			Assert.AreEqual(10, SUT.MyProperty);

			SUT.DataContext = null;

			Assert.AreEqual(42, SUT.MyProperty);
		}

		[TestMethod]
		public void When_DependencyProperty_FallbackValue_Array_IndexOutOfBounds_binding()
		{
			MyControl.MyPropertyProperty.ToString();

			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyPropertyProperty, new Binding("[0]") { FallbackValue = 42 });

			Assert.AreEqual(42, SUT.MyProperty);

			SUT.DataContext = new int[1] { 43 };

			Assert.AreEqual(43, SUT.MyProperty);

			SUT.DataContext = new int[0];

			Assert.AreEqual(42, SUT.MyProperty);
		}

		[TestMethod]
		public void When_DependencyProperty_FallbackValue_Dictionary_KeyNotFoundException_binding()
		{
			MyControl.MyPropertyProperty.ToString();

			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyPropertyProperty, new Binding("[key]") { FallbackValue = 42 });

			Assert.AreEqual(42, SUT.MyProperty);

			SUT.DataContext = new Dictionary<string, int> { { "key", 43 } };

			Assert.AreEqual(43, SUT.MyProperty);

			SUT.DataContext = new Dictionary<string, int>();

			Assert.AreEqual(42, SUT.MyProperty);
		}

		[TestMethod]
		public void When_DependencyProperty_FallbackValue_VisibilityStringConversion_binding()
		{
			MyControl.MyPropertyProperty.ToString();

			var SUT = new MyControl();
			var source = new Target2();

			Assert.AreEqual(Visibility.Visible, SUT.MyVisibilityProperty);

			SUT.SetBinding(MyControl.MyVisibilityPropertyProperty, new Binding("Object") { FallbackValue = "Collapsed" });
			Assert.AreEqual(Visibility.Collapsed, SUT.MyVisibilityProperty);

			SUT.DataContext = source;
			Assert.AreEqual(Visibility.Visible, SUT.MyVisibilityProperty);

			source.Object = 10;
			Assert.AreEqual(Visibility.Collapsed, SUT.MyVisibilityProperty);

			source.Object = Visibility.Visible;
			Assert.AreEqual(Visibility.Visible, SUT.MyVisibilityProperty);

			SUT.DataContext = null;
			Assert.AreEqual(Visibility.Collapsed, SUT.MyVisibilityProperty);
		}

		[TestMethod]
		public void When_DependencyProperty_Object_AttachedProperty()
		{
			var SUT = new MyObjectTest();
			SUT.MyProperty = 42;
			Assert.AreEqual(42, SUT.MyProperty);

			var props = MyObjectTest.MyPropertyProperty.GetMetadata(typeof(MyObjectTest));
		}

		[TestMethod]
		public void When_PrivateProperty_And_XBind()
		{
			var source = new PrivateProperty(42);
			var SUT = new Windows.UI.Xaml.Controls.Grid();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Path = "MyProperty",
					CompiledSource = source
				}
			);

			SUT.ApplyXBind();

			Assert.AreEqual(42, SUT.Tag);
		}

		[TestMethod]
		public void When_PrivateProperty_And_Binding()
		{
			var source = new PrivateProperty(42);
			var SUT = new Windows.UI.Xaml.Controls.Grid();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Path = "MyProperty"
				}
			);

			SUT.DataContext = source;

			Assert.IsNull(SUT.Tag);
		}

		[TestMethod]
		public void When_Public_Field_And_xBind()
		{
			var source = new PublicField(42);
			var SUT = new Windows.UI.Xaml.Controls.Grid();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Path = "MyField",
					CompiledSource = source
				}
			);

			SUT.ApplyXBind();

			Assert.AreEqual(42, SUT.Tag);
		}

		[TestMethod]
		public void When_Private_Field_And_xBind()
		{
			var source = new PrivateField(42);
			var SUT = new Windows.UI.Xaml.Controls.Grid();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Path = "MyField",
					CompiledSource = source
				}
			);

			SUT.ApplyXBind();

			Assert.AreEqual(42, SUT.Tag);
		}

		[TestMethod]
		public void When_Private_Field_And_xBind_Not_OneTime()
		{
			var source = new PrivateField(42);
			var SUT = new Windows.UI.Xaml.Controls.Grid();

			var binding = new Binding()
			{
				Path = "MyField",
				Mode = BindingMode.OneWay,
				CompiledSource = source
			};
			binding.SetBindingXBindProvider(
					source,
					(a) => (true, "Test"),
					null,
					new[] { "MyField" });

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				binding
			);

			SUT.ApplyXBind();

			Assert.AreEqual("Test", SUT.Tag);
		}

		[TestMethod]
		public void When_Public_Field_And_Binding()
		{
			var source = new PublicField(42);
			var SUT = new Windows.UI.Xaml.Controls.Grid();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Path = "MyField"
				}
			);

			SUT.DataContext = source;

			Assert.IsNull(SUT.Tag);
		}

		[TestMethod]
		public void When_Private_Field_And_Binding()
		{
			var source = new PrivateField(42);
			var SUT = new Windows.UI.Xaml.Controls.Grid();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Path = "MyField"
				}
			);

			SUT.DataContext = source;

			Assert.IsNull(SUT.Tag);
		}

		[TestMethod]
		public void When_Reentrant_Set()
		{
			var sut = new TextBox();

			sut.TextChanging += (o, e) =>
			{
				sut.Text = "Bob";
			};

			sut.Text = "Alice";

			Assert.AreEqual("Bob", sut.Text);
		}

		[TestMethod]
		public void When_Reentrant_Set_With_Additional_Set()
		{
			var sut = new TextBox();

			sut.TextChanging += (o, e) =>
			{
				sut.SetValue(Grid.RowProperty, 3);
				sut.Text = "Bob";
			};

			sut.Text = "Alice";

			Assert.AreEqual("Bob", sut.Text);
			var row = (int)sut.GetValue(Grid.RowProperty);
			Assert.AreEqual(3, row);
		}

		[TestMethod]
		public void When_Source_Complex()
		{
			var SUT = new Windows.UI.Xaml.Controls.Grid();
			var subject = new ElementNameSubject();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Path = "MyProperty",
					Source = new { MyProperty = 42 }
				}
			);

			Assert.AreEqual(42, SUT.Tag);
		}

		[TestMethod]
		public void When_Source_String()
		{
			var source = "Test";
			var SUT = new Windows.UI.Xaml.Controls.Grid();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Source = source
				}
			);

			Assert.AreEqual(source, SUT.Tag);
		}

		[TestMethod]
		public void When_Subject_Source_Complex()
		{
			var SUT = new Windows.UI.Xaml.Controls.Grid();
			var subject = new ElementNameSubject();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Path = "MyProperty",
					Source = subject
				}
			);

			Assert.IsNull(SUT.Tag);

			subject.ElementInstance = new { MyProperty = 42 };

			Assert.AreEqual(42, SUT.Tag);
		}

		[TestMethod]
		public void When_Subject_Source()
		{
			var SUT = new Windows.UI.Xaml.Controls.Grid();
			var subject = new ElementNameSubject();

			SUT.SetBinding(
				Windows.UI.Xaml.Controls.Grid.TagProperty,
				new Binding()
				{
					Source = subject
				}
			);

			Assert.IsNull(SUT.Tag);

			subject.ElementInstance = 42;

			Assert.AreEqual(42, SUT.Tag);
		}

		[TestMethod]
		public void When_ExplicitUpdateSourceTrigger()
		{
			var source = new MyBindingSource { IntValue = 42 };
			var target = new MyControl();
			target.SetBinding(MyControl.MyPropertyProperty, new Binding { Source = source, Path = nameof(MyBindingSource.IntValue), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit });

			Assert.AreEqual(42, source.IntValue);
			Assert.AreEqual(42, target.MyProperty);

			target.MyProperty = -42;

			Assert.AreEqual(42, source.IntValue);
			Assert.AreEqual(-42, target.MyProperty);
		}

		[TestMethod]
		public void When_ExplicitUpdateSourceTrigger_UpdateSource()
		{
			var source = new MyBindingSource { IntValue = 42 };
			var target = new MyControl();
			target.SetBinding(MyControl.MyPropertyProperty, new Binding { Source = source, Path = nameof(MyBindingSource.IntValue), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit });

			var sut = target.GetBindingExpression(MyControl.MyPropertyProperty);

			Assert.AreEqual(42, source.IntValue);
			Assert.AreEqual(42, target.MyProperty);

			target.MyProperty = -42;
			sut.UpdateSource();

			Assert.AreEqual(-42, source.IntValue);
			Assert.AreEqual(-42, target.MyProperty);
		}

		[TestMethod]
		public void When_ExplicitSetBindingBetweenProperties_IsNotFallBackValue()
		{
			var source = new MyBindingSource { IntValue = 42 };
			var target = new MyControl();
			var target2 = new MyObjectTest();

			target.SetBinding(MyControl.MyPropertyProperty, new Binding { Source = source, Path = nameof(MyBindingSource.IntValue), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit });
			target2.Binding(nameof(MyControl.MyProperty), nameof(MyControl.MyProperty), source: target, BindingMode.TwoWay);

			Assert.AreEqual(42, source.IntValue);
			Assert.AreEqual(42, target.MyProperty);
			Assert.AreEqual(42, target2.MyProperty);
		}

		[TestMethod]
		public void When_SelfRelativeSource()
		{
			var SUT = new SelfBindingTest();

			Assert.AreEqual(-1, SUT.Property1);
			Assert.AreEqual(-2, SUT.Property2);

			SUT.SetBinding(SelfBindingTest.Property2Property, new Binding { Path = "Property1", RelativeSource = new RelativeSource(RelativeSourceMode.Self) });

			Assert.AreEqual(-1, SUT.Property1);
			Assert.AreEqual(-1, SUT.Property2);

			SUT.Property1 = 42;
			Assert.AreEqual(42, SUT.Property2);
		}

		[TestMethod]
		public void When_Parent_Collected()
		{
			var SUT = new Grid();
			var parentChanged = 0;
			SUT.RegisterParentChangedCallback(null, (s, e, a) => parentChanged++);

			static WeakReference SetParent(Grid sut)
			{
				var parent = new Border();
				sut.SetParent(parent);
				return new WeakReference(parent);
			}

			var wr = SetParent(SUT);

			int count = 10;
			while (wr.IsAlive && (count-- > 0))
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				Thread.Sleep(100);
			}

			Assert.IsNull(wr.Target);

			Assert.AreEqual(1, parentChanged);

			SUT.SetParent(null);

			Assert.AreEqual(2, parentChanged);
		}

		[TestMethod]
		public void When_NonUI_DependencyObject_NonUISub_Content()
		{
			var SUT = new NonUIDependencyObject();
			SUT.SetBinding(NonUIDependencyObject.MyValueProperty, new Binding("MyModelValue"));
			var SUT2 = new NonUIDependencyObject();
			SUT2.SetBinding(NonUIDependencyObject.MyValueProperty, new Binding("MyModelValue"));
			var model = new NonUIDependencyObject_Model();

			Assert.AreEqual(-1, SUT2.MyValue);
			Assert.AreEqual(-1, SUT.MyValue);

			SUT.SubProperty = SUT2;

			Assert.AreEqual(-1, SUT2.MyValue);
			Assert.AreEqual(-1, SUT.MyValue);

			SUT.DataContext = model;

			Assert.AreEqual(0, SUT2.MyValue);
			Assert.AreEqual(0, SUT.MyValue);
		}

		[TestMethod]
		public void When_UI_DependencyObject_NonUISub_Content()
		{
			var SUT = new UIDependencyObject();
			SUT.SetBinding(UIDependencyObject.MyValueProperty, new Binding("MyModelValue"));
			var SUT2 = new NonUIDependencyObject();
			SUT2.SetBinding(NonUIDependencyObject.MyValueProperty, new Binding("MyModelValue"));
			var model = new NonUIDependencyObject_Model();

			Assert.AreEqual(-1, SUT2.MyValue);
			Assert.AreEqual(-1, SUT.MyValue);

			SUT.SubProperty = SUT2;

			Assert.AreEqual(-1, SUT2.MyValue);
			Assert.AreEqual(-1, SUT.MyValue);

			SUT.DataContext = model;

			Assert.AreEqual(0, SUT2.MyValue);
			Assert.AreEqual(0, SUT.MyValue);
		}

		[TestMethod]
		public void When_Binding_OneWay_Overwritten_By_Binding()
		{
			var previousDC = new MyBindingSource { IntValue = 45 };
			var replacementDC = new MyBindingSource { IntValue = 22 };
			var slider = new Slider();
			slider.SetBinding(Slider.ValueProperty, new Binding { Path = new PropertyPath(nameof(MyBindingSource.IntValue)), Mode = BindingMode.OneWay, Source = previousDC });

			Assert.AreEqual(45, slider.Value);

			slider.SetBinding(Slider.ValueProperty, new Binding { Path = new PropertyPath(nameof(MyBindingSource.IntValue)), Mode = BindingMode.OneWay, Source = replacementDC });

			Assert.AreEqual(22, slider.Value);

			previousDC.IntValue = 14;

			Assert.AreEqual(22, slider.Value);
		}

		[TestMethod]
		[Ignore("Activate this test when bindings are correctly cleared by setting local value")]
		public void When_Binding_OneWay_Overwritten_By_Local()
		{
			var dc = new MyBindingSource { IntValue = 45 };
			var slider = new Slider() { DataContext = dc };
			slider.SetBinding(Slider.ValueProperty, new Binding { Path = new PropertyPath(nameof(MyBindingSource.IntValue)), Mode = BindingMode.OneWay });

			Assert.AreEqual(45, slider.Value);

			slider.Value = 22; // Setting local value should clear one-way binding
			dc.IntValue = 14;

			Assert.AreEqual(22, slider.Value);
		}

		[TestMethod]
		[Ignore("Activate this test when bindings are correctly cleared by setting local value")]
		public void When_Binding_OneWay_Overwritten_By_Local_Explicit_Source()
		{
			var dc = new MyBindingSource { IntValue = 45 };
			var slider = new Slider();
			slider.SetBinding(Slider.ValueProperty, new Binding { Path = new PropertyPath(nameof(MyBindingSource.IntValue)), Mode = BindingMode.OneWay, Source = dc });

			Assert.AreEqual(45, slider.Value);

			slider.Value = 22; // Setting local value should clear one-way binding
			dc.IntValue = 14;

			Assert.AreEqual(22, slider.Value);
		}

		[TestMethod]
		public void When_Binding_And_ClearValue()
		{
			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyPropertyProperty, new Binding { Path = new PropertyPath(".") });

			SUT.DataContext = 42;

			Assert.AreEqual(42, SUT.MyProperty);

			SUT.ClearValue(MyControl.MyPropertyProperty);

			SUT.DataContext = 43;

			Assert.AreEqual(0, SUT.MyProperty);
		}

		[TestMethod]
		public void When_Binding_And_ClearValue_With_Events()
		{
			List<int> myPropertyHistory = new();
			var SUT = new MyControl();

			SUT.SetBinding(MyControl.MyPropertyProperty, new Binding { Path = new PropertyPath(".") });

			SUT.RegisterPropertyChangedCallback(
				MyControl.MyPropertyProperty,
				(s, e) =>
				{
					myPropertyHistory.Add(SUT.MyProperty);
				});

			SUT.DataContext = 42;

			Assert.AreEqual(42, SUT.MyProperty);
			Assert.AreEqual(1, myPropertyHistory.Count);
			Assert.AreEqual(42, myPropertyHistory[0]);

			SUT.ClearValue(MyControl.MyPropertyProperty);

			Assert.AreEqual(2, myPropertyHistory.Count);
			Assert.AreEqual(0, myPropertyHistory[1]);

			SUT.DataContext = 43;

			Assert.AreEqual(2, myPropertyHistory.Count);
			Assert.AreEqual(0, SUT.MyProperty);
		}

		public partial class BaseTarget : DependencyObject
		{
			private List<object> _dataContextChangedList = new List<object>();

			public BaseTarget(BaseTarget parent = null)
			{
				this.SetParent(parent);

				ChildrenBinders = new DependencyObjectCollection<DependencyObject>(this);
			}

			public int DataContextChangedCount { get; private set; }

			partial void OnDataContextChangedPartial(DependencyPropertyChangedEventArgs e)
			{
				_dataContextChangedList.Add(DataContext);
				DataContextChangedCount++;
			}

			#region ChildrenBinders DependencyProperty

			public IList<DependencyObject> ChildrenBinders
			{
				get => (IList<DependencyObject>)GetValue(ChildrenBindersProperty);
				set => SetValue(ChildrenBindersProperty, value);
			}

			// Using a DependencyProperty as the backing store for ChildrenBinders.  This enables animation, styling, binding, etc...
			public static readonly DependencyProperty ChildrenBindersProperty =
				DependencyProperty.Register(
					name: "ChildrenBinders",
					propertyType: typeof(IList<DependencyObject>),
					ownerType: typeof(BaseTarget),
					typeMetadata: new FrameworkPropertyMetadata(null, (s, e) => ((BaseTarget)s)?.OnChildrenBindersChanged(e))
				);


			private void OnChildrenBindersChanged(DependencyPropertyChangedEventArgs e)
			{
			}

			#endregion

		}

		public class Target1 : BaseTarget
		{

		}

		public class Target2 : BaseTarget
		{
			public Target2(BaseTarget parent = null)
				: base(parent)
			{

			}

			#region TargetValue DependencyProperty

			public int TargetValue
			{
				get { return (int)GetValue(TargetValueProperty); }
				set { SetValue(TargetValueProperty, value); }
			}

			// Using a DependencyProperty as the backing store for TargetValue.  This enables animation, styling, binding, etc...
			public static readonly DependencyProperty TargetValueProperty =
				DependencyProperty.Register("TargetValue", typeof(int), typeof(Target2), new FrameworkPropertyMetadata(0, (s, e) => ((Target2)s)?.OnTargetValueChanged(e)));


			private void OnTargetValueChanged(DependencyPropertyChangedEventArgs e)
			{
				TargetValueSetCount++;
			}

			#endregion



			public int TargetValueSetCount { get; set; }


			#region Brush DependencyProperty

			public Brush Brush
			{
				get { return (Brush)GetValue(BrushProperty); }
				set { SetValue(BrushProperty, value); }
			}

			// Using a DependencyProperty as the backing store for Brush.  This enables animation, styling, binding, etc...
			public static readonly DependencyProperty BrushProperty =
				DependencyProperty.Register("Brush", typeof(Brush), typeof(Target2), new FrameworkPropertyMetadata(null, (s, e) => ((Target2)s)?.OnBrushChanged(e)));


			private void OnBrushChanged(DependencyPropertyChangedEventArgs e)
			{
				BrushSetCount++;
			}

			#endregion

			public int BrushSetCount { get; set; }


			#region Object DependencyProperty

			public Object Object
			{
				get { return (Object)GetValue(ObjectProperty); }
				set { SetValue(ObjectProperty, value); }
			}

			// Using a DependencyProperty as the backing store for Object.  This enables animation, styling, binding, etc...
			public static readonly DependencyProperty ObjectProperty =
				DependencyProperty.Register("Object", typeof(Object), typeof(Target2), new FrameworkPropertyMetadata(null, (s, e) => ((Target2)s)?.OnObjectChanged(e)));


			private void OnObjectChanged(DependencyPropertyChangedEventArgs e)
			{
				objectSetCount++;
			}

			#endregion

			public int objectSetCount { get; set; }
		}

		public class MyBindingSource : System.ComponentModel.INotifyPropertyChanged
		{
			private int _intValue;

			public int IntValue
			{
				get => _intValue;
				set
				{
					_intValue = value;
					OnPropertyChanged();
				}
			}

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
				=> PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}

		public class MySource : System.ComponentModel.INotifyPropertyChanged
		{
			private MySource2 _child;

			public MySource()
			{
				Child = new MySource2(42);
			}

			public MySource2 Child
			{
				get { return _child; }
				set
				{
					_child = value;

					if (PropertyChanged != null)
					{
						PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("Child"));
					}
				}
			}

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
		}

		public class MySource2
		{
			public MySource2(int value)
			{
				Value = value;
			}

			public int Value { get; private set; }
		}

		public class SourceLevel0
		{
			public SourceLevel1 Item { get; set; } = new SourceLevel1();
		}

		public class SourceLevel1
		{
			public SourceLevel2[] List { get; set; } = new SourceLevel2[0];
		}

		public class SourceLevel2
		{
			public SourceLevel3 Details { get; set; } = new SourceLevel3();
		}

		public class SourceLevel3
		{
			public int Info { get; set; } = 1000;

			public bool InfoBoolean { get; set; } = true;
		}

		public class Attachable
		{
			public static readonly DependencyProperty MyValueProperty =
				DependencyProperty.RegisterAttached(
					"MyValue",
					typeof(int),
					typeof(Attachable),
					new FrameworkPropertyMetadata(0)
				);

			public static int GetMyValue(object view)
			{
				return (int)DependencyObjectExtensions.GetValue(view, MyValueProperty);
			}

			public static void SetMyValue(object view, int row)
			{
				DependencyObjectExtensions.SetValue(view, MyValueProperty, row);
			}

			public static readonly DependencyProperty MyExplicitValueProperty =
				DependencyProperty.RegisterAttached(
					"MyExplicitValue",
					typeof(int),
					typeof(Attachable),
					new FrameworkPropertyMetadata(0)
				);

			public static int GetMyExplicitValue(Target1 view)
			{
				return (int)DependencyObjectExtensions.GetValue(view, MyValueProperty);
			}

			public static void SetMyExplicitValue(Target1 view, int row)
			{
				DependencyObjectExtensions.SetValue(view, MyValueProperty, row);
			}
		}

		public partial class PrivateProperty
		{
			public PrivateProperty(int value)
			{
				MyProperty = value;
			}

			private int MyProperty { get; set; }
		}

		public class PublicField
		{
			public int MyField;

			public PublicField(int value)
			{
				MyField = value;
			}
		}

		public class PrivateField
		{
			private int MyField;

			public PrivateField(int value)
			{
				MyField = value;
			}
		}
	}

	public partial class SelfBindingTest : DependencyObject
	{
		public int Property1
		{
			get { return (int)GetValue(Property1Property); }
			set { SetValue(Property1Property, value); }
		}

		public static readonly DependencyProperty Property1Property =
			DependencyProperty.Register("Property1", typeof(int), typeof(SelfBindingTest), new FrameworkPropertyMetadata(-1));

		public int Property2
		{
			get { return (int)GetValue(Property2Property); }
			set { SetValue(Property2Property, value); }
		}

		public static readonly DependencyProperty Property2Property =
			DependencyProperty.Register("Property2", typeof(int), typeof(SelfBindingTest), new FrameworkPropertyMetadata(-2));
	}

	public partial class MyObjectTest : DependencyObject
	{
		#region MyProperty DependencyProperty

		public int MyProperty
		{
			get { return (int)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(int), typeof(MyObjectTest), new FrameworkPropertyMetadata(0));


		private void OnMyPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion
	}


	public partial class MyControl : DependencyObject
	{
		public MyControl()
		{

		}

		public int MyProperty
		{
			get { return (int)this.GetValue(MyPropertyProperty); }
			set { this.SetValue(MyPropertyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(int), typeof(MyControl), new FrameworkPropertyMetadata(0));

		public Brush MyBrushProperty
		{
			get { return (Brush)this.GetValue(MyBrushPropertyProperty); }
			set { this.SetValue(MyBrushPropertyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyBrushProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyBrushPropertyProperty =
			DependencyProperty.Register("MyBrushProperty", typeof(Brush), typeof(MyControl), new FrameworkPropertyMetadata(null));


		public Visibility MyVisibilityProperty
		{
			get { return (Visibility)this.GetValue(MyVisibilityPropertyProperty); }
			set { this.SetValue(MyVisibilityPropertyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyVisibilityProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyVisibilityPropertyProperty =
			DependencyProperty.Register("MyVisibilityProperty", typeof(Visibility), typeof(MyControl), new FrameworkPropertyMetadata(Visibility.Visible));

	}

	public class OppositeConverter : IValueConverter
	{
		public int ConversionCount { get; private set; }

		public object LastValue { get; private set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			LastValue = value;
			++ConversionCount;

			var number = (int)value;

			return -number;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}

	public class BoolToNumber : IValueConverter
	{
		public int ConversionCount { get; private set; }

		public object LastValue { get; private set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			LastValue = value;
			++ConversionCount;

			var b = (bool)value;

			return b ? -1000 : 1000;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}

	public partial class UIDependencyObject : FrameworkElement
	{
		public DependencyObject SubProperty
		{
			get { return (DependencyObject)GetValue(SubPropertyProperty); }
			set { SetValue(SubPropertyProperty, value); }
		}

		public static readonly DependencyProperty SubPropertyProperty =
			DependencyProperty.Register("SubProperty", typeof(DependencyObject), typeof(UIDependencyObject), new PropertyMetadata(null));

		public int MyValue
		{
			get { return (int)GetValue(MyValueProperty); }
			set { SetValue(MyValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyValueProperty =
			DependencyProperty.Register("MyValue", typeof(int), typeof(UIDependencyObject), new PropertyMetadata(-1));

	}

	public partial class NonUIDependencyObject : DependencyObject
	{
		public DependencyObject SubProperty
		{
			get { return (DependencyObject)GetValue(SubPropertyProperty); }
			set { SetValue(SubPropertyProperty, value); }
		}

		public static readonly DependencyProperty SubPropertyProperty =
			DependencyProperty.Register("SubProperty", typeof(DependencyObject), typeof(NonUIDependencyObject), new PropertyMetadata(null));

		public int MyValue
		{
			get { return (int)GetValue(MyValueProperty); }
			set { SetValue(MyValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyValueProperty =
			DependencyProperty.Register("MyValue", typeof(int), typeof(NonUIDependencyObject), new PropertyMetadata(-1));

	}

	public class NonUIDependencyObject_Model : System.ComponentModel.INotifyPropertyChanged
	{
		private int myModelValue;

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		public int MyModelValue
		{
			get => myModelValue; set
			{
				myModelValue = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(MyModelValue)));
			}
		}
	}
}
