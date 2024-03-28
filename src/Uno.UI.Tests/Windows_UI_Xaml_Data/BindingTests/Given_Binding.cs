using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Tests.Windows_UI_Xaml_Data.BindingTests.Controls;
using Windows.UI.Xaml.Controls;
using FluentAssertions;
using Windows.UI.Xaml.Data;
using FluentAssertions.Execution;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.BindingTests
{
	[TestClass]
	public class Given_Binding
	{
		[TestMethod]
		public void When_ElementName_In_Template()
		{
			var SUT = new Binding_ElementName_In_Template();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.AreEqual(SUT.topLevel.Tag, tb.Text);
		}

		[TestMethod]
		public void When_ElementName_In_Template_Resource()
		{
			var SUT = new Binding_ElementName_In_Template_Resource();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.AreEqual(SUT.topLevel.Tag, tb.Text);
		}

		[TestMethod]
		public void When_ElementName_In_Template_Resource_In_Dictionary()
		{
			var SUT = new Binding_ElementName_In_Template_Resource_In_Dictionary();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.AreEqual(SUT.topLevel.Tag, tb.Text);
		}

		[TestMethod]
		public void When_ElementName_In_Template_ItemsControl()
		{
			var SUT = new Binding_ElementName_In_Template_ItemsControl();

			SUT.PrimaryActionsList.ItemsSource = new[] { "test" };

			SUT.ForceLoaded();

			var button = SUT.FindName("button") as Windows.UI.Xaml.Controls.Button;

			Assert.AreEqual(SUT.PrimaryActionsList.Tag, button.Tag);
		}

		[TestMethod]
		public void Binding_ElementName_In_Template_ItemsControl_Nested_Outer()
		{
			var SUT = new Binding_ElementName_In_Template_ItemsControl_Nested_Outer();

			SUT.ForceLoaded();

			SUT.PrimaryActionsList.ApplyTemplate();

			SUT.PrimaryActionsList.ItemsSource = new[] { "test" };

			var SecondaryActionsList = SUT.FindName("SecondaryActionsList") as ItemsControl;
			SecondaryActionsList.ItemsSource = new[] { "test" };

			var button = SUT.FindName("button") as Button;
			Assert.AreEqual(SUT.Tag, button.Tag);
		}

		[TestMethod]
		public void When_ElementName_In_Template_ItemsControl_NonUINested()
		{
			var SUT = new Binding_ElementName_In_Template_ItemsControl_NonUINested();

			SUT.PrimaryActionsList.ItemsSource = new[] { "test" };

			SUT.ForceLoaded();

			var button = SUT.FindName("button") as Windows.UI.Xaml.Controls.Button;

			Assert.AreEqual(SUT.PrimaryActionsList.Tag, button.Tag);

			var nestedDO = Binding_ElementName_In_Template_ItemsControl_NonUINested_Attached.GetNonUIObject(button);

			Assert.IsNotNull(nestedDO);
			Assert.AreEqual(nestedDO.InnerProperty, button.Tag);
		}

		[TestMethod]
		public void When_ElementName_NonUINested_GlobalResources()
		{
			var SUT = new Binding_ElementName_NonUINested_GlobalResources();

			var primaryActionsList = SUT.FindName("PrimaryActionsList") as Windows.UI.Xaml.Controls.ItemsControl;

			primaryActionsList.ItemsSource = new[] { "test" };

			SUT.ForceLoaded();

			var button = SUT.FindName("button") as Windows.UI.Xaml.Controls.Button;

			Assert.AreEqual(primaryActionsList.Tag, button.Tag);

			var nestedDO = Binding_ElementName_NonUINested_GlobalResources_Attached.GetNonUIObject(button);

			Assert.IsNotNull(nestedDO);
			Assert.AreEqual(nestedDO.InnerProperty, button.Tag);
		}

		[TestMethod]
		public void When_ElementName_In_Template_In_Template()
		{
			var SUT = new Binding_ElementName_In_Template_In_Template();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock") as Windows.UI.Xaml.Controls.TextBlock;

			Assert.AreEqual(SUT.topLevel.Tag, tb.Text);
		}

		[TestMethod]
		public void When_Xaml_Object_With_Common_Properties()
		{
			using var _ = new AssertionScope();
			var SUT = new Binding_Xaml_Object_With_Common_Properties();

			SUT.ForceLoaded();

			SUT.topLevel.Should().NotBeNull();
			SUT.topLevel.Text.Should().Be("42");

			var bindingExpression = SUT.topLevel.GetBindingExpression(TextBlock.TextProperty);
			bindingExpression.Should().NotBeNull();

			var binding = bindingExpression.ParentBinding;
			binding.Should().NotBeNull();
			binding.Path.Should().NotBeNull();
			binding.Path.Path.Should().Be("Tag");
			// https://github.com/unoplatform/uno/issues/8532
			//binding.ElementName.Should().BeOfType<ElementNameSubject>().Which.Name.Should().Be("topLevel");
			binding.ElementName.Should().BeOfType<ElementNameSubject>();
			binding.Converter.Should().NotBeNull();
			binding.ConverterParameter.Should().Be("topLevel");
			binding.ConverterLanguage.Should().Be("topLevel");
			binding.UpdateSourceTrigger.Should().Be(UpdateSourceTrigger.Default);
			binding.TargetNullValue.Should().Be("TargetNullValue");
			binding.FallbackValue.Should().Be("FallbackValue");
			binding.Mode.Should().Be(BindingMode.OneWay);
			binding.RelativeSource.Should().NotBeNull();
			binding.RelativeSource.Mode.Should().Be(RelativeSourceMode.None);
			binding.Source.Should().Be("Source");
		}

		[TestMethod]
		public void When_Xaml_Object_With_Xaml_Object_Properties()
		{
			using var _ = new AssertionScope();
			var SUT = new Binding_Xaml_Object_With_Xaml_Object_Properties();

			SUT.ForceLoaded();

			SUT.topLevel.Should().NotBeNull();
			SUT.topLevel.Text.Should().Be("42");

			var bindingExpression = SUT.topLevel.GetBindingExpression(TextBlock.TextProperty);
			bindingExpression.Should().NotBeNull();

			var binding = bindingExpression.ParentBinding;
			binding.Should().NotBeNull();
			binding.Path.Should().NotBeNull();
			binding.Path.Path.Should().Be("Tag");
			binding.ElementName.Should().BeOfType<ElementNameSubject>();
			binding.Converter.Should().NotBeNull();
			binding.ConverterParameter.Should().Be("topLevel");
			binding.ConverterLanguage.Should().Be("topLevel");
			binding.UpdateSourceTrigger.Should().Be(UpdateSourceTrigger.Default);
			binding.TargetNullValue.Should().Be("TargetNullValue");
			binding.FallbackValue.Should().Be("FallbackValue");
			binding.Mode.Should().Be(BindingMode.OneWay);
			binding.RelativeSource.Should().NotBeNull();
			binding.RelativeSource.Mode.Should().Be(RelativeSourceMode.None);
			binding.Source.Should().Be("Source");
		}

		[TestMethod]
		public void When_Binding_Empty_Quotes()
		{
			var SUT = new Binding_Empty_Quotes();
			SUT.ForceLoaded();
			SUT.sut.Text.Should().Be("Current DataContext: MyDataContext");
		}

		[TestMethod]
		public void When_TemplateBinding_Attached_Property()
		{
			var SUT = new Binding_TemplateBinding_AttachedDP();
			SUT.ForceLoaded();
			var tb = SUT.tb;
			var sv = (ScrollViewer)tb.GetTemplateRoot();

			Assert.AreEqual(ScrollBarVisibility.Auto, sv.HorizontalScrollBarVisibility);
			Assert.AreEqual(ScrollBarVisibility.Hidden, sv.VerticalScrollBarVisibility);

			Assert.AreEqual(ScrollBarVisibility.Auto, (ScrollBarVisibility)tb.GetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty));
			Assert.AreEqual(ScrollBarVisibility.Hidden, (ScrollBarVisibility)tb.GetValue(ScrollViewer.VerticalScrollBarVisibilityProperty));
		}
	}
}
