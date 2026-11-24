// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.h

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class IconSourceElement
{
	/// <summary>
	/// Gets or sets the IconSource used as the icon content.
	/// </summary>
	public IconSource IconSource
	{
		get => (IconSource)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	/// <summary>
	/// Identifies the IconSource dependency property.
	/// </summary>
	public static DependencyProperty IconSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(IconSource),
			typeof(IconSource),
			typeof(IconSourceElement),
			new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: (s, e) => ((IconSourceElement)s).SwapIconElements()));
}
