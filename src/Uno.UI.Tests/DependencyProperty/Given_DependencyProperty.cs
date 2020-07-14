using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using System.Threading;
using Windows.UI.Xaml.Controls;
using FluentAssertions;

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
			var testProperty = DependencyProperty.Register(nameof(When_GetDefaultValue), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

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
			var testProperty = DependencyProperty.Register(nameof(When_GetDefaultValue_And_PropertyType_Is_ValueType_And_DefaultValue_Is_Null), typeof(int), typeof(MockDependencyObject), new FrameworkPropertyMetadata(null));

			Assert.AreEqual(default(int), SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_GetDefaultValue_And_PropertyType_Is_ValueType_And_DefaultValue_Is_Different_Type()
		{
			var defaultValue = "test";

			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_GetDefaultValue_And_PropertyType_Is_ValueType_And_DefaultValue_Is_Different_Type), typeof(int), typeof(MockDependencyObject), new FrameworkPropertyMetadata(defaultValue));

			Assert.AreEqual(defaultValue, SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_SetValue_Registration_NotRaised()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_Registration_NotRaised), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

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
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_and_DefaultValue_Registration_Raised), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

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
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_and_DefaultValue_Registration_RaisedTwice), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

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
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_And_DefaultValue_Then_StaticRegistration_NotRaised), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42", cb));

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
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_And_NoDefaultValue_Then_StaticRegistration_NotRaised), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata(null, cb));

			Assert.AreEqual(null, SUT.GetValue(testProperty));
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
			var testProperty = DependencyProperty.Register(nameof(When_SetValue_And_DefaultValue_Then_StaticRegistration_Raised), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42", cb));

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
						Assert.AreEqual(null, e.NewValue);
						break;
				}
			};

			var testProperty = DependencyProperty.Register(nameof(When_SetValue_And_DefaultValue_Then_StaticRegistration_RaisedMultiple), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42", cb));

			SUT.SetValue(testProperty, "test");
			Assert.AreEqual("test", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, "test2");
			Assert.AreEqual("test2", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, null);
			Assert.AreEqual(null, SUT.GetValue(testProperty));

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

			var testProperty = DependencyProperty.Register(nameof(When_SetValue_Integer_And_SameValue_Then_RaisedOnce), typeof(int), typeof(MockDependencyObject), new FrameworkPropertyMetadata(0, cb));

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

			var testProperty = DependencyProperty.Register(nameof(When_SetValue_Thickness_And_SameValue_Then_RaisedOnce), typeof(Thickness), typeof(MockDependencyObject), new FrameworkPropertyMetadata(Thickness.Empty, cb));

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
						Assert.AreEqual(null, e.OldValue);
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

			var testProperty = DependencyProperty.Register(nameof(When_SetValue_Reference_And_SameValue_Then_RaisedOnce), typeof(object), typeof(MockDependencyObject), new FrameworkPropertyMetadata(null, cb));

			SUT.SetValue(testProperty, o1);
			Assert.AreEqual(o1, SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, o1);
			Assert.AreEqual(o1, SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, o2);
			Assert.AreEqual(o2, SUT.GetValue(testProperty));

			Assert.AreEqual(2, raisedCount);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void When_Property_RegisterTwice_then_Fail()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Property_RegisterTwice_then_Fail), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));
			var testProperty2 = DependencyProperty.Register(nameof(When_Property_RegisterTwice_then_Fail), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));
		}

		[TestMethod]
		public void When_Local_Value_Cleared_Then_Default_Returned()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Local_Value_Cleared_Then_Default_Returned), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

			SUT.SetValue(testProperty, "Not42");

			Assert.AreEqual("Not42", SUT.GetValue(testProperty));

			SUT.ClearValue(testProperty);

			Assert.AreEqual("42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_Two_Precedences_Set_Then_Only_Highest_Returned()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Two_Precedences_Set_Then_Only_Highest_Returned), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

			SUT.SetValue(testProperty, "Not42");
			SUT.SetValue(testProperty, "ALowPriorityValue", DependencyPropertyValuePrecedences.ImplicitStyle);

			Assert.AreEqual("Not42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		public void When_Animating_And_Animation_Is_Done_Then_Returns_Local()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Animating_And_Animation_Is_Done_Then_Returns_Local), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

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
			var testProperty = DependencyProperty.Register(nameof(When_Setting_UnsetValue_Then_Property_Reverts_To_Default), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

			SUT.SetValue(testProperty, "Not42");

			Assert.AreEqual("Not42", SUT.GetValue(testProperty));

			SUT.SetValue(testProperty, DependencyProperty.UnsetValue);

			Assert.AreEqual("42", SUT.GetValue(testProperty));
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void When_Setting_UnsetValue_On_DefaultValue_Then_Fails()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Setting_UnsetValue_On_DefaultValue_Then_Fails), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

			SUT.SetValue(testProperty, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.DefaultValue);
		}

		[TestMethod]
		public void When_Setting_Value_Then_Current_Highest_Is_Local()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(nameof(When_Setting_Value_Then_Current_Highest_Is_Local), typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata("42"));

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
				new FrameworkPropertyMetadata(defaultValue)
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
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(
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
		public void When_CoerceValueCallback_ReturnNull_DefaultValue_Not_Coerced()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_CoerceValueCallback_ReturnNull_DefaultValue_Not_Coerced),
				typeof(string),
				typeof(MockDependencyObject),
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(
					defaultValue,
					null,
					ReturnNull
				)
			);

			SUT.SetValue(testProperty, baseValue);

			var actualValue = SUT.GetValue(testProperty);
			var localValue = SUT.ReadLocalValue(testProperty);

			Assert.AreEqual(null, actualValue);
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
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(
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
				new FrameworkPropertyMetadata(
					null,
					null,
					Now
				)
			);

			var defaultValue = (DateTime)SUT.GetValue(testProperty);

			SUT.SetValue(testProperty, null);

			var actualValue = (DateTime)SUT.GetValue(testProperty);

			var later = DateTime.Now.AddMinutes(1);

			Assert.IsNotNull(actualValue);
			Assert.IsNotNull(defaultValue);
			Assert.IsTrue(defaultValue < actualValue && actualValue < later);
		}

		#region CoerceValueCallbacks

		private object PreventSet(object dependencyObject, object baseValue)
		{
			return DependencyProperty.UnsetValue;
		}

		private object DoNothing(object dependencyObject, object baseValue)
		{
			return baseValue;
		}

		private object ReturnNull(object dependencyObject, object baseValue)
		{
			return null;
		}

		private object AbsoluteInteger(object dependencyObject, object baseValue)
		{
			return Math.Abs((int)baseValue);
		}

		private object IgnoreNegative(object dependencyObject, object baseValue)
		{
			return (int)baseValue < 0
				? DependencyProperty.UnsetValue
				: baseValue;
		}

		private object StringTake10(object dependencyObject, object baseValue)
		{
			return new string(((string)baseValue).Take(10).ToArray());
		}

		private object Now(object dependencyObject, object baseValue)
		{
			return DateTime.Now;
		}

		private static object _customCoercion;
		private object Custom(object dependencyObject, object baseValue)
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
		[ExpectedException(typeof(ArgumentException))]
		public void When_OverrideMetadata_With_Metadata_Is_Not_Derived_From_BaseMetadata_Then_Fail()
		{
			var testProperty = DependencyProperty.Register(
				nameof(When_OverrideMetadata_With_Metadata_Is_Not_Derived_From_BaseMetadata_Then_Fail),
				typeof(string),
				typeof(MockDependencyObject),
				new FrameworkPropertyMetadata(null)
			);

			testProperty.OverrideMetadata(typeof(MockDependencyObject2), new FrameworkPropertyMetadata(null));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void When_OverrideMetadata_With_ForType_Is_OwnerType_Then_Fail()
		{
			var testProperty = DependencyProperty.Register(
				nameof(When_OverrideMetadata_With_ForType_Is_OwnerType_Then_Fail),
				typeof(string),
				typeof(MockDependencyObject),
				null
			);

			testProperty.OverrideMetadata(typeof(MockDependencyObject), new FrameworkPropertyMetadata("test"));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void When_OverrideMetadata_With_Same_ForType_Twice_Then_Fail()
		{
			MyDependencyObject1.MyPropertyProperty.OverrideMetadata(typeof(MyDependencyObject2), new FrameworkPropertyMetadata("test"));
		}

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
		[ExpectedException(typeof(ArgumentException))]
		public void When_SetValue_With_Coercion_Precedence_Then_Fail()
		{
			var SUT = new MockDependencyObject();
			var testProperty = DependencyProperty.Register(
				nameof(When_SetValue_With_Coercion_Precedence_Then_Fail),
				typeof(string),
				typeof(MockDependencyObject),
				null
			);

			SUT.SetValue(testProperty, "test", DependencyPropertyValuePrecedences.Coercion);
		}

		[TestMethod]
		public void When_OverrideMetadata_DefaultValue()
		{
			var SUT1 = new MyDependencyObject1();
			var SUT2 = new MyDependencyObject2();
			var SUT3 = new MyDependencyObject3();

			Assert.AreEqual("default1", MyDependencyObject1.MyPropertyProperty.GetMetadata(typeof(MyDependencyObject1)).DefaultValue);
			Assert.AreEqual("default2", MyDependencyObject1.MyPropertyProperty.GetMetadata(typeof(MyDependencyObject2)).DefaultValue);
			Assert.AreEqual("default3", MyDependencyObject1.MyPropertyProperty.GetMetadata(typeof(MyDependencyObject3)).DefaultValue);

			Assert.AreEqual("default1", SUT1.GetValue(MyDependencyObject1.MyPropertyProperty));
			Assert.AreEqual("default2", SUT2.GetValue(MyDependencyObject1.MyPropertyProperty));
			Assert.AreEqual("default3", SUT3.GetValue(MyDependencyObject1.MyPropertyProperty));
		}

		[TestMethod]
		public void When_OverrideMetadata_CoerceValueCallback()
		{
			var SUT1 = new MyDependencyObject1();
			var SUT2 = new MyDependencyObject2();
			var SUT3 = new MyDependencyObject3();

			// +1 CoerceValueCallback (1)
			SUT1.SetValue(MyDependencyObject1.MyPropertyProperty, "A");
			SUT2.SetValue(MyDependencyObject1.MyPropertyProperty, "A");
			SUT3.SetValue(MyDependencyObject1.MyPropertyProperty, "A");

			// +1 CoerceValueCallback (2)
			SUT1.SetValue(MyDependencyObject1.MyPropertyProperty, "A");
			SUT2.SetValue(MyDependencyObject1.MyPropertyProperty, "A");
			SUT3.SetValue(MyDependencyObject1.MyPropertyProperty, "A");

			// +1 CoerceValueCallback (3)
			SUT1.SetValue(MyDependencyObject1.MyPropertyProperty, "B");
			SUT2.SetValue(MyDependencyObject1.MyPropertyProperty, "B");
			SUT3.SetValue(MyDependencyObject1.MyPropertyProperty, "B");

			// +1 CoerceValueCallback (4)
			SUT1.CoerceValue(MyDependencyObject1.MyPropertyProperty);
			SUT2.CoerceValue(MyDependencyObject1.MyPropertyProperty);
			SUT3.CoerceValue(MyDependencyObject1.MyPropertyProperty);

			Assert.AreEqual(4, SUT1.CoerceValueCallbackCount);
			Assert.AreEqual(4, SUT2.CoerceValueCallbackCount);
			Assert.AreEqual(4, SUT3.CoerceValueCallbackCount);
		}

		[TestMethod]
		public void When_SetValue_Inheritance_And_CoerceValue_Then_GetValue_Local_Is_UnsetValue()
		{
			var SUT = new MyDependencyObject1();

			SUT.SetValue(MyDependencyObject1.MyPropertyProperty, "value", DependencyPropertyValuePrecedences.Inheritance);
			SUT.CoerceValue(MyDependencyObject1.MyPropertyProperty);

			Assert.AreEqual(DependencyProperty.UnsetValue, SUT.GetValue(MyDependencyObject1.MyPropertyProperty, DependencyPropertyValuePrecedences.Local));
		}

		[TestMethod]
		public void When_OverrideMetadata_PropertyChangedCallback()
		{
			var SUT1 = new MyDependencyObject1();
			var SUT2 = new MyDependencyObject2();
			var SUT3 = new MyDependencyObject3();

			SUT1.SetValue(MyDependencyObject1.MyPropertyProperty, "A"); // +1 PropertyChangedCallback (1)
			SUT2.SetValue(MyDependencyObject1.MyPropertyProperty, "A"); // +2 PropertyChangedCallback (2)
			SUT3.SetValue(MyDependencyObject1.MyPropertyProperty, "A"); // +3 PropertyChangedCallback (3)

			SUT1.SetValue(MyDependencyObject1.MyPropertyProperty, "A"); // +0 PropertyChangedCallback (1)
			SUT2.SetValue(MyDependencyObject1.MyPropertyProperty, "A"); // +0 PropertyChangedCallback (2)
			SUT3.SetValue(MyDependencyObject1.MyPropertyProperty, "A"); // +0 PropertyChangedCallback (3)

			SUT1.SetValue(MyDependencyObject1.MyPropertyProperty, "B"); // +1 PropertyChangedCallback (2)
			SUT2.SetValue(MyDependencyObject1.MyPropertyProperty, "B"); // +2 PropertyChangedCallback (4)
			SUT3.SetValue(MyDependencyObject1.MyPropertyProperty, "B"); // +3 PropertyChangedCallback (6)

			SUT1.CoerceValue(MyDependencyObject1.MyPropertyProperty); // +0 PropertyChangedCallback (2)
			SUT1.CoerceValue(MyDependencyObject1.MyPropertyProperty); // +0 PropertyChangedCallback (4)
			SUT1.CoerceValue(MyDependencyObject1.MyPropertyProperty); // +0 PropertyChangedCallback (6)

			var propertyChangedCallbacks1 = new[]
			{
				"changed1: coercion1: A",
				"changed1: coercion1: B",
			};

			var propertyChangedCallbacks2 = new[]
			{
				"changed1: coercion2: A",
				"changed2: coercion2: A",
				"changed1: coercion2: B",
				"changed2: coercion2: B",
			};

			var propertyChangedCallbacks3 = new[]
			{
				"changed1: coercion3: A",
				"changed2: coercion3: A",
				"changed3: coercion3: A",
				"changed1: coercion3: B",
				"changed2: coercion3: B",
				"changed3: coercion3: B",
			};

			Assert.IsTrue(SUT1.PropertyChangedCallbacks.SequenceEqual(propertyChangedCallbacks1));
			Assert.IsTrue(SUT2.PropertyChangedCallbacks.SequenceEqual(propertyChangedCallbacks2));
			Assert.IsTrue(SUT3.PropertyChangedCallbacks.SequenceEqual(propertyChangedCallbacks3));
		}

		[TestMethod]
		public void When_OverrideMetadata_FrameworkPropertyMetadata_Options()
		{
			var SUT1 = new MyDependencyObject1();
			var SUT2 = new MyDependencyObject2();
			var SUT3 = new MyDependencyObject3();

			var metadata1 = MyDependencyObject1.MyPropertyProperty.GetMetadata(typeof(MyDependencyObject1));
			var metadata2 = MyDependencyObject1.MyPropertyProperty.GetMetadata(typeof(MyDependencyObject2));
			var metadata3 = MyDependencyObject1.MyPropertyProperty.GetMetadata(typeof(MyDependencyObject3));

			Assert.IsNotInstanceOfType(metadata1, typeof(FrameworkPropertyMetadata));
			Assert.IsInstanceOfType(metadata2, typeof(FrameworkPropertyMetadata));
			Assert.IsInstanceOfType(metadata3, typeof(FrameworkPropertyMetadata));

			Assert.AreEqual(FrameworkPropertyMetadataOptions.Inherits, (metadata2 as FrameworkPropertyMetadata).Options);
			Assert.AreEqual(FrameworkPropertyMetadataOptions.Inherits, (metadata3 as FrameworkPropertyMetadata).Options);
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
				new FrameworkPropertyMetadata(defaultValue)
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
				new FrameworkPropertyMetadata(defaultValue)
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
				new FrameworkPropertyMetadata(defaultValue)
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
				(s, e) => {
					invocations++;

					registration2 = SUT.RegisterPropertyChangedCallback(
						SimpleDependencyObject1.MyPropertyProperty,
						(s2, e2) => {
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

			var dp1 = DependencyProperty.Register("TestProperty", typeof(string), typeof(MockDependencyObject), new FrameworkPropertyMetadata(null));

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

			property1 = DependencyProperty.Register("Property1", typeof(int), typeof(MockDependencyObject), new FrameworkPropertyMetadata(0, OnProperty1Changed));
			property2 = DependencyProperty.Register("Property2", typeof(int), typeof(MockDependencyObject), new FrameworkPropertyMetadata(0, OnProperty2Changed));
			property3 = DependencyProperty.Register("Property3", typeof(int), typeof(MockDependencyObject), new FrameworkPropertyMetadata(0, OnProperty3Changed));

			SUT.SetValue(property1, 1);

			Assert.AreEqual(1, SUT.GetValue(property1));
			Assert.AreEqual(2, SUT.GetValue(property2));
			Assert.AreEqual(3, SUT.GetValue(property3));
		}

		[TestMethod]
		public void When_NullablePropertyBinding()
		{
			var SUT = new Windows.UI.Xaml.Controls.Border();
			SUT.Tag = new NullablePropertyOwner() { MyNullable = 42 };

			var o2 = new Windows.UI.Xaml.Controls.Border();
			o2.SetBinding(
				Windows.UI.Xaml.Controls.Border.TagProperty,
				new Binding() {
					Path = "Tag.MyNullable.Value",
					CompiledSource = SUT
				}
			);

			o2.ApplyCompiledBindings();

			Assert.AreEqual(42, o2.Tag);
		}

		[TestMethod]
		public void When_DataContext_Changing()
		{
			var SUT = new NullablePropertyOwner();
			var datacontext1 = new NullablePropertyOwner {MyNullable = 42};
			var datacontext2 = new NullablePropertyOwner {MyNullable = 42};
			var datacontext3 = new NullablePropertyOwner {MyNullable = 84};

			var changes = new List<DependencyPropertyChangedEventArgs>();

			SUT.MyNullableChanged += (snd, evt) => changes.Add(evt);

			SUT.SetBinding(
				NullablePropertyOwner.MyNullableProperty,
				new Binding() {
					Path = "MyNullable"
				}
			);

			SUT.DataContext = datacontext1;
			changes.Count.Should().Be(1);
			changes.Last().NewValue.Should().Be(42);

			SUT.DataContext = datacontext2;
			changes.Count.Should().Be(1); // Here we ensure we're not receiving a default value, still no changes

			SUT.DataContext = datacontext3;
			changes.Count.Should().Be(2);
			changes.Last().NewValue.Should().Be(84);

			SUT.DataContext = null;
			changes.Count.Should().Be(3);
			changes.Last().NewValue.Should().Be(null);

			var parent = new Border {Child = SUT};

			parent.DataContext = datacontext1;
			changes.Count.Should().Be(3);

			SUT.DataContext = DependencyProperty.UnsetValue; // Propagate the datacontext from parent
			changes.Count.Should().Be(4);
			changes.Last().NewValue.Should().Be(42);
		}

		[TestMethod]
		public void When_Set_Within_Style_Application()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var button = new Button();
			button.RegisterPropertyChangedCallback(Button.PaddingProperty, (o, e) =>
			{
				button.Content = "Frogurt";
			});

			app.HostView.Children.Add(button); // Causes default style to be applied

			var localContent = button.ReadLocalValue(Button.ContentProperty);
			Assert.AreEqual("Frogurt", localContent);
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
				new FrameworkPropertyMetadata(
					"default1",
					(s, e) => {
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
				new FrameworkPropertyMetadata(
					"default1",
					(s, e) => { (s as MyDependencyObject1).PropertyChangedCallbacks.Add("changed1: " + e.NewValue); },
					(s, baseValue) => { (s as MyDependencyObject1).CoerceValueCallbackCount++; return "coercion1: " + baseValue; }

				)
			);

		public List<string> PropertyChangedCallbacks = new List<string>();
		public int CoerceValueCallbackCount { get; set; } = 0;
	}

	partial class MyDependencyObject2 : MyDependencyObject1
	{
		static MyDependencyObject2()
		{
			var metadata = new FrameworkPropertyMetadata(
				"default2",
				FrameworkPropertyMetadataOptions.Inherits,
				(s, e) => { (s as MyDependencyObject1).PropertyChangedCallbacks.Add("changed2: " + e.NewValue); },
				(s, baseValue) => { (s as MyDependencyObject1).CoerceValueCallbackCount++; return "coercion2: " + baseValue; }
			);

			MyPropertyProperty.OverrideMetadata(typeof(MyDependencyObject2), metadata);
		}

		public MyDependencyObject2() { }
	}

	partial class MyDependencyObject3 : MyDependencyObject2
	{
		static MyDependencyObject3()
		{
			var metadata = new FrameworkPropertyMetadata(
				"default3",
				(s, e) => { (s as MyDependencyObject1).PropertyChangedCallbacks.Add("changed3: " + e.NewValue); },
				(s, baseValue) => { (s as MyDependencyObject1).CoerceValueCallbackCount++; return "coercion3: " + baseValue; }
			);

			MyPropertyProperty.OverrideMetadata(typeof(MyDependencyObject3), metadata);
		}

		public MyDependencyObject3() { }
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
				new FrameworkPropertyMetadata(
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

	#endregion
}
