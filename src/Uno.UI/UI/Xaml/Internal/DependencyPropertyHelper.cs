#nullable enable

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Core;

/// <summary>
/// The goal of this class is to provide a set of helper methods to work with <see cref="DependencyProperty"/> from
/// external projects.
/// </summary>
/// <remarks>
/// A small reflection is still required to access the class because those using it needs to know what they are doing.
/// </remarks>
internal static class DependencyPropertyHelper
{
	/// <summary>
	/// Get the default value of a <see cref="DependencyProperty"/> for the owner type. 
	/// </summary>
	/// <remarks>
	/// This is the value defined in the metadata of the property, which _may_ be overriden
	/// using the internal DependencyProperty.OverrideMetadata() method on a per-type basis
	/// -- that's why the type parameter is required.
	///
	/// A classic example of that is the Button.VerticalAlignmentProperty which is overridden
	/// to be VerticalAlignment.Center, while the default value is VerticalAlignment.Stretch
	/// from the base class.
	///
	/// This overload will return the default value for the owner type of the property - where
	/// the value is originally defined.
	/// </remarks>
	public static object? GetDefaultValue(DependencyProperty dependencyProperty)
		=> dependencyProperty.GetMetadata(dependencyProperty.OwnerType)?.DefaultValue;

	/// <summary>
	/// Get a reference to <see cref="DependencyProperty"/> by its name and owner type.
	/// </summary>
	/// <remarks>
	/// The name will match the name of the property as defined when the property is registered.
	///
	/// There is usually NO "Property" suffix on that name since it's the name that is used in XAML.
	///
	/// The name is case-sensitive.
	/// </remarks>
	public static DependencyProperty? GetDependencyPropertyByName(Type ownerType, string propertyName)
		=> DependencyProperty.GetProperty(ownerType, propertyName);

	/// <summary>
	/// Get a reference to <see cref="DependencyProperty"/> by its name on the given type.
	/// </summary>
	/// <remarks>
	/// The name will match the name of the property as defined when the property is registered.
	///
	/// There is usually NO "Property" suffix on that name since it's the name that is used in XAML.
	///
	/// The name is case-sensitive.
	/// </remarks>
	public static DependencyProperty? GetDependencyPropertyByName<T>(string propertyName)
		where T : DependencyObject
		=> GetDependencyPropertyByName(typeof(T), propertyName);

	/// <summary>
	/// Get all the <see cref="DependencyProperty"/> defined for a given type. 
	/// </summary>
	public static IReadOnlyCollection<DependencyProperty>? GetDependencyPropertiesForType<T>()
		where T : DependencyObject
		=> GetPropertiesForTypeCore(typeof(T));

	/// <summary>
	/// Try to get all the <see cref="DependencyProperty"/> defined for a given type.
	/// </summary>
	/// <returns>False means it's not a dependency object</returns>
	public static bool TryGetDependencyPropertiesForType(
		Type forType,
		[NotNullWhen(true)] out IReadOnlyCollection<DependencyProperty>? properties)
	{
		// Check if type is a DependencyObject
		if (!typeof(DependencyObject).IsAssignableFrom(forType))
		{
			properties = null;
			return false;
		}

		properties = GetPropertiesForTypeCore(forType);
		return true;
	}

	private static DependencyProperty[] GetPropertiesForTypeCore(Type type)
	{
		DependencyProperty.ForceInitializeTypeConstructor(type);

		var results = new List<DependencyProperty>();

		var currentType = type;

		do
		{
			if (DependencyProperty.DependencyPropertyRegistry.Instance.TryGetTypeTable(currentType, out var typeTable))
			{
				foreach (var value in typeTable!.Values)
				{
					results.Add((DependencyProperty)value);
				}
			}

			// Dependency properties are inherited
			currentType = currentType.BaseType;
		}
		while (currentType != typeof(object) && currentType != null);

		var array = results.ToArray();

		// Produce a pre-sorted list, aligned with the initial behavior of DependencyPropertyDetailsCollection
		Array.Sort(array, (l, r) => l.UniqueId - r.UniqueId);

		return array;
	}

	/// <summary>
	/// Get the type of the property. 
	/// </summary>
	public static Type GetPropertyType(DependencyProperty dependencyProperty)
		=> dependencyProperty.Type;

