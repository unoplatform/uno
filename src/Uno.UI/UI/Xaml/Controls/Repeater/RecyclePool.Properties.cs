// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RecyclePoolFactory.cpp, commit 4b206bce3

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Xaml.Controls;

partial class RecyclePool
{
	public RecyclePool()
	{
		RecyclePool.EnsureProperties();
	}

	// #pragma region IRecyclePoolStatics

	/// <summary>
	/// Identifies the ReuseKey attached dependency property.
	/// </summary>
	internal static DependencyProperty ReuseKeyProperty
	{
		[DynamicDependency(nameof(GetReuseKey))]
		[DynamicDependency(nameof(SetReuseKey))]
		get
		{
			EnsureProperties();
			return s_reuseKeyProperty;
		}
	}

	/// <summary>
	/// Returns the reuse key value attached to the specified element.
	/// </summary>
	/// <param name="element">The target element.</param>
	/// <returns>The reuse key value attached to the element.</returns>
	internal static string GetReuseKey(UIElement element)
	{
		return (string)element.GetValue(ReuseKeyProperty);
	}

	/// <summary>
	/// Sets the reuse key value on the specified element.
	/// </summary>
	/// <param name="element">The target element.</param>
	/// <param name="value">The reuse key value to attach.</param>
	internal static void SetReuseKey(UIElement element, string value)
	{
		element.SetValue(ReuseKeyProperty, value);
	}

	/// <summary>
	/// Identifies the PoolInstance attached dependency property.
	/// </summary>
	public static DependencyProperty PoolInstanceProperty
	{
		[DynamicDependency(nameof(GetPoolInstance))]
		[DynamicDependency(nameof(SetPoolInstance))]
		get
		{
			EnsureProperties();
			return s_PoolInstanceProperty;
		}
	}

	/// <summary>
	/// Returns the <see cref="RecyclePool"/> attached to the specified <see cref="DataTemplate"/>.
	/// </summary>
	/// <param name="dataTemplate">The target template.</param>
	/// <returns>The <see cref="RecyclePool"/> attached to the template, or null.</returns>
	public static RecyclePool GetPoolInstance(DataTemplate dataTemplate)
	{
		return (RecyclePool)dataTemplate.GetValue(PoolInstanceProperty);
	}

	/// <summary>
	/// Sets the <see cref="RecyclePool"/> on the specified <see cref="DataTemplate"/>.
	/// </summary>
	/// <param name="dataTemplate">The target template.</param>
	/// <param name="value">The <see cref="RecyclePool"/> to attach.</param>
	public static void SetPoolInstance(DataTemplate dataTemplate, RecyclePool value)
	{
		dataTemplate.SetValue(PoolInstanceProperty, value);
	}

	internal static DependencyProperty OriginTemplateProperty
	{
		[DynamicDependency(nameof(GetOriginTemplate))]
		[DynamicDependency(nameof(SetOriginTemplate))]
		get
		{
			EnsureProperties();
			return s_originTemplateProperty;
		}
	}

	internal static DataTemplate GetOriginTemplate(UIElement element)
	{
		return (DataTemplate)element.GetValue(s_originTemplateProperty);
	}

	internal static void SetOriginTemplate(UIElement element, DataTemplate value)
	{
		element.SetValue(s_originTemplateProperty, value);
	}

	// #pragma endregion

	internal static void EnsureProperties()
	{
		if (s_PoolInstanceProperty == null)
		{
			s_PoolInstanceProperty =
				DependencyProperty.RegisterAttached(
					"PoolInstance",
					typeof(RecyclePool),
					typeof(ItemsRepeater),
					new FrameworkPropertyMetadata(default(RecyclePool)));
		}

		if (s_reuseKeyProperty == null)
		{
			s_reuseKeyProperty =
				DependencyProperty.RegisterAttached(
					"ReuseKey",
					typeof(string),
					typeof(ItemsRepeater),
					new FrameworkPropertyMetadata("" /* defaultValue */, null /* propertyChangedCallback */));
		}

		if (s_originTemplateProperty == null)
		{
			s_originTemplateProperty =
				DependencyProperty.RegisterAttached(
					"OriginTemplate",
					typeof(DataTemplate),
					typeof(object),
					new FrameworkPropertyMetadata(null /* defaultValue */, null /* propertyChangedCallback */));
		}
	}

	internal static void ClearProperties()
	{
		s_reuseKeyProperty = null;
		s_originTemplateProperty = null;
		s_PoolInstanceProperty = null;
	}
}
