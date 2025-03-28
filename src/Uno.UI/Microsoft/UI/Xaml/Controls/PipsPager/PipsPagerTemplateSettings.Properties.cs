// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPagerTemplateSettings.properties.cpp, tag winui3/release/1.4.2

using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent
/// sources when defining templates for a PipsPager.
/// </summary>
public sealed partial class PipsPagerTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets or sets the list of integers to represent the pips in the PipsPager.
	/// </summary>
	public IList<int> PipsPagerItems
	{
		get => (IList<int>)GetValue(PipsPagerItemsProperty);
		set => SetValue(PipsPagerItemsProperty, value);
	}

	internal static DependencyProperty PipsPagerItemsProperty { get; } =
		DependencyProperty.Register(
			nameof(PipsPagerItems),
			typeof(IList<int>),
			typeof(PipsPagerTemplateSettings),
			new FrameworkPropertyMetadata(null));
}
