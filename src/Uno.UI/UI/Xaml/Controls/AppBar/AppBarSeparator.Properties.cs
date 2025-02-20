// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBarSeparator_Partial.cpp, tag winui3/release/1.6.4, commit 262a901e09

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarSeparator
{
	/// <summary>
	/// Gets or sets the order in which this item is moved to the CommandBar overflow menu.
	/// </summary>
	public int DynamicOverflowOrder
	{
		get => (int)GetValue(DynamicOverflowOrderProperty);
		set => SetValue(DynamicOverflowOrderProperty, value);
	}

	/// <summary>
	/// Identifies the DynamicOverflowOrder dependency property.
	/// </summary>
	public static DependencyProperty DynamicOverflowOrderProperty { get; } =
		DependencyProperty.Register(
			nameof(DynamicOverflowOrder),
			typeof(int),
			typeof(AppBarSeparator),
			new FrameworkPropertyMetadata(default(int)));

	/// <summary>
	/// Gets or sets a value that indicates whether the separator is shown with reduced padding.
	/// </summary>
	public bool IsCompact
	{
		get => (bool)GetValue(IsCompactProperty);
		set => SetValue(IsCompactProperty, value);
	}

	/// <summary>
	/// Identifies the IsCompact dependency property.
	/// </summary>
	public static DependencyProperty IsCompactProperty { get; } =
	DependencyProperty.Register(
		nameof(IsCompact),
		typeof(bool),
		typeof(AppBarSeparator),
		new FrameworkPropertyMetadata(default(bool))
	);

	/// <summary>
	/// Gets a value that indicates whether this item is in the overflow menu.
	/// </summary>
	public bool IsInOverflow
	{
		get => CommandBar.IsCommandBarElementInOverflow(this);
		private set => SetValue(IsInOverflowProperty, value);
	}

	/// <summary>
	/// Identifies the IsInOverflow dependency property.
	/// </summary>
	public static DependencyProperty IsInOverflowProperty { get; } =
		DependencyProperty.Register(
			nameof(IsInOverflow),
			typeof(bool),
			typeof(AppBarSeparator),
			new FrameworkPropertyMetadata(default(bool)));

	bool ICommandBarElement3.IsInOverflow
	{
		get => IsInOverflow;
		set => IsInOverflow = value;
	}

	internal bool UseOverflowStyle
	{
		get => (bool)GetValue(UseOverflowStyleProperty);
		set => SetValue(UseOverflowStyleProperty, value);
	}

	bool ICommandBarOverflowElement.UseOverflowStyle
	{
		get => UseOverflowStyle;
		set => UseOverflowStyle = value;
	}

	internal static DependencyProperty UseOverflowStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(UseOverflowStyle),
			typeof(bool),
			typeof(AppBarSeparator),
			new FrameworkPropertyMetadata(default(bool))
		);
}
