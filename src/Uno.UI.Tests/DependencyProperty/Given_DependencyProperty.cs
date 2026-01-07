using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AwesomeAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_DependencyProperty
	{
		[ClassInitialize]
		public static void Init(TestContext ctx)
		{
		}

		[TestMethod]
		public void When_SetValue_and_NoMetadata()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_and_NoMetadata), typeof(string), typeof(MockDependencyObject), null);

			SUT.SetValue(testProperty, "test");

			Assert.AreEqual("test", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_GetDefaultValue()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_GetDefaultValue), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));

			Assert.AreEqual("42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_GetDefaultValue_And_PropertyType_Is_ValueType_And_No_DefaultValue()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_GetDefaultValue_And_PropertyType_Is_ValueType_And_No_DefaultValue), typeof(int), typeof(MockDependencyObject), null);

			Assert.AreEqual(default(int), SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_GetDefaultValue_And_PropertyType_Is_ValueType_And_DefaultValue_Is_Null()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_GetDefaultValue_And_PropertyType_Is_ValueType_And_DefaultValue_Is_Null), typeof(int), typeof(MockDependencyObject), new PropertyMetadata(null));

			Assert.AreEqual(default(int), SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_GetDefaultValue_And_PropertyType_Is_ValueType_And_DefaultValue_Is_Different_Type()
		{
			var defaultValue = "test";

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_GetDefaultValue_And_PropertyType_Is_ValueType_And_DefaultValue_Is_Different_Type), typeof(int), typeof(MockDependencyObject), new PropertyMetadata(defaultValue));

			Assert.AreEqual(defaultValue, SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_Registration_NotRaised()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_Registration_NotRaised), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));

			SUT.RegisterDisposablePropertyChangedCallback(testProperty, (s, e) =>
			{
				Assert.Fail();
			});

			Assert.AreEqual("42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_and_NoDefaultValue_Registration_Raised()
		{
			bool raised = false;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_and_NoDefaultValue_Registration_Raised), typeof(string), typeof(MockDependencyObject), null);

			SUT.RegisterDisposablePropertyChangedCallback(testProperty, (s, e) =>
			{
				Assert.AreEqual("test", e.NewValue);
				Assert.IsNull(e.OldValue);
				raised = true;
			});

			Assert.IsNull(SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, "test");

			Assert.AreEqual("test", SUT.GetValue(testProperty));
			Assert.IsTrue(raised);
		}

		[TestMethod]
		public void When_SetValue_and_DefaultValue_Registration_Raised()
		{
			bool raised = false;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_and_DefaultValue_Registration_Raised), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));

			SUT.RegisterDisposablePropertyChangedCallback(testProperty, (s, e) =>
			{
				Assert.AreEqual("test", e.NewValue);
				Assert.AreEqual("42", e.OldValue);
				raised = true;
			});

			Assert.AreEqual("42", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, "test");

			Assert.AreEqual("test", SUT.GetValue(testProperty));
			Assert.IsTrue(raised);
		}

		[TestMethod]
		public void When_SetValue_and_DefaultValue_Registration_RaisedTwice()
		{
			int raisedCount = 0;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_and_DefaultValue_Registration_RaisedTwice), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));

			SUT.RegisterDisposablePropertyChangedCallback(testProperty, (s, e) =>
			{

				if (raisedCount == 0)
				{
					Assert.AreEqual("test", e.NewValue);
					Assert.AreEqual("42", e.OldValue);
				}
				else if (raisedCount == 1)
				{
					Assert.AreEqual("test2", e.NewValue);
					Assert.AreEqual("test", e.OldValue);
				}

				raisedCount++;
			});

			Assert.AreEqual("42", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, "test");
			Assert.AreEqual("test", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, "test2");
			Assert.AreEqual("test2", SUT.GetValue(testProperty));

			Assert.AreEqual(2, raisedCount);
		}

		[TestMethod]
		public void When_SetValue_And_DefaultValue_Then_StaticRegistration_NotRaised()
		{
			PropertyChangedCallback cb = (s, e) =>
			{
				Assert.Fail();
			};

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_And_DefaultValue_Then_StaticRegistration_NotRaised), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42", cb));

			Assert.AreEqual("42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_And_NoDefaultValue_Then_StaticRegistration_NotRaised()
		{
			PropertyChangedCallback cb = (s, e) =>
			{
				Assert.Fail();
			};

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_And_NoDefaultValue_Then_StaticRegistration_NotRaised), typeof(string), typeof(MockDependencyObject), new PropertyMetadata(null, cb));

			Assert.IsNull(SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_And_DefaultValue_Then_StaticRegistration_Raised()
		{
			bool raised = false;

			PropertyChangedCallback cb = (s, e) =>
			{
				Assert.AreEqual("test", e.NewValue);
				Assert.AreEqual("42", e.OldValue);
				raised = true;
			};

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_And_DefaultValue_Then_StaticRegistration_Raised), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42", cb));

			SUT.SetValue(testProperty, "test");

			Assert.AreEqual("test", SUT.GetValue(testProperty));
			Assert.IsTrue(raised);
		}

		[TestMethod]
		public void When_SetValue_And_DefaultValue_Then_StaticRegistration_RaisedMultiple()
		{
			var raisedCount = 0;

			var SUT = new MockDependencyObject();

			PropertyChangedCallback cb = (s, e) =>
			{

				Assert.AreEqual(s, SUT);

				switch (raisedCount++)
				{
					case 0:
						Assert.AreEqual("42", e.OldValue);
						Assert.AreEqual("test", e.NewValue);
						break;
					case 1:
						Assert.AreEqual("test", e.OldValue);
						Assert.AreEqual("test2", e.NewValue);
						break;
					case 2:
						Assert.AreEqual("test2", e.OldValue);
						Assert.IsNull(e.NewValue);
						break;
				}
			};

			var testProperty = DependencyProperty.Register(nameof(When_SetValue_And_DefaultValue_Then_StaticRegistration_RaisedMultiple), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42", cb));

			SUT.SetValue(testProperty, "test");
			Assert.AreEqual("test", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, "test2");
			Assert.AreEqual("test2", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, null);
			Assert.IsNull(SUT.GetValue(testProperty));

			Assert.AreEqual(3, raisedCount);
		}

		[TestMethod]
		public void When_SetValue_Integer_And_SameValue_Then_RaisedOnce()
		{
			var raisedCount = 0;

			var SUT = new MockDependencyObject();

			PropertyChangedCallback cb = (s, e) =>
			{

				Assert.AreEqual(s, SUT);

				switch (raisedCount++)
				{
					case 0:
						Assert.AreEqual(0, e.OldValue);
						Assert.AreEqual(42, e.NewValue);
						break;

					case 1:
						Assert.AreEqual(42, e.OldValue);
						Assert.AreEqual(43, e.NewValue);
						break;

					default:
						Assert.Fail("Should be called only three times");
						break;
				}
			};

			var testProperty = DependencyProperty.Register(nameof(When_SetValue_Integer_And_SameValue_Then_RaisedOnce), typeof(int), typeof(MockDependencyObject), new PropertyMetadata(0, cb));

			SUT.SetValue(testProperty, 42);
			Assert.AreEqual(42, SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, 42);
			Assert.AreEqual(42, SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, 43);
			Assert.AreEqual(43, SUT.GetValue(testProperty));

			Assert.AreEqual(2, raisedCount);
		}

		[TestMethod]
		public void When_SetValue_Thickness_And_SameValue_Then_RaisedOnce()
		{
			var raisedCount = 0;

			var SUT = new MockDependencyObject();

			PropertyChangedCallback cb = (s, e) =>
			{

				Assert.AreEqual(s, SUT);

				switch (raisedCount++)
				{
					case 0:
						Assert.AreEqual(Thickness.Empty, e.OldValue);
						Assert.AreEqual(new Thickness(2), e.NewValue);
						break;

					case 1:
						Assert.AreEqual(new Thickness(2), e.OldValue);
						Assert.AreEqual(new Thickness(1), e.NewValue);
						break;

					default:
						Assert.Fail("Should be called only three times");
						break;
				}
			};

			var testProperty = DependencyProperty.Register(nameof(When_SetValue_Thickness_And_SameValue_Then_RaisedOnce), typeof(Thickness), typeof(MockDependencyObject), new PropertyMetadata(Thickness.Empty, cb));

			SUT.SetValue(testProperty, new Thickness(2));
			Assert.AreEqual(new Thickness(2), SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, new Thickness(2));
			Assert.AreEqual(new Thickness(2), SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, new Thickness(1));
			Assert.AreEqual(new Thickness(1), SUT.GetValue(testProperty));

			Assert.AreEqual(2, raisedCount);
		}

		[TestMethod]
		public void When_SetValue_Reference_And_SameValue_Then_RaisedOnce()
		{
			var raisedCount = 0;

			var SUT = new MockDependencyObject();

			var o1 = new object();
			var o2 = new object();

			PropertyChangedCallback cb = (s, e) =>
			{

				Assert.AreEqual(s, SUT);

				switch (raisedCount++)
				{
					case 0:
						Assert.IsNull(e.OldValue);
						Assert.AreEqual(o1, e.NewValue);
						break;

					case 1:
						Assert.AreEqual(o1, e.OldValue);
						Assert.AreEqual(o2, e.NewValue);
						break;

					default:
						Assert.Fail("Should be called only three times");
						break;
				}
			};

			var testProperty = DependencyProperty.Register(nameof(When_SetValue_Reference_And_SameValue_Then_RaisedOnce), typeof(object), typeof(MockDependencyObject), new PropertyMetadata(null, cb));

			SUT.SetValue(testProperty, o1);
			Assert.AreEqual(o1, SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, o1);
			Assert.AreEqual(o1, SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, o2);
			Assert.AreEqual(o2, SUT.GetValue(testProperty));

			Assert.AreEqual(2, raisedCount);
		}

		[TestMethod]
		public void When_Property_RegisterTwice_then_Fail()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Property_RegisterTwice_then_Fail), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));
			Assert.ThrowsExactly<InvalidOperationException>(() => DependencyProperty.Register(nameof(When_Property_RegisterTwice_then_Fail), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42")));
		}

		[TestMethod]
		public void When_Local_Value_Cleared_Then_Default_Returned()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Local_Value_Cleared_Then_Default_Returned), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));

			SUT.SetValue(testProperty, "Not42");

			Assert.AreEqual("Not42", SUT.GetValue(testProperty));

			SUT.ClearValue(testProperty);

			Assert.AreEqual("42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_Two_Precedences_Set_Then_Only_Highest_Returned()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Two_Precedences_Set_Then_Only_Highest_Returned), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));

			SUT.SetValue(testProperty, "Not42");
			SUT.SetValue(testProperty, "ALowPriorityValue", DependencyPropertyValuePrecedences.DefaultStyle);

			Assert.AreEqual("Not42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_Animating_And_Animation_Is_Done_Then_Returns_Local()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Animating_And_Animation_Is_Done_Then_Returns_Local), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));

			SUT.SetValue(testProperty, "Not42");

			Assert.AreEqual("Not42", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, "TheAnimatedValue", DependencyPropertyValuePrecedences.Animations);

			Assert.AreEqual("TheAnimatedValue", SUT.GetValue(testProperty));

			SUT.ClearValue(testProperty, DependencyPropertyValuePrecedences.Animations);

			Assert.AreEqual("Not42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_Setting_UnsetValue_Then_Property_Reverts_To_Default()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Setting_UnsetValue_Then_Property_Reverts_To_Default), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));

			SUT.SetValue(testProperty, "Not42");

			Assert.AreEqual("Not42", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, DependencyProperty.UnsetValue);

			Assert.AreEqual("42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_Setting_UnsetValue_On_DefaultValue_Then_Fails()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Setting_UnsetValue_On_DefaultValue_Then_Fails), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));
			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.SetValue(testProperty, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.DefaultValue));
		}

		[TestMethod]
		public void When_Setting_Value_Then_Current_Highest_Is_Local()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Setting_Value_Then_Current_Highest_Is_Local), typeof(string), typeof(MockDependencyObject), new PropertyMetadata("42"));

			SUT.SetValue(testProperty, "Not42");

			Assert.AreEqual("Not42", SUT.GetValue(testProperty));

			Assert.AreEqual(DependencyPropertyValuePrecedences.Local, SUT.GetCurrentHighestValuePrecedence(testProperty));
		}

		[TestMethod]
		public void When_ClearValue_and_Value_Was_Set_Then_Callback_Raised()
		{
			bool raised = false;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_ClearValue_and_Value_Was_Set_Then_Callback_Raised), typeof(string), typeof(MockDependencyObject), null);

			SUT.SetValue(testProperty, "test");

			SUT.RegisterDisposablePropertyChangedCallback(testProperty, (s, e) =>
			{
				Assert.IsNull(e.NewValue);
				Assert.AreEqual("test", e.OldValue);
				raised = true;
			});

			Assert.AreEqual("test", SUT.GetValue(testProperty));

			SUT.ClearValue(testProperty);

			Assert.IsNull(SUT.GetValue(testProperty));
			Assert.IsTrue(raised);
		}

		[TestMethod]
		public void When_SetValue_Null_And_PropertyType_Is_ValueType_Then_GetValue_Explicit_DefaultValue()
		{
			var defaultValue = 42;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_SetValue_Null_And_PropertyType_Is_ValueType_Then_GetValue_Explicit_DefaultValue),
				typeof(int),
				typeof(MockDependencyObject),
				new PropertyMetadata(defaultValue)
			);

			SUT.SetValue(testProperty, null);

			Assert.AreEqual(defaultValue, SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_Null_And_PropertyType_Is_ValueType_Then_GetValue_Implicit_DefaultValue()
		{
			var defaultValue = default(int);

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_SetValue_Null_And_PropertyType_Is_ValueType_Then_GetValue_Implicit_DefaultValue),
				typeof(int),
				typeof(MockDependencyObject),
				null
			);

			SUT.SetValue(testProperty, null);

			Assert.AreEqual(defaultValue, SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_CoerceValueCallback_Now_And_CoerceValue()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_Now_And_CoerceValue),
				typeof(DateTime),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					null,
					null,
					Now
				)
			);

			Assert.AreEqual(default(DateTime), SUT.GetValue(testProperty));
			Assert.AreEqual(default(DateTime), SUT.GetValue(testProperty));

			var yesterday = DateTime.Now.AddDays(-1);
			SUT.SetValue(testProperty, yesterday);
			var now = SUT.GetValue(testProperty);
			Assert.AreNotEqual(yesterday, now);

			Assert.AreEqual(SUT.GetValue(testProperty), SUT.GetValue(testProperty));

			var time1 = SUT.GetValue(testProperty);
			var time2 = SUT.GetValue(testProperty);
			Assert.AreEqual(time1, time2);

			time1 = SUT.GetValue(testProperty);
			Thread.Sleep(1);
			SUT.SetValue(testProperty, null); // force coercion
			time2 = SUT.GetValue(testProperty);
			Assert.AreNotEqual(time1, time2);

			time1 = SUT.GetValue(testProperty);
			Thread.Sleep(1);
			SUT.CoerceValue(testProperty); // force coercion
			time2 = SUT.GetValue(testProperty);
			var a = time1 == time2;
			var b = time1.Equals(time2);
			var c = (DateTime)time2 > (DateTime)time1;
			Assert.AreNotEqual(time1, time2);
		}

		[TestMethod]
		public void When_CoerceValueCallback_Custom_And_CoerceValue()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_Custom_And_CoerceValue),
				typeof(string),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					null,
					null,
					Custom
				)
			);

			_customCoercion = "customValue";
			SUT.CoerceValue(testProperty);
			var coercedValue = SUT.GetValue(testProperty);

			Assert.AreEqual("customValue", coercedValue);
		}

		[TestMethod]
		public void When_CoerceValue_Reads_Property_Value()
		{
			var SUT = new MockDependencyObject();

			var previousValue = "Previous";
			var newValue = "New";

			Action callback = null;

			object Coerce(object dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
			{
				callback?.Invoke();

				return baseValue;
			}

			var property = DependencyProperty.Register(
				nameof(When_CoerceValue_Reads_Property_Value),
				typeof(string),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					"",
					null,
					Coerce
				)
			);

			callback = () =>
			{
				var currentValue = SUT.GetValue(property);
				Assert.AreEqual("", currentValue);
			};

			SUT.SetValue(property, previousValue);

			callback = () =>
			{
				var currentValue = SUT.GetValue(property);
				Assert.AreEqual(previousValue, currentValue);
			};

			SUT.SetValue(property, newValue);
		}

		[TestMethod]
		public void When_CoerceValueCallback_ReturnNull_DefaultValue_Not_Coerced()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_ReturnNull_DefaultValue_Not_Coerced),
				typeof(string),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					"default",
					null,
					ReturnNull
				)
			);

			Assert.AreEqual("default", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, "test");

			Assert.IsNull(SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, DependencyProperty.UnsetValue);

			Assert.AreEqual("default", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, "test");

			Assert.IsNull(SUT.GetValue(testProperty));

			SUT.ClearValue(testProperty);

			Assert.AreEqual("default", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_CoerceValueCallback_PreventSet()
		{
			var defaultValue = 5;
			var baseValue = -10;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_PreventSet),
				typeof(int),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					defaultValue,
					null,
					PreventSet
				)
			);

			SUT.SetValue(testProperty, baseValue);

			var actualValue = SUT.GetValue(testProperty);

			Assert.AreEqual(defaultValue, actualValue);
		}

		[TestMethod]
		public void When_CoerceValueCallback_DoNothing()
		{
			var defaultValue = 5;
			var baseValue = -10;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_DoNothing),
				typeof(int),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					defaultValue,
					null,
					DoNothing
				)
			);

			SUT.SetValue(testProperty, baseValue);

			var actualValue = SUT.GetValue(testProperty);

			Assert.AreEqual(baseValue, actualValue);
		}

		[TestMethod]
		public void When_CoerceValueCallback_ReturnNull()
		{
			var defaultValue = "A";
			var baseValue = "B";

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_ReturnNull),
				typeof(string),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					defaultValue,
					null,
					ReturnNull
				)
			);

			SUT.SetValue(testProperty, baseValue);

			var actualValue = SUT.GetValue(testProperty);

			Assert.IsNull(actualValue);
		}

		[TestMethod]
		public void When_CoerceValueCallback_ReadLocalValue()
		{
			var defaultValue = "A";
			var baseValue = "B";

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_ReadLocalValue),
				typeof(string),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					defaultValue,
					null,
					ReturnNull
				)
			);

			SUT.SetValue(testProperty, baseValue);

			var actualValue = SUT.GetValue(testProperty);
			var localValue = SUT.ReadLocalValue(testProperty);

			Assert.IsNull(actualValue);
			Assert.AreEqual(baseValue, localValue);
		}

		[TestMethod]
		public void When_CoerceValueCallback_ReturnNull_Integer()
		{
			var defaultValue = 5;
			var baseValue = -10;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_ReturnNull_Integer),
				typeof(int),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					defaultValue,
					null,
					ReturnNull
				)
			);

			SUT.SetValue(testProperty, baseValue);

			var actualValue = SUT.GetValue(testProperty);

			Assert.AreEqual(defaultValue, actualValue);
		}

		[TestMethod]
		public void When_CoerceValueCallback_AbsoluteInteger()
		{
			var defaultValue = 5;
			var baseValue = -10;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_AbsoluteInteger),
				typeof(int),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					defaultValue,
					null,
					AbsoluteInteger
				)
			);

			SUT.SetValue(testProperty, baseValue);

			var actualValue = SUT.GetValue(testProperty);

			Assert.AreEqual(Math.Abs(baseValue), actualValue);
		}

		[TestMethod]
		public void When_CoerceValueCallback_IgnoreNegative()
		{
			var defaultValue = 5;
			var baseValue = -10;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_IgnoreNegative),
				typeof(int),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					defaultValue,
					null,
					IgnoreNegative
				)
			);

			SUT.SetValue(testProperty, baseValue);

			var actualValue = SUT.GetValue(testProperty);

			Assert.AreEqual(defaultValue, actualValue);
		}

		[TestMethod]
		public void When_CoerceValueCallback_StringTake10()
		{
			var defaultValue = "";
			var baseValue = "ABCDEFGHIJKLMNOP";

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_StringTake10),
				typeof(int),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					defaultValue,
					null,
					StringTake10
				)
			);

			SUT.SetValue(testProperty, baseValue);

			var actualValue = SUT.GetValue(testProperty);

			Assert.AreEqual("ABCDEFGHIJ", actualValue);
		}

		[TestMethod]
		public void When_CorceValueCallback_Now()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CorceValueCallback_Now),
				typeof(DateTime),
				typeof(MockDependencyObject),
				new PropertyMetadata(
					null,
					null,
					Now
				)
			);

			var defaultValue = (DateTime)SUT.GetValue(testProperty);

			SUT.SetValue(testProperty, null);

			var actualValue = (DateTime)SUT.GetValue(testProperty);

			var later = DateTime.Now.AddMinutes(1);
			Assert.IsTrue(defaultValue < actualValue && actualValue < later);
		}

		#region CoerceValueCallbacks

		private object PreventSet(object dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			return DependencyProperty.UnsetValue;
		}

		private object DoNothing(object dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			return baseValue;
		}

		private object ReturnNull(object dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			return null;
		}

		private object AbsoluteInteger(object dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			return Math.Abs((int)baseValue);
		}

		private object IgnoreNegative(object dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			return (int)baseValue < 0
				? DependencyProperty.UnsetValue
				: baseValue;
		}

		private object StringTake10(object dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			return new string(((string)baseValue).Take(10).ToArray());
		}

		private object Now(object dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			return DateTime.Now;
		}

		private static object _customCoercion;
		private object Custom(object dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			if (_customCoercion != null)
			{
				var coercedValue = _customCoercion;
				_customCoercion = null;
				return coercedValue;
			}

			return baseValue;
		}

		#endregion

		[TestMethod]
		public void When_PropertyMetadata_Is_Null()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_PropertyMetadata_Is_Null),
				typeof(string),
				typeof(MockDependencyObject),
				null
			);

			var metadata = testProperty.GetMetadata(typeof(MockDependencyObject));

			Assert.IsNotNull(metadata);
			Assert.IsNull(metadata.CoerceValueCallback);
			Assert.IsNull(metadata.PropertyChangedCallback);
			Assert.IsNull(metadata.DefaultValue);
		}

		[TestMethod]
		public void When_SetValue_With_Coercion_Precedence_Then_Fail()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_SetValue_With_Coercion_Precedence_Then_Fail),
				typeof(string),
				typeof(MockDependencyObject),
				null
			);
			Assert.ThrowsExactly<ArgumentException>(() => SUT.SetValue(testProperty, "test", DependencyPropertyValuePrecedences.Coercion));
		}

		[TestMethod]
		public void When_SetValue_Inheritance_And_CoerceValue_Then_GetValue_Local_Is_UnsetValue()
		{
			var SUT = new MyDependencyObject1();

			SUT.SetValue(MyDependencyObject1.MyPropertyProperty, "value", DependencyPropertyValuePrecedences.Inheritance);
			SUT.CoerceValue(MyDependencyObject1.MyPropertyProperty);

			Assert.AreEqual(DependencyProperty.UnsetValue, SUT.ReadLocalValue(MyDependencyObject1.MyPropertyProperty));
		}

		[TestMethod]
		public void When_SetValue_TypeA_On_PropertyType_Of_TypeB_Then_GetValue_Of_TypeA()
		{
			var value = "test";

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_TypeA_On_PropertyType_Of_TypeB_Then_GetValue_Of_TypeA), typeof(int), typeof(MockDependencyObject), null);

			SUT.SetValue(testProperty, value);

			Assert.AreEqual(value, SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_Null_And_PropertyType_Is_ReferenceType_Then_GetValue_Null()
		{
			var defaultValue = "42";

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_SetValue_Null_And_PropertyType_Is_ReferenceType_Then_GetValue_Null),
				typeof(string),
				typeof(MockDependencyObject),
				new PropertyMetadata(defaultValue)
			);

			SUT.SetValue(testProperty, null);

			var value = SUT.GetValue(testProperty);

			Assert.IsNull(SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_Null_And_PropertyType_Is_Nullable_ValueType_Then_GetValue_Null()
		{
			var defaultValue = 42;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_SetValue_Null_And_PropertyType_Is_Nullable_ValueType_Then_GetValue_Null),
				typeof(int?),
				typeof(MockDependencyObject),
				new PropertyMetadata(defaultValue)
			);

			SUT.SetValue(testProperty, null);

			Assert.IsNull(SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_Null_And_PropertyType_Is_ValueType_Then_GetValue_DefaultValue()
		{
			var defaultValue = 42;

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_SetValue_Null_And_PropertyType_Is_ValueType_Then_GetValue_DefaultValue),
				typeof(int),
				typeof(MockDependencyObject),
				new PropertyMetadata(defaultValue)
			);

			SUT.SetValue(testProperty, null);

			var value = SUT.GetValue(testProperty);

			Assert.AreEqual(defaultValue, SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_And_RegisterPropertyChangedCallback()
		{
			var SUT = new SimpleDependencyObject1();

			int invocations = 0;

			var registration1 = SUT.RegisterPropertyChangedCallback(
				SimpleDependencyObject1.MyPropertyProperty,
				(s, e) =>
				{
					invocations++;
				}
			);

			SUT.SetValue(SimpleDependencyObject1.MyPropertyProperty, "42");

			var value = SUT.GetValue(SimpleDependencyObject1.MyPropertyProperty);

			Assert.AreEqual("42", value);
			Assert.AreEqual(1, invocations);
			Assert.AreEqual(1, SUT.ChangedCallbackCount);

			SUT.UnregisterPropertyChangedCallback(SimpleDependencyObject1.MyPropertyProperty, registration1);

			SUT.SetValue(SimpleDependencyObject1.MyPropertyProperty, "43");

			Assert.AreEqual(1, invocations);
			Assert.AreEqual(2, SUT.ChangedCallbackCount);
		}

		[TestMethod]
		public void When_SetValue_And_RegisterPropertyChangedCallback_Recurse()
		{
			var SUT = new SimpleDependencyObject1();

			int invocations = 0;
			int invocations2 = 0;
			long registration2 = 0;

			var registration1 = SUT.RegisterPropertyChangedCallback(
				SimpleDependencyObject1.MyPropertyProperty,
				(s, e) =>
				{
					invocations++;

					registration2 = SUT.RegisterPropertyChangedCallback(
						SimpleDependencyObject1.MyPropertyProperty,
						(s2, e2) =>
						{
							invocations2++;
						}
					);
				}
			);

			SUT.SetValue(SimpleDependencyObject1.MyPropertyProperty, "42");

			var value = SUT.GetValue(SimpleDependencyObject1.MyPropertyProperty);

			Assert.AreEqual("42", value);
			Assert.AreEqual(1, invocations);
			Assert.AreEqual(1, SUT.ChangedCallbackCount);
			Assert.AreEqual(0, invocations2);

			SUT.UnregisterPropertyChangedCallback(SimpleDependencyObject1.MyPropertyProperty, registration1);

			SUT.SetValue(SimpleDependencyObject1.MyPropertyProperty, "43");

			Assert.AreEqual(1, invocations);
			Assert.AreEqual(1, invocations2);
			Assert.AreEqual(2, SUT.ChangedCallbackCount);
		}

		[TestMethod]
		public void When_ManualRegister()
		{
			// Try reading the property before it gets registered to validate against caches cleanup
			Assert.IsNull(DependencyProperty.GetProperty(typeof(MockDependencyObject), "TestProperty"));

			var dp1 = DependencyProperty.Register("TestProperty", typeof(string), typeof(MockDependencyObject), new PropertyMetadata(null));

			var o = new MockDependencyObject();
			o.SetBinding(dp1, new Binding { Path = new PropertyPath("MyProperty") });

			o.DataContext = new { MyProperty = "42" };

			Assert.AreEqual("42", o.GetValue(dp1));

			var dp1p = DependencyProperty.GetProperty(typeof(MockDependencyObject), "TestProperty");

			Assert.IsNotNull(dp1p);
		}

		[TestMethod]
		public void When_SetValue_With_Nested_Callbacks()
		{
			var SUT = new MockDependencyObject();

			DependencyProperty property1 = null;
			DependencyProperty property2 = null;
			DependencyProperty property3 = null;

			PropertyChangedCallback OnProperty1Changed = (s, e) =>
			{
				SUT.SetValue(property3, 0); // we expect value 0 to be overwritten by property2's callback
				SUT.SetValue(property2, 2);
			};

			PropertyChangedCallback OnProperty2Changed = (s, e) =>
			{
				SUT.SetValue(property3, 3);
			};

			PropertyChangedCallback OnProperty3Changed = (s, e) =>
			{
			};

			property1 = DependencyProperty.Register("Property1", typeof(int), typeof(MockDependencyObject), new PropertyMetadata(0, OnProperty1Changed));
			property2 = DependencyProperty.Register("Property2", typeof(int), typeof(MockDependencyObject), new PropertyMetadata(0, OnProperty2Changed));
			property3 = DependencyProperty.Register("Property3", typeof(int), typeof(MockDependencyObject), new PropertyMetadata(0, OnProperty3Changed));

			SUT.SetValue(property1, 1);

			Assert.AreEqual(1, SUT.GetValue(property1));
			Assert.AreEqual(2, SUT.GetValue(property2));
			Assert.AreEqual(3, SUT.GetValue(property3));
		}

		[TestMethod]
		public void When_AddCallback_OnPropertyChanged()
		{
			var brush = new SolidColorBrush();
			IDisposable disposable = null;
			IDisposable disposable2 = null;
			disposable = brush.RegisterDisposablePropertyChangedCallback(OnBrushChanged);

			void OnBrushChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
			{
				disposable2 = brush.RegisterDisposablePropertyChangedCallback(OnInnerAddCallbackBrushChanged);
			}

			brush.Color = Colors.Red;

			disposable?.Dispose();
			disposable2?.Dispose();
		}

		private void OnInnerAddCallbackBrushChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			Assert.Fail();
		}

		[TestMethod]
		public void When_RemoveCallback_OnPropertyChanged()
		{
			var brush = new SolidColorBrush();
			IDisposable disposable = null;
			IDisposable disposable2 = null;
			disposable = brush.RegisterDisposablePropertyChangedCallback(OnBrushChanged);
			disposable2 = brush.RegisterDisposablePropertyChangedCallback(OnInnerRemoveCallbackBrushChanged);

			void OnBrushChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
			{
				disposable2.Dispose();
			}

			Action act = () => brush.Color = Colors.Red;
			act.Should().NotThrow();

			disposable?.Dispose();
			disposable2?.Dispose();
		}

		private void OnInnerRemoveCallbackBrushChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
		}

		[TestMethod]
		public void When_AddParentChanged_OnParentChanged()
		{
			var firstParent = new Microsoft.UI.Xaml.Controls.Border();
			var secondParent = new Microsoft.UI.Xaml.Controls.Border();
			var border = new Microsoft.UI.Xaml.Controls.Border();
			firstParent.Child = border;

			IDisposable disposable = null;
			IDisposable disposable2 = null;
			disposable = border.RegisterParentChangedCallback(1, OnParentChangedCallback);
			void OnParentChangedCallback(object instance, object key, DependencyObjectParentChangedEventArgs args)
			{
				disposable2 = border.RegisterParentChangedCallback(2, OnInnerAddParentChangedCallback);
			}

			firstParent.Child = null;

			disposable?.Dispose();
			disposable2?.Dispose();
		}

		private void OnInnerAddParentChangedCallback(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			Assert.Fail();
		}

		[TestMethod]
		public void When_RemoveParentChanged_OnParentChanged()
		{
			var firstParent = new Microsoft.UI.Xaml.Controls.Border();
			var secondParent = new Microsoft.UI.Xaml.Controls.Border();
			var border = new Microsoft.UI.Xaml.Controls.Border();
			firstParent.Child = border;
			IDisposable disposable = null;
			IDisposable disposable2 = null;
			disposable = border.RegisterParentChangedCallback(1, OnParentChangedCallback);
			disposable2 = border.RegisterParentChangedCallback(2, OnInnerAddParentChangedCallback);
			void OnParentChangedCallback(object instance, object key, DependencyObjectParentChangedEventArgs args)
			{
				disposable2.Dispose();
			}

			Action act = () => firstParent.Child = null;
			act.Should().NotThrow();

			disposable?.Dispose();
			disposable2?.Dispose();
		}

		private void OnInnerRemoveParentChangedCallback(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
		}

		[TestMethod]
		public void When_NullablePropertyBinding()
		{
			var SUT = new Microsoft.UI.Xaml.Controls.Border();
			SUT.Tag = new NullablePropertyOwner() { MyNullable = 42 };

			var o2 = new Microsoft.UI.Xaml.Controls.Border();
			o2.SetBinding(
				Microsoft.UI.Xaml.Controls.Border.TagProperty,
				new Binding()
				{
					Path = "Tag.MyNullable.Value",
					CompiledSource = SUT
				}
			);

			o2.ApplyXBind();

			Assert.AreEqual(42, o2.Tag);
		}

		[TestMethod]
		public void When_NullableStructRecordPropertyBinding()
		{
			var SUT = new Microsoft.UI.Xaml.Controls.Border();
			var propertyOwner = new NullableStructRecordPropertyOwner()
			{
				MyProperty = null
			};
			SUT.Tag = propertyOwner;

			var o2 = new Microsoft.UI.Xaml.Controls.Border();
			o2.SetBinding(
				Microsoft.UI.Xaml.Controls.Border.TagProperty,
				new Binding()
				{
					Path = "Tag.MyProperty.Value.OtherProperty",
					Mode = BindingMode.OneWay,
					CompiledSource = SUT
				}
			);

			o2.ApplyXBind();

			Assert.IsNull(o2.Tag);

			propertyOwner.MyProperty
				= new NullableStructRecordPropertyOwner.MyRecord("42");

			Assert.AreEqual("42", o2.Tag);
		}

		[TestMethod]
		public void When_StructRecordWithValuePropertyBinding()
		{
			var SUT = new Microsoft.UI.Xaml.Controls.Border();
			var propertyOwner = new StructRecordWithValuePropertyOwner()
			{
				MyProperty = new StructRecordWithValuePropertyOwner.MyRecord()
			};
			SUT.Tag = propertyOwner;

			var o2 = new Microsoft.UI.Xaml.Controls.Border();
			o2.SetBinding(
				Microsoft.UI.Xaml.Controls.Border.TagProperty,
				new Binding()
				{
					Path = "Tag.MyProperty.Value",
					Mode = BindingMode.OneWay,
					CompiledSource = SUT
				}
			);

			o2.ApplyXBind();

			Assert.IsNull(o2.Tag);

			propertyOwner.MyProperty
				= new StructRecordWithValuePropertyOwner.MyRecord("42");

			Assert.AreEqual("42", o2.Tag);
		}

		[TestMethod]
		public void When_DataContext_Changing()
		{
			var SUT = new NullablePropertyOwner();
			var datacontext1 = new NullablePropertyOwner { MyNullable = 42 };
			var datacontext2 = new NullablePropertyOwner { MyNullable = 42 };
			var datacontext3 = new NullablePropertyOwner { MyNullable = 84 };

			var values = new List<object>();

			SUT.MyNullableChanged += (snd, evt) => values.Add(evt.NewValue);

			SUT.SetBinding(
				NullablePropertyOwner.MyNullableProperty,
				new Binding()
				{
					Path = "MyNullable"
				}
			);

			SUT.DataContext = datacontext1;
			values.Count.Should().Be(1);
			values.Last().Should().Be(42);

			SUT.DataContext = datacontext2;
			values.Count.Should().Be(1); // Here we ensure we're not receiving a default value, still no changes

			SUT.DataContext = datacontext3;
			values.Count.Should().Be(2);
			values.Last().Should().Be(84);

			SUT.DataContext = null;
			values.Count.Should().Be(3);
			values.Last().Should().Be(null);

			var parent = new Border { Child = SUT };

			parent.DataContext = datacontext1;
			values.Count.Should().Be(3);

			SUT.DataContext = DependencyProperty.UnsetValue; // Propagate the datacontext from parent
			values.Count.Should().Be(4);
			values.Last().Should().Be(42);
		}

		[TestMethod]
		public void When_Set_Within_Style_Application()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var button = new Button();

			using var r = button.RegisterDisposablePropertyChangedCallback(
				Control.PaddingProperty,
				(o, e) =>
				{
					e.BypassesPropagation.Should().BeFalse();
					e.NewPrecedence.Should().Be(DependencyPropertyValuePrecedences.DefaultStyle);
					button.Content = "Frogurt";
				});

			using var _ = new AssertionScope();

			app.HostView.Children.Add(button); // Causes default style to be applied

			button.ReadLocalValue(Button.ContentProperty).Should().Be("Frogurt");
		}

		[TestMethod]
		public void When_Set_Style_Property()
		{
			var style = new Style();
			style.Setters.Add(new Setter(Border.BorderThicknessProperty, 10d));

			var sut = new Border { BorderThickness = new Thickness(100d) };

			// Local value is 100d
			sut.BorderThickness.Should().Be(new Thickness(100d), "Before applying style");

			sut.Style = style;

			// Local value should stay 100d
			sut.BorderThickness.Should().Be(new Thickness(100d), "After applying style");

			sut.ClearValue(Border.BorderThicknessProperty);

			// Local value is cleared, fallback to style value
			sut.BorderThickness.Should().Be(new Thickness(10d), "After removing local value");
		}

		[TestMethod]
		public void When_Set_Style_Property_PrecedenceInContext()
		{
			var style = new Style();
			style.Setters.Add(new Setter(Border.BorderThicknessProperty, 10d));

			var sut = new Border { BorderThickness = new Thickness(100d) };

			using var registration1 = sut.RegisterDisposablePropertyChangedCallback(
				Border.BorderThicknessProperty,
				(dependencyObject, args) =>
				{
					sut.Tag = args.NewPrecedence;
				});

			using var registration2 = sut.RegisterDisposablePropertyChangedCallback(
				FrameworkElement.TagProperty,
				(dependencyObject, args) =>
				{
					args.NewPrecedence.Should().Be(DependencyPropertyValuePrecedences.Local);
					args.NewValue.Should().Be(DependencyPropertyValuePrecedences.ExplicitOrImplicitStyle);
				});

			using var _ = new AssertionScope();

			// Local value is 100d
			sut.BorderThickness.Should().Be(new Thickness(100d), "Before applying style");
			sut.Tag.Should().BeNull("After applying style");

			sut.Style = style;

			// Local value should stay 100d
			sut.BorderThickness.Should().Be(new Thickness(100d), "After applying style");

			sut.ClearValue(Border.BorderThicknessProperty);

			// Local value is cleared, fallback to style value
			sut.BorderThickness.Should().Be(new Thickness(10d), "After removing local value");
			sut.Tag.Should().Be(DependencyPropertyValuePrecedences.ExplicitOrImplicitStyle, "After applying style");

			registration2.Dispose();

			sut.ClearValue(Border.StyleProperty);

			sut.BorderThickness.Should().Be(Thickness.Empty, "After removing style");
			sut.Tag.Should().Be(DependencyPropertyValuePrecedences.DefaultValue, "After removing style");
		}

		[TestMethod]
		public void When_Set_Style_MultipleProperty()
		{
			var style = new Style();
			style.Setters.Add(new Setter(Border.BorderThicknessProperty, 10d));

			var sut = new Border { BorderThickness = new Thickness(100d) };

			// Local value is 100d
			sut.BorderThickness.Should().Be(new Thickness(100d), "Before applying style");

			sut.Style = style;

			// Local value should stay 100d
			sut.BorderThickness.Should().Be(new Thickness(100d), "After applying style");

			sut.ClearValue(Border.BorderThicknessProperty);

			// Local value is cleared, fallback to style value
			sut.BorderThickness.Should().Be(new Thickness(10d), "After removing local value");
		}

		[TestMethod]
		public void When_Set_Style_Property_PrecedenceInContext_No_Local()
		{
			var style = new Style();
			style.Setters.Add(new Setter(Border.BorderThicknessProperty, 10d));

			var sut = new Border { /*Don't set local BorderThickness*/ };

			using var registration1 = sut.RegisterDisposablePropertyChangedCallback(
				Border.BorderThicknessProperty,
				(dependencyObject, args) =>
				{
					sut.Tag = args.NewPrecedence;
				});

			using var registration2 = sut.RegisterDisposablePropertyChangedCallback(
				FrameworkElement.TagProperty,
				(dependencyObject, args) =>
				{
					args.NewPrecedence.Should().Be(DependencyPropertyValuePrecedences.Local);
					args.NewValue.Should().Be(DependencyPropertyValuePrecedences.ExplicitOrImplicitStyle);
				});

			using var _ = new AssertionScope();

			sut.Tag.Should().BeNull("Before applying style");

			sut.Style = style;
		}

		[TestMethod]
		public void When_Set_Style_Property_FromCallback_PrecedenceInContext_No_Local()
		{
			var style = new Style();
			style.Setters.Add(new Setter(Border.BorderThicknessProperty, 10d));

			var sut = new Border { /*Don't set local BorderThickness*/ };

			using var registration1 = sut.RegisterDisposablePropertyChangedCallback(
				FrameworkElement.TagProperty,
				(dependencyObject, args) =>
				{
					args.NewPrecedence.Should().Be(DependencyPropertyValuePrecedences.Local, "Tag property");
					sut.Style = args.NewValue as Style;
				});

			using var registration2 = sut.RegisterDisposablePropertyChangedCallback(
				Border.BorderThicknessProperty,
				(dependencyObject, args) =>
				{
					args.NewPrecedence.Should().Be(DependencyPropertyValuePrecedences.ExplicitOrImplicitStyle);
					args.NewValue.Should().Be(new Thickness(10d));
					sut.Child = new Rectangle();
				});

			using var registration3 = sut.RegisterDisposablePropertyChangedCallback(
				Border.ChildProperty,
				(dependencyObject, args) =>
				{
					args.NewPrecedence.Should().Be(DependencyPropertyValuePrecedences.Local, "ChildProperty");
					args.NewValue.Should().BeOfType<Rectangle>();
				});

			using var _ = new AssertionScope();

			sut.Tag.Should().BeNull("Before applying style");

			sut.Tag = style;
		}

		[TestMethod]
		public void When_Callback_And_Changed_With_Binding()
		{
			var source = new ChangedCallbackOrderElement();
			var target = new ChangedCallbackOrderElement();
			var binding = new Binding()
			{
				Source = target,
				Path = new PropertyPath("Test"),
				Mode = BindingMode.TwoWay
			};
			source.SetBinding(ChangedCallbackOrderElement.TestProperty, binding);

			int order = 0;

			void OnSourceChanged(object s, object e)
			{
				order++;
				Assert.IsTrue(source.Test);
				Assert.IsFalse(target.Test);
				Assert.AreEqual(1, order);
			}

			void OnTargetChanged(object s, object e)
			{
				order++;
				Assert.IsTrue(source.Test);
				Assert.IsTrue(target.Test);
				Assert.AreEqual(2, order);
			}

			void OnTargetCallback(object s, object e)
			{
				order++;
				Assert.IsTrue(source.Test);
				Assert.IsTrue(target.Test);
				Assert.AreEqual(3, order);
			}

			void OnSourceCallback(object s, object e)
			{
				order++;
				Assert.IsTrue(source.Test);
				Assert.IsTrue(target.Test);
				Assert.AreEqual(4, order);
			}

			source.TestCallback += OnSourceCallback;
			target.TestCallback += OnTargetCallback;
			source.TestChanged += OnSourceChanged;
			target.TestChanged += OnTargetChanged;

			source.Test = true;
		}

		[TestMethod]
		public void When_Set_With_Both_Style_And_LocalValue()
		{
			var style = new Style { TargetType = typeof(MyDependencyObject) };
			style.Setters.Add(new Setter { Property = MyDependencyObject.PropAProperty, Value = "StyleValue" });
			style.Setters.Add(new Setter { Property = MyDependencyObject.PropBProperty, Value = "StyleValueForB" });

			var sut = new MyDependencyObject { PropA = "LocalValue" };

			sut.PropA.Should().Be("LocalValue");
			sut.PropB.Should().Be("LocalValue");

			sut.ClearValue(MyDependencyObject.PropBProperty);
			sut.PropB.Should().Be(null);

			sut.Style = style;

			// Here the call back of PropA should not be called, so PropB should be on the ExplicitStyle value
			sut.PropA.Should().Be("LocalValue");
			sut.PropB.Should().Be("StyleValueForB");

			sut.ClearValue(MyDependencyObject.PropBProperty);
			sut.PropB.Should().Be("StyleValueForB");

			sut.ClearValue(MyDependencyObject.PropAProperty);

			sut.PropA.Should().Be("StyleValue");
			sut.PropB.Should().Be("StyleValue");

			sut.ClearValue(MyDependencyObject.PropAProperty);
			sut.PropA.Should().Be("StyleValue");
			sut.ClearValue(MyDependencyObject.PropBProperty);
			sut.PropB.Should().Be("StyleValueForB");

			sut.ClearValue(FrameworkElement.StyleProperty);

			sut.PropA.Should().Be(null);
			sut.PropB.Should().Be(null);
		}

		[TestMethod]
		public void When_DefaultValueOverride()
		{
			var SUT = new MyDependencyObjectWithDefaultValueOverride();
			Assert.AreEqual(42, SUT.GetValue(MyDependencyObjectWithDefaultValueOverride.MyPropertyProperty));
		}

		[TestMethod]
		public void When_FastLocal_Promoted()
		{
			FastLocalTestObject SUT = new();

			WeakReference DoWork()
			{
				object instance = new();
				var wr = new WeakReference(instance);
				SUT.MyProperty = instance;

				// Promote to full dependencypropertydetails stack, which
				// should clear the fast local field.
				SUT.SetValue(FastLocalTestObject.MyPropertyProperty, new object(), DependencyPropertyValuePrecedences.Animations);

				// Clear the precedence to restore the local value
				SUT.ClearValue(FastLocalTestObject.MyPropertyProperty, DependencyPropertyValuePrecedences.Animations);

				// Reset the local value
				SUT.MyProperty = null;
				instance = null;

				return wr;
			}

			var wr = DoWork();

			int rounds = 0;
			do
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
				Thread.Sleep(10);
			} while (wr.IsAlive && rounds++ < 3);

			Assert.IsFalse(wr.IsAlive);
		}

		private class MyDependencyObject : FrameworkElement
		{
			internal static readonly DependencyProperty PropAProperty = DependencyProperty.Register(
				"PropA", typeof(string), typeof(MyDependencyObject), new PropertyMetadata(default(string), OnPropAChanged));

			private static void OnPropAChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
			{
				(d as MyDependencyObject).PropB = e.NewValue as string; // should assign using a kind a "local" precedence
			}

			internal string PropA
			{
				get { return (string)GetValue(PropAProperty); }
				set { SetValue(PropAProperty, value); }
			}

			public static readonly DependencyProperty PropBProperty = DependencyProperty.Register(
				"PropB", typeof(string), typeof(MyDependencyObject), new PropertyMetadata(default(string)));

			public string PropB
			{
				get { return (string)GetValue(PropBProperty); }
				set { SetValue(PropBProperty, value); }
			}
		}

	}

	#region DependencyObjects

	partial class MockDependencyObject : DependencyObject
	{

	}

	partial class MockDependencyObject2 : MockDependencyObject
	{

	}

	partial class SimpleDependencyObject1 : DependencyObject
	{
		public SimpleDependencyObject1() { }

		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(string), typeof(SimpleDependencyObject1),
				new PropertyMetadata(
					"default1",
					(s, e) =>
					{
						(s as SimpleDependencyObject1).PropertyChangedCallbacks.Add("changed1: " + e.NewValue);
						(s as SimpleDependencyObject1).ChangedCallbackCount++;
					}
				)
			);

		public List<string> PropertyChangedCallbacks = new List<string>();
		public int ChangedCallbackCount { get; set; } = 0;
	}


	partial class MyDependencyObject1 : DependencyObject
	{
		public MyDependencyObject1() { }

		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(string), typeof(MyDependencyObject1),
				new PropertyMetadata(
					"default1",
					(s, e) => { (s as MyDependencyObject1).PropertyChangedCallbacks.Add("changed1: " + e.NewValue); },
					(s, baseValue, _) => { (s as MyDependencyObject1).CoerceValueCallbackCount++; return "coercion1: " + baseValue; }
				)
			);

		public List<string> PropertyChangedCallbacks = new List<string>();
		public int CoerceValueCallbackCount { get; set; } = 0;
	}

	partial class NullablePropertyOwner : FrameworkElement
	{

		#region MyNullable DependencyProperty

		public int? MyNullable
		{
			get { return (int?)GetValue(MyNullableProperty); }
			set { SetValue(MyNullableProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyNullable.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyNullableProperty =
			DependencyProperty.Register(
				"MyNullable",
				typeof(int?),
				typeof(NullablePropertyOwner),
				new PropertyMetadata(
					null,
					(s, e) => ((NullablePropertyOwner)s)?.OnMyNullableChanged(e)
				)
			);


		private void OnMyNullableChanged(DependencyPropertyChangedEventArgs e)
		{
			MyNullableChanged?.Invoke(this, e);
		}

		internal event EventHandler<DependencyPropertyChangedEventArgs> MyNullableChanged;

		#endregion

	}

	partial class NullableStructRecordPropertyOwner : FrameworkElement
	{
		public MyRecord? MyProperty
		{
			get => (MyRecord?)GetValue(MyPropertyProperty);
			set => SetValue(MyPropertyProperty, value);
		}

		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register(
				nameof(MyProperty),
				typeof(MyRecord?),
				typeof(NullableStructRecordPropertyOwner),
				new PropertyMetadata(null, DataReady));

		private static void DataReady(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{

		}

		public readonly record struct MyRecord(string OtherProperty);
	}

	partial class StructRecordWithValuePropertyOwner : FrameworkElement
	{
		public MyRecord MyProperty
		{
			get => (MyRecord)GetValue(MyPropertyProperty);
			set => SetValue(MyPropertyProperty, value);
		}

		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register(
				nameof(MyProperty),
				typeof(MyRecord),
				typeof(StructRecordWithValuePropertyOwner),
				new PropertyMetadata(null, DataReady));

		private static void DataReady(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{

		}

		public readonly record struct MyRecord(string Value);
	}

	partial class MyDependencyObjectWithDefaultValueOverride : FrameworkElement
	{
		public MyDependencyObjectWithDefaultValueOverride()
		{
		}

		internal override bool GetDefaultValue2(DependencyProperty property, out object defaultValue)
		{
			if (property == MyPropertyProperty)
			{
				defaultValue = 42;

				return true;
			}

			return base.GetDefaultValue2(property, out defaultValue);
		}

		public int MyProperty
		{
			get { return (int)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(int), typeof(MyDependencyObjectWithDefaultValueOverride), new PropertyMetadata(0));

	}

	internal class ChangedCallbackOrderElement : FrameworkElement
	{
		public ChangedCallbackOrderElement()
		{
			this.RegisterPropertyChangedCallback(TestProperty, OnTestCallback);
		}

		public event EventHandler TestChanged;
		public event EventHandler TestCallback;

		public bool Test
		{
			get => (bool)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static readonly DependencyProperty TestProperty =
			DependencyProperty.Register(
				nameof(Test),
				typeof(bool),
				typeof(ChangedCallbackOrderElement),
				new PropertyMetadata(false, OnTestChanged));

		private static void OnTestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var custom = (ChangedCallbackOrderElement)d;
			custom.TestChanged?.Invoke(custom, null);
		}

		private void OnTestCallback(DependencyObject sender, DependencyProperty dp)
		{
			TestCallback?.Invoke(this, null);
		}
	}

	internal partial class FastLocalTestObject : DependencyObject
	{
		public object MyProperty
		{
			get { return (object)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(object), typeof(FastLocalTestObject), new PropertyMetadata(null));
	}

	#endregion
}
