#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.BinderTests;

/// <summary>
/// Owner whose properties are typed by a generic parameter, the shape used by libraries such as the
/// CommunityToolkit's <c>Animation&lt;TValue, TKeyFrame&gt;</c>.
/// </summary>
public static class GenericPropertyTypeOwner<T>
{
	public static readonly DependencyProperty ValueProperty =
		DependencyProperty.Register(
			"Value",
			typeof(T),
			typeof(GenericPropertyTypeOwner<T>),
			new PropertyMetadata(null));

	public static readonly DependencyProperty AttachedValueProperty =
		DependencyProperty.RegisterAttached(
			"AttachedValue",
			typeof(T),
			typeof(GenericPropertyTypeOwner<T>),
			new PropertyMetadata(null));
}

public partial class Given_DependencyProperty
{
	// A trimming requirement on propertyType or ownerType warns (IL2087/IL2111/IL2072) in every consuming library that
	// registers a property typed or owned by a generic parameter, by System.Type or by a runtime Type; WinUI has none.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25840")]
	public void When_Register_Type_Parameters_Have_No_DynamicallyAccessedMembers()
	{
		var parameterTypes = new[] { typeof(string), typeof(Type), typeof(Type), typeof(PropertyMetadata) };

		foreach (var name in new[] { nameof(DependencyProperty.Register), nameof(DependencyProperty.RegisterAttached) })
		{
			var method = typeof(DependencyProperty).GetMethod(name, BindingFlags.Public | BindingFlags.Static, parameterTypes);
			Assert.IsNotNull(method, name);

			foreach (var parameter in method.GetParameters().Where(p => p.ParameterType == typeof(Type)))
			{
				Assert.IsNull(parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>(), $"{name}: {parameter.Name} must not carry [DynamicallyAccessedMembers]");
			}
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25840")]
	public void When_PropertyType_Is_Generic_Parameter_Then_Default_Value()
	{
		// A null default on a non-nullable value type is replaced by the type's zero value at registration.
		Assert.AreEqual(0, GenericPropertyTypeOwner<int>.ValueProperty.GetMetadata(typeof(GenericPropertyTypeOwner<int>)).DefaultValue);
		Assert.AreEqual(default(Thickness), GenericPropertyTypeOwner<Thickness>.ValueProperty.GetMetadata(typeof(GenericPropertyTypeOwner<Thickness>)).DefaultValue);

		// Nullable value types and reference types keep the null default.
		Assert.IsNull(GenericPropertyTypeOwner<int?>.ValueProperty.GetMetadata(typeof(GenericPropertyTypeOwner<int?>)).DefaultValue);
		Assert.IsNull(GenericPropertyTypeOwner<Thickness?>.ValueProperty.GetMetadata(typeof(GenericPropertyTypeOwner<Thickness?>)).DefaultValue);
		Assert.IsNull(GenericPropertyTypeOwner<string>.ValueProperty.GetMetadata(typeof(GenericPropertyTypeOwner<string>)).DefaultValue);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25840")]
	public void When_PropertyType_Is_Generic_Parameter_Then_SetValue_GetValue()
	{
		var SUT = new MockDependencyObject();

		Assert.AreEqual(0, SUT.GetValue(GenericPropertyTypeOwner<int>.AttachedValueProperty));
		Assert.IsNull(SUT.GetValue(GenericPropertyTypeOwner<string>.AttachedValueProperty));

		SUT.SetValue(GenericPropertyTypeOwner<int>.AttachedValueProperty, 42);
		SUT.SetValue(GenericPropertyTypeOwner<string>.AttachedValueProperty, "test");

		Assert.AreEqual(42, SUT.GetValue(GenericPropertyTypeOwner<int>.AttachedValueProperty));
		Assert.AreEqual("test", SUT.GetValue(GenericPropertyTypeOwner<string>.AttachedValueProperty));
	}
}
