using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.BinderTests;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.ApplicationModel.VoiceCommands;

namespace Uno.UI.Tests.DependencyPropertyTests
{
	[TestClass]
	public partial class Given_DependencyProperty
	{
		[TestMethod]
		public void When_DefaultValueProvider_Registered_Provides_Value()
		{
			var test = new DefaultValueProviderSample();

			var value = test.GetPrecedenceSpecificValue(DefaultValueProviderSample.TestProperty, DependencyPropertyValuePrecedences.DefaultValue);
			Assert.AreEqual(3, value);
		}

		[TestMethod]
		public void When_DefaultValueProvider_Registered_Other_Property_Not_Affected()
		{
			var test = new DefaultValueProviderSample();

			var value = test.GetPrecedenceSpecificValue(DefaultValueProviderSample.OtherProperty, DependencyPropertyValuePrecedences.DefaultValue);
			Assert.AreEqual(0, value);
		}

		[TestMethod]
		public void When_DefaultValueProvider_Multiple_Registered_Overwrite()
		{
			var test = new InheritedDefaultValueProviderSample();

			var value = test.GetPrecedenceSpecificValue(DefaultValueProviderSample.TestProperty, DependencyPropertyValuePrecedences.DefaultValue);
			Assert.AreEqual(17, value);
		}

		[TestMethod]
		public void When_DefaultValueProvider_Multiple_Registered_Unaffected()
		{
			var test = new InheritedDefaultValueProviderSample2();

			var value = test.GetPrecedenceSpecificValue(DefaultValueProviderSample.TestProperty, DependencyPropertyValuePrecedences.DefaultValue);
			Assert.AreEqual(3, value); // GetDefaultValue should apply
		}

		[TestMethod]
		public void When_GetDefaultValue()
		{
			var expected = 42;
			var defaultValueTest = new DefaultValueTest();
			Assert.AreEqual(expected, defaultValueTest.GetPrecedenceSpecificValue(DefaultValueTest.TestValueProperty, DependencyPropertyValuePrecedences.DefaultValue));
			Assert.AreEqual(expected, defaultValueTest.GetValue(DefaultValueTest.TestValueProperty));
			Assert.AreEqual(expected, (defaultValueTest as IDependencyObjectStoreProvider).Store.GetPropertyDetails(DefaultValueTest.TestValueProperty).GetDefaultValue());
			Assert.AreEqual(expected, defaultValueTest.TestValue);
		}

		[TestMethod]
		public void When_SetDefaultValue()
		{
			var expected = 24;
			var defaultValueTest = new DefaultValueTest();
			((IDependencyObjectStoreProvider)defaultValueTest).Store.GetPropertyDetails(DefaultValueTest.TestValueProperty).SetDefaultValue(expected);
			Assert.AreEqual(expected, defaultValueTest.GetPrecedenceSpecificValue(DefaultValueTest.TestValueProperty, DependencyPropertyValuePrecedences.DefaultValue));
			Assert.AreEqual(expected, defaultValueTest.GetValue(DefaultValueTest.TestValueProperty));
			Assert.AreEqual(expected, ((IDependencyObjectStoreProvider)defaultValueTest).Store.GetPropertyDetails(DefaultValueTest.TestValueProperty).GetDefaultValue());
			Assert.AreEqual(expected, defaultValueTest.TestValue);
		}

		[TestMethod]
		public void When_SetParentInGetDefaultValue()
		{
			DPCollectionDefaultValue value = new DPCollectionDefaultValue();
			// The issue happens inside TryGetPropertyDetails(DependencyProperty property, bool forceCreate)
			// where it initially starts off with 16 entries, but setting the child collection
			// as a child makes it expand to 32 entries, which caused the already-in-progress code to lose reference.
			value.GetValue(DPCollectionDefaultValue.ChildCollectionProperty);
		}
	}

	internal partial class DefaultValueTest : UIElement
	{
		public int TestValue
		{
			get { return (int)GetValue(TestValueProperty); }
			set { SetValue(TestValueProperty, value); }
		}

		public static DependencyProperty TestValueProperty { get; } =
			DependencyProperty.Register("TestValue", typeof(int), typeof(DefaultValueTest), new PropertyMetadata(42));
	}

	internal partial class InheritedDefaultValueProviderSample : DefaultValueProviderSample
	{
		internal override bool GetDefaultValue2(DependencyProperty property, out object value)
		{
			if (property == DefaultValueProviderSample.TestProperty)
			{
				value = 17;
				return true;
			}

			return base.GetDefaultValue2(property, out value);
		}
	}

	internal partial class InheritedDefaultValueProviderSample2 : DefaultValueProviderSample
	{
		internal override bool GetDefaultValue2(DependencyProperty property, out object value)
		{
			if (property == DefaultValueProviderSample.OtherProperty)
			{
				value = 17;
				return true;
			}

			return base.GetDefaultValue2(property, out value);
		}
	}

	internal partial class DPCollectionDefaultValue : UIElement
	{
		public IList<DependencyObject> ChildCollection
		{
			get { return (IList<DependencyObject>)GetValue(ChildCollectionProperty); }
			set { SetValue(ChildCollectionProperty, value); }
		}

		public static readonly DependencyProperty ChildCollectionProperty =
			DependencyProperty.Register("ChildCollection", typeof(IList<DependencyObject>), typeof(DPCollectionDefaultValue), new PropertyMetadata(null));

		internal override bool GetDefaultValue2(DependencyProperty property, out object defaultValue)
		{
			if (property == ChildCollectionProperty)
			{
				defaultValue = new DependencyObjectCollection();
				defaultValue.SetParent(this);
				return true;
			}

			return base.GetDefaultValue2(property, out defaultValue);
		}
	}

	internal partial class DefaultValueProviderSample : UIElement
	{
		public DefaultValueProviderSample()
		{

		}

		internal override bool GetDefaultValue2(DependencyProperty property, out object value)
		{
			if (property == DefaultValueProviderSample.TestProperty)
			{
				value = 3;
				return true;
			}

			return base.GetDefaultValue2(property, out value);
		}

		public int Test
		{
			get => (int)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(int), typeof(DefaultValueProviderSample), new PropertyMetadata(0));

		public int Other
		{
			get => (int)GetValue(OtherProperty);
			set => SetValue(OtherProperty, value);
		}

		public static readonly DependencyProperty OtherProperty =
			DependencyProperty.Register(nameof(Other), typeof(int), typeof(DefaultValueProviderSample), new PropertyMetadata(0));
	}
}
