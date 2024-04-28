using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Tests.Windows_UI_Xaml_Data.BindingTests.Controls;
using Microsoft.UI.Xaml.Controls;
using FluentAssertions;
using Microsoft.UI.Xaml.Data;
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

			var tb = SUT.FindName("innerTextBlock");

			// This is expected to be null as SUT's name scope is different from innerTextBlock's name scope.
			Assert.IsNull(tb);
		}

		[TestMethod]
		public void When_ElementName_In_Template_Resource()
		{
			var SUT = new Binding_ElementName_In_Template_Resource();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock");

			// This is expected to be null as SUT's name scope is different from innerTextBlock's name scope.
			Assert.IsNull(tb);
		}

		[TestMethod]
		public void When_ElementName_In_Template_Resource_In_Dictionary()
		{
			var SUT = new Binding_ElementName_In_Template_Resource_In_Dictionary();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock");

			// This is expected to be null as SUT's name scope is different than innerTextBlock's name scope.
			Assert.IsNull(tb);
		}

		[TestMethod]
		public void When_ElementName_In_Template_ItemsControl()
		{
			var SUT = new Binding_ElementName_In_Template_ItemsControl();

			SUT.PrimaryActionsList.ItemsSource = new[] { "test" };

			SUT.ForceLoaded();

			var button = SUT.FindName("button");

			// This is expected to be null as SUT's name scope is different than button's name scope.
			Assert.IsNull(button);
		}

		[TestMethod]
		public void Binding_ElementName_In_Template_ItemsControl_Nested_Outer()
		{
			var SUT = new Binding_ElementName_In_Template_ItemsControl_Nested_Outer();

			SUT.ForceLoaded();

			SUT.PrimaryActionsList.ApplyTemplate();

			SUT.PrimaryActionsList.ItemsSource = new[] { "test" };

			var secondaryActionsList = SUT.FindName("SecondaryActionsList");

			// This is expected to be null as SUT's name scope is different than SecondaryActionsList's name scope.
			Assert.IsNull(secondaryActionsList);

			var button = SUT.FindName("button");

			// This is expected to be null as SUT's name scope is different than button's name scope.
			Assert.IsNull(button);
		}

		[TestMethod]
		public void When_ElementName_In_Template_ItemsControl_NonUINested()
		{
			var SUT = new Binding_ElementName_In_Template_ItemsControl_NonUINested();

			SUT.PrimaryActionsList.ItemsSource = new[] { "test" };

			SUT.ForceLoaded();

			var button = SUT.FindName("button");

			// This is expected to be null as SUT's name scope is different than button's name scope.
			Assert.IsNull(button);
		}

		[TestMethod]
		public void When_ElementName_NonUINested_GlobalResources()
		{
			var SUT = new Binding_ElementName_NonUINested_GlobalResources();

			var primaryActionsList = SUT.FindName("PrimaryActionsList");

			// This is expected to be null as SUT's name scope is different than PrimaryActionsList's name scope.
			Assert.IsNull(primaryActionsList);

			SUT.ForceLoaded();

			var button = SUT.FindName("button");

			// This is expected to be null as SUT's name scope is different than button's name scope.
			Assert.IsNull(button);
		}

		[TestMethod]
		public void When_ElementName_In_Template_In_Template()
		{
			var SUT = new Binding_ElementName_In_Template_In_Template();

			SUT.ForceLoaded();

			var tb = SUT.FindName("innerTextBlock");

			// This is expected to be null as SUT's name scope is different than innerTextBlock's name scope.
			Assert.IsNull(tb);
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
