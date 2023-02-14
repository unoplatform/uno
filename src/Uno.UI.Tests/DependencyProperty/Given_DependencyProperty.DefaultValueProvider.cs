using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.BinderTests;
using Microsoft.UI.Xaml;

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
