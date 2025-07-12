// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ExpanderTemplateSettings.properties.cpp, commit 8d20a91

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources
/// when defining templates for an Expander. Not intended for general use.
/// </summary>
public sealed partial class ExpanderTemplateSettings : DependencyObject
{
	internal ExpanderTemplateSettings()
	{
	}

	/// <summary>
	/// Gets the height of the Expander content.
	/// </summary>
	public double ContentHeight
	{
		get => (double)GetValue(ContentHeightProperty);
		internal set => SetValue(ContentHeightProperty, value);
	}

	private static DependencyProperty ContentHeightProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentHeight),
			typeof(double),
			typeof(ExpanderTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the height of the Expander content when the expand direction is negative.
	/// </summary>
	public double NegativeContentHeight
	{
		get => (double)GetValue(NegativeContentHeightProperty);
		internal set => SetValue(NegativeContentHeightProperty, value);
	}

	private static DependencyProperty NegativeContentHeightProperty { get; } =
		DependencyProperty.Register(
			nameof(NegativeContentHeight),
			typeof(double),
			typeof(ExpanderTemplateSettings),
			new FrameworkPropertyMetadata(0.0));
}
