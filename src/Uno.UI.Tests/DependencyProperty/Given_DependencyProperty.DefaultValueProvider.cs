using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.BinderTests;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.DependencyPropertyTests
{
	[TestClass]
	public partial class Given_DependencyProperty
	{
		[TestMethod]
		public void When_DefaultValueProvider_Registered_Provides_Value()
		{
			bool GetDefaultValue(DependencyProperty property, out object value)
			{
				if (property == DefaultValueProviderSample.TestProperty)
				{
					value = 3;
					return true;
				}

				value = null;
				return false;
			}

			var test = new DefaultValueProviderSample();
			test.RegisterDefaultValueProvider(GetDefaultValue);

			var value = test.GetPrecedenceSpecificValue(DefaultValueProviderSample.TestProperty, DependencyPropertyValuePrecedences.DefaultValue);
			Assert.AreEqual(3, value);
		}

		[TestMethod]
		public void When_DefaultValueProvider_Registered_Other_Property_Not_Affected()
		{
			bool GetDefaultValue(DependencyProperty property, out object value)
			{
				if (property == DefaultValueProviderSample.TestProperty)
				{
					value = 3;
					return true;
				}

				value = 42; // Set arbitrary to verify false has effect
				return false;
			}

			var test = new DefaultValueProviderSample();
			test.RegisterDefaultValueProvider(GetDefaultValue);

			var value = test.GetPrecedenceSpecificValue(DefaultValueProviderSample.OtherProperty, DependencyPropertyValuePrecedences.DefaultValue);
			Assert.AreEqual(0, value);
		}

		[TestMethod]
		public void When_DefaultValueProvider_Multiple_Registered_Overwrite()
		{
			bool GetDefaultValue(DependencyProperty property, out object value)
			{
				if (property == DefaultValueProviderSample.TestProperty)
				{
					value = 3;
					return true;
				}

				value = null;
				return false;
			}

			bool GetDefaultValue2(DependencyProperty property, out object value)
			{
				if (property == DefaultValueProviderSample.TestProperty)
				{
					value = 17;
					return true;
				}

				value = null;
				return false;
			}

			var test = new DefaultValueProviderSample();
			test.RegisterDefaultValueProvider(GetDefaultValue);
			test.RegisterDefaultValueProvider(GetDefaultValue2);

			var value = test.GetPrecedenceSpecificValue(DefaultValueProviderSample.TestProperty, DependencyPropertyValuePrecedences.DefaultValue);
			Assert.AreEqual(17, value);
		}

		[TestMethod]
		public void When_DefaultValueProvider_Multiple_Registered_Unaffected()
		{
			bool GetDefaultValue(DependencyProperty property, out object value)
			{
				if (property == DefaultValueProviderSample.TestProperty)
				{
					value = 3;
					return true;
				}

				value = null;
				return false;
			}

			bool GetDefaultValue2(DependencyProperty property, out object value)
			{
				if (property == DefaultValueProviderSample.OtherProperty)
				{
					value = 17;
					return true;
				}

				value = 42;
				return false;
			}

			var test = new DefaultValueProviderSample();
			test.RegisterDefaultValueProvider(GetDefaultValue);
			test.RegisterDefaultValueProvider(GetDefaultValue2);

			var value = test.GetPrecedenceSpecificValue(DefaultValueProviderSample.TestProperty, DependencyPropertyValuePrecedences.DefaultValue);
			Assert.AreEqual(3, value); // GetDefaultValue should apply
		}
	}

	internal partial class DefaultValueProviderSample : MockDependencyObject
	{
		public DefaultValueProviderSample()
		{
			
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
