// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\SortIndicator.properties.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Private.Controls;

partial class SortIndicator
{
	/// <summary>
	/// Gets or sets the direction the chevron indicates.
	/// </summary>
	public SortIndicatorDirection Direction
	{
		get => (SortIndicatorDirection)GetValue(DirectionProperty);
		set => SetValue(DirectionProperty, value);
	}

	/// <summary>
	/// Identifies the Direction dependency property.
	/// </summary>
	public static DependencyProperty DirectionProperty { get; } =
		DependencyProperty.Register(
			nameof(Direction),
			typeof(SortIndicatorDirection),
			typeof(SortIndicator),
			new FrameworkPropertyMetadata(SortIndicatorDirection.None, OnDirectionPropertyChanged));

	private static void OnDirectionPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (SortIndicator)sender;
		owner.OnDirectionPropertyChanged(args);
	}
}
