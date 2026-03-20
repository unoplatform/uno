// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	partial class RecyclePool
	{
		internal static DependencyProperty ReuseKeyProperty
		{
			[DynamicDependency(nameof(GetReuseKey))]
			[DynamicDependency(nameof(SetReuseKey))]
			get;
		} = DependencyProperty.RegisterAttached(
			"ReuseKey",
			typeof(string),
			typeof(RecyclePool),
			new FrameworkPropertyMetadata(defaultValue: "" /* defaultValue */, propertyChangedCallback: null /* propertyChangedCallback */));

		internal static DependencyProperty OriginTemplateProperty { get; } = DependencyProperty.RegisterAttached(
			"OriginTemplate",
			typeof(DataTemplate),
			typeof(RecyclePool),
			new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: null));


		#region IRecyclePoolStatics

		internal static string GetReuseKey(UIElement element)
		{
			return (string)element.GetValue(ReuseKeyProperty);
		}

		internal static void SetReuseKey(UIElement element, string value)
		{
			element.SetValue(ReuseKeyProperty, value);
		}

		#endregion
	}
}