	/// <summary>
	/// Get the name of the property.
	/// </summary>
	public static string GetName(DependencyProperty dependencyProperty)
		=> dependencyProperty.Name;

	/// <summary>
	/// Get the owner type of the property  
	/// </summary>
	/// <remarks>
	/// This is the property that defines the property, not the type that uses it.
	/// </remarks>
	public static Type GetOwnerType(DependencyProperty dependencyProperty)
		=> dependencyProperty.OwnerType;

	/// <summary>
	/// Get whether the property is an Attached Property.
	/// </summary>
	public static bool GetIsAttached(DependencyProperty dependencyProperty)
		=> dependencyProperty.IsAttached;

	/// <summary>
	/// Get whether the property type is nullable - if the _null_ value is a valid value for the property.
	/// </summary>
	public static bool GetPropertyIsTypeNullable(DependencyProperty dependencyProperty)
		=> dependencyProperty.IsTypeNullable;

	/// <summary>
	/// This method is used to get the default value of a property on a dependency object and give the precedence of that value.
	/// </summary>
	/// <remarks>
	/// This method won't check the local value of the property. It's basically what the property would be if it there was no local value.
	/// </remarks>
	public static (object? value, DependencyPropertyValuePrecedences precedence) GetDefaultUnsetValue(
		DependencyObject obj,
		DependencyProperty dependencyProperty)
	{
		// 1st: Check assigned style value
		if (obj is FrameworkElement fe && fe.TryGetValueFromStyle(dependencyProperty, out var valueFromStyle))
		{
			// .TryGetValueFromStyle() will return false if the value is UnsetValue
			return (valueFromStyle, DependencyPropertyValuePrecedences.ExplicitOrImplicitStyle);
		}

		// 2nd: Check built-in style value, if any
		if (obj is Control control && control.TryGetValueFromBuiltInStyle(dependencyProperty, out var valueFromImplicitStyle))
		{
			// .TryGetValueFromBuiltInStyle() will return false if the value is UnsetValue
			return (valueFromImplicitStyle, DependencyPropertyValuePrecedences.DefaultStyle);
		}

		if (obj is IDependencyObjectStoreProvider { Store: { } store })
		{
			var details = store.GetPropertyDetails(dependencyProperty);

			// 3rd: Check inherited value
			var inheritedValue = details.GetInheritedValue();
			if (inheritedValue != DependencyProperty.UnsetValue)
			{
				return (inheritedValue, DependencyPropertyValuePrecedences.Inheritance);
			}

			// 4th: Check default value
			var defaultValue = store.GetDefaultValue(dependencyProperty);
			if (defaultValue != DependencyProperty.UnsetValue)
			{
				return (defaultValue, DependencyPropertyValuePrecedences.DefaultValue);
			}
		}

		// 5th: Return default value of the DP (should not happen)
		return (dependencyProperty.GetDefaultValue(obj, obj.GetType()), DependencyPropertyValuePrecedences.DefaultValue);
	}

	/// <summary>
	/// Set the value of a property on an object for a given precedence.
	/// </summary>
	/// <remarks>
	/// You must know what you are doing when using this method as it can break the property system.
	/// </remarks>
	public static void SetValueForPrecedence(
		DependencyObject obj,
		DependencyProperty dependencyProperty,
		object value,
		DependencyPropertyValuePrecedences precedence)
		=> obj.SetValue(dependencyProperty, value, precedence);

	/// <summary>
	/// Get if the property value is inherited through the visual tree. 
	/// </summary>
	public static bool GetIsInherited(DependencyProperty dependencyProperty)
		=> dependencyProperty.IsInherited;

	/// <summary>
	/// Get the multiple aspects of a given property at the same time.
	/// </summary>
	public static (Type PropertyType, Type OwnerType, string Name, bool IsTypeNullable, bool IsAttached, bool IsInherited, object? defaultValue) GetDetails(
		DependencyProperty property)
	{
		return (property.Type,
			property.OwnerType,
			property.Name,
			property.IsTypeNullable,
			property.IsAttached,
			property.IsInherited,
			property.GetDefaultValue(referenceObject: null, property.Type));
	}
}
