#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Tests.BinderTests;

public partial class Given_DependencyProperty
{
	public struct TypeDefaultStruct
	{
		public int Count;
	}

	public enum TypeDefaultEnum { Zero, One }

	// The expected values were measured on native WinUI (WinAppSDK) for properties registered with a null default.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25840")]
	public void When_Register_Without_DefaultValue_Then_Known_ValueType_Has_WinUI_Default()
	{
		var SUT = new MockDependencyObject();
		var cases = new (Type PropertyType, object Expected)[]
		{
			(typeof(bool), false),
			(typeof(int), 0),
			(typeof(long), 0L),
			(typeof(float), 0f),
			(typeof(double), 0d),
			(typeof(TimeSpan), TimeSpan.Zero),
			(typeof(DateTimeOffset), default(DateTimeOffset)),
			(typeof(Point), default(Point)),
			(typeof(Size), default(Size)),
			(typeof(Rect), default(Rect)),
			(typeof(Color), default(Color)),
			(typeof(Thickness), default(Thickness)),
			(typeof(CornerRadius), default(CornerRadius)),
			(typeof(GridLength), new GridLength(1, GridUnitType.Star)),
			(typeof(Visibility), Visibility.Visible),
			(typeof(Orientation), Orientation.Vertical),
			(typeof(global::Windows.UI.Text.FontStyle), global::Windows.UI.Text.FontStyle.Normal),
			(typeof(global::Windows.Foundation.Collections.CollectionChange), global::Windows.Foundation.Collections.CollectionChange.Reset),
			(typeof(global::Windows.System.VirtualKey), global::Windows.System.VirtualKey.None),
			(typeof(global::Microsoft.UI.Input.PointerDeviceType), global::Microsoft.UI.Input.PointerDeviceType.Touch),
		};

		foreach (var (propertyType, expected) in cases)
		{
			var property = DependencyProperty.Register(
				$"{nameof(When_Register_Without_DefaultValue_Then_Known_ValueType_Has_WinUI_Default)}_{propertyType.Name}",
				propertyType,
				typeof(MockDependencyObject),
				new PropertyMetadata(null));

			Assert.AreEqual(expected, property.GetMetadata(typeof(MockDependencyObject)).DefaultValue, propertyType.Name);
			Assert.AreEqual(expected, SUT.GetValue(property), propertyType.Name);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25840")]
	public void When_Register_Without_DefaultValue_Then_Other_ValueType_Is_Null()
	{
		var SUT = new MockDependencyObject();

		// Custom value types, nullable value types and the value types outside WinUI's XAML metadata,
		// including the enums Uno and WinRT define outside it.
		var propertyTypes = new[]
		{
			typeof(TypeDefaultStruct),
			typeof(TypeDefaultEnum),
			typeof(TypeDefaultStruct?),
			typeof(int?),
			typeof(Thickness?),
			typeof(Visibility?),
			typeof(short),
			typeof(uint),
			typeof(char),
			typeof(byte),
			typeof(decimal),
			typeof(Guid),
			typeof(DateTime),
			typeof(Duration),
			typeof(Microsoft.UI.Xaml.Media.Matrix),
			typeof(Microsoft.UI.Xaml.Media.Animation.KeyTime),
			typeof(System.Numerics.Vector2),
			typeof(Microsoft.UI.Composition.CompositionStretch),
			typeof(global::Windows.Storage.FileAttributes),
			typeof(global::Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode),
			typeof(string),
			typeof(object),
			typeof(Type),
		};

		for (var i = 0; i < propertyTypes.Length; i++)
		{
			var propertyType = propertyTypes[i];
			var property = DependencyProperty.Register(
				$"{nameof(When_Register_Without_DefaultValue_Then_Other_ValueType_Is_Null)}_{i}",
				propertyType,
				typeof(MockDependencyObject),
				new PropertyMetadata(null));

			Assert.IsNull(property.GetMetadata(typeof(MockDependencyObject)).DefaultValue, propertyType.ToString());
			Assert.IsNull(SUT.GetValue(property), propertyType.ToString());
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25840")]
	public void When_RegisterAttached_Without_Metadata_Then_ValueType_Default()
	{
		var SUT = new MockDependencyObject();
		var prefix = nameof(When_RegisterAttached_Without_Metadata_Then_ValueType_Default);
		var intProperty = DependencyProperty.RegisterAttached(prefix + "_Int", typeof(int), typeof(MockDependencyObject), null);
		var visibilityProperty = DependencyProperty.RegisterAttached(prefix + "_Visibility", typeof(Visibility), typeof(MockDependencyObject), null);
		var structProperty = DependencyProperty.RegisterAttached(prefix + "_Struct", typeof(TypeDefaultStruct), typeof(MockDependencyObject), null);
		var enumProperty = DependencyProperty.RegisterAttached(prefix + "_Enum", typeof(TypeDefaultEnum), typeof(MockDependencyObject), null);

		Assert.AreEqual(0, SUT.GetValue(intProperty));
		Assert.AreEqual(Visibility.Visible, SUT.GetValue(visibilityProperty));
		Assert.IsNull(SUT.GetValue(structProperty));
		Assert.IsNull(SUT.GetValue(enumProperty));

		SUT.SetValue(structProperty, new TypeDefaultStruct { Count = 42 });
		SUT.SetValue(enumProperty, TypeDefaultEnum.One);

		Assert.AreEqual(42, ((TypeDefaultStruct)SUT.GetValue(structProperty)).Count);
		Assert.AreEqual(TypeDefaultEnum.One, SUT.GetValue(enumProperty));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25840")]
	public void When_SetValue_Null_And_ValueType_Has_No_Default_Then_GetValue_Null()
	{
		// WinUI stores the null and GetValue returns it, for a custom value type as for any other.
		var SUT = new MockDependencyObject();
		var prefix = nameof(When_SetValue_Null_And_ValueType_Has_No_Default_Then_GetValue_Null);
		var structProperty = DependencyProperty.Register(prefix + "_Struct", typeof(TypeDefaultStruct), typeof(MockDependencyObject), new PropertyMetadata(null));
		var enumProperty = DependencyProperty.Register(prefix + "_Enum", typeof(TypeDefaultEnum), typeof(MockDependencyObject), null);

		SUT.SetValue(structProperty, new TypeDefaultStruct { Count = 1 });
		SUT.SetValue(enumProperty, TypeDefaultEnum.One);
		SUT.SetValue(structProperty, null);
		SUT.SetValue(enumProperty, null);

		Assert.IsNull(SUT.GetValue(structProperty));
		Assert.IsNull(SUT.GetValue(enumProperty));

		SUT.SetValue(enumProperty, TypeDefaultEnum.One);
		SUT.ClearValue(enumProperty);

		Assert.IsNull(SUT.GetValue(enumProperty));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25840")]
	public void When_TwoWay_Binding_Writes_Into_Struct_Default_Then_Other_Properties_Keep_Theirs()
	{
		var prefix = nameof(When_TwoWay_Binding_Writes_Into_Struct_Default_Then_Other_Properties_Keep_Theirs);
		var boundProperty = DependencyProperty.Register(prefix + "_Bound", typeof(Thickness), typeof(MockDependencyObject), new PropertyMetadata(null));
		var otherProperty = DependencyProperty.Register(prefix + "_Other", typeof(Thickness), typeof(MockDependencyObject), new PropertyMetadata(null));
		var targetProperty = DependencyProperty.Register(prefix + "_Target", typeof(double), typeof(MockDependencyObject), new PropertyMetadata(null));

		// Through the reflection setter, a two-way binding into a struct member writes into the box
		// GetValue returns, which is the registered default while the property is unset.
		var source = new MockDependencyObject();
		var target = new MockDependencyObject();
		BindingOperations.SetBinding(target, targetProperty, new Binding
		{
			Source = source,
			Path = new PropertyPath(prefix + "_Bound.Left"),
			Mode = BindingMode.TwoWay,
		});
		target.SetValue(targetProperty, 42d);

		var laterProperty = DependencyProperty.Register(prefix + "_Later", typeof(Thickness), typeof(MockDependencyObject), new PropertyMetadata(null));

		Assert.AreEqual(default(Thickness), new MockDependencyObject().GetValue(otherProperty));
		Assert.AreEqual(default(Thickness), new MockDependencyObject().GetValue(laterProperty));
		Assert.AreNotSame(
			boundProperty.GetMetadata(typeof(MockDependencyObject)).DefaultValue,
			otherProperty.GetMetadata(typeof(MockDependencyObject)).DefaultValue);
	}
}
