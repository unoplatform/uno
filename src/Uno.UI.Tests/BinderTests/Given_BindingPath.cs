using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_BindingPath
	{
		[TestMethod]
		public void When_SetValue_Then_TargetUpdated()
		{
			var (target, sut) = Arrange();

			sut.Value = "42";

			Assert.AreEqual("42", target.Value);
		}

		[TestMethod]
		public void When_WithHigherPrecedence_SetValue_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(DependencyPropertyValuePrecedences.Animations);

			sut.Value = "Animations"; // Animations

			Assert.AreEqual("Animations", target.Value);

			Assert.AreEqual("Animations", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Animations));
		}

		[TestMethod]
		public void When_WithHigherPrecedence_SetValueAndLocalValue_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(DependencyPropertyValuePrecedences.Animations);

			sut.Value = "Animations";
			sut.SetLocalValue("Local");

			Assert.AreEqual("Animations", target.Value);

			Assert.AreEqual("Animations", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Animations));
			Assert.AreEqual("Local", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Local));
		}

		[TestMethod]
		public void When_WithHigherPrecedence_SetValueAndLocalValueAndClear_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(DependencyPropertyValuePrecedences.Animations);

			sut.Value = "Animations";
			sut.SetLocalValue("Local");
			sut.ClearValue();

			Assert.AreEqual("Local", target.Value);

			Assert.AreEqual(DependencyProperty.UnsetValue, target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Animations));
			Assert.AreEqual("Local", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Local));
		}

		[TestMethod]
		public void When_WithHigherPrecedence_SetValueAndClear_SetTargetValue_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(DependencyPropertyValuePrecedences.Animations);

			sut.Value = "Animations";
			sut.ClearValue();

			target.Value = "TargetLocalValue";

			Assert.AreEqual("TargetLocalValue", target.Value);

			Assert.AreEqual(DependencyProperty.UnsetValue, target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Animations));
			Assert.AreEqual("TargetLocalValue", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Local));
		}

		[TestMethod]
		public void When_WithLowerPrecedence_SetValue_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(DependencyPropertyValuePrecedences.Inheritance);
			target.Value = "CustomDefaultLocalValue";

			sut.Value = "Inherit";

			Assert.AreEqual("CustomDefaultLocalValue", target.Value);

			Assert.AreEqual("Inherit", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Inheritance));
		}

		[TestMethod]
		public void When_WithLowerPrecedence_SetValueAndLocalValue_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(DependencyPropertyValuePrecedences.Inheritance);

			sut.Value = "Inherit";
			sut.SetLocalValue("Local");

			Assert.AreEqual("Local", target.Value);

			Assert.AreEqual("Local", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Local));
			Assert.AreEqual("Inherit", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Inheritance));
		}

		[TestMethod]
		public void When_WithLowerPrecedence_SetValueAndLocalValueAndClear_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(DependencyPropertyValuePrecedences.Inheritance);

			sut.Value = "Inherit";
			sut.SetLocalValue("Local");
			sut.ClearValue();

			Assert.AreEqual("Local", target.Value);

			Assert.AreEqual(DependencyProperty.UnsetValue, target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Animations));
			Assert.AreEqual("Local", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Local));
		}

		[TestMethod]
		public void When_WithLowerPrecedence_SetValueAndClear_SetTargetValue_Then_ValueUpdated()
		{
			var (target, sut) = Arrange(DependencyPropertyValuePrecedences.Inheritance);

			sut.Value = "Inherit";
			sut.ClearValue();

			target.Value = "TargetLocalValue";

			Assert.AreEqual("TargetLocalValue", target.Value);

			Assert.AreEqual(DependencyProperty.UnsetValue, target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Animations));
			Assert.AreEqual("TargetLocalValue", target.GetPrecedenceSpecificValue(MyTarget.ValueProperty, DependencyPropertyValuePrecedences.Local));
		}

		[TestMethod]
		public void When_Initially_Incorrect_DataContext()
		{
			var (target, sut) = ArrangeIncorrect(DependencyPropertyValuePrecedences.Local);

			// Expecting this to fail because the DataContext does not match expectations
			sut.SetLocalValue("Initial");

			// Fix the DataContext, which should also fix the setters and allow the BindingPath to work correctly
			var vm = new MyTarget();
			sut.DataContext = vm;

			// Expecting this to succeed
			sut.SetLocalValue("Initial2");

			Assert.AreEqual("Initial2", sut.Value);
			Assert.AreEqual("Initial2", vm.Value);
		}

		private static (MyTarget target, BindingPath binding) Arrange(DependencyPropertyValuePrecedences? precedence = null)
		{
			var target = new MyTarget();
			var binding = new BindingPath(nameof(MyTarget.Value), MyTarget.FallbackValue, precedence, false)
			{
				DataContext = target
			};

			return (target, binding);
		}

		private static (object target, BindingPath binding) ArrangeIncorrect(DependencyPropertyValuePrecedences? precedence = null)
		{
			var target = new object();
			var binding = new BindingPath(nameof(MyTarget.Value), MyTarget.FallbackValue, precedence, false)
			{
				DataContext = target
			};

			return (target, binding);
		}

		public partial class MyTarget : DependencyObject
		{
			public const string DefaultValue = "__default__";
			public const string FallbackValue = "__fallback__";

			public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
				"Value", typeof(string), typeof(MyTarget), new FrameworkPropertyMetadata(DefaultValue));

			public string Value
			{
				get => (string) this.GetValue(ValueProperty);
				set => this.SetValue(ValueProperty, value);
			}
		}
	}
}
