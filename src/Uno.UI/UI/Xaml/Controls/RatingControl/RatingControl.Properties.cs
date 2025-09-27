// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RatingControl.properties.cpp, tag winui3/release/1.5.3, commit 2a60e27c591846556fa9ec4d8f305afdf0f96dc1

using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class RatingControl
{
	/// <summary>
	/// Gets or sets the text label for the control.
	/// </summary>
	public string Caption
	{
		get => (string)GetValue(CaptionProperty);
		set => SetValue(CaptionProperty, value);
	}

	/// <summary>
	/// Identifies the Caption dependency property.
	/// </summary>
	public static DependencyProperty CaptionProperty { get; } =
		DependencyProperty.Register(
			nameof(Caption),
			typeof(string),
			typeof(RatingControl),
			new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the initial set rating value.
	/// </summary>
	public int InitialSetValue
	{
		get => (int)GetValue(InitialSetValueProperty);
		set => SetValue(InitialSetValueProperty, value);
	}

	/// <summary>
	/// Identifies the InitialSetValue dependency property.
	/// </summary>
	public static DependencyProperty InitialSetValueProperty { get; } =
		DependencyProperty.Register(
			nameof(InitialSetValue),
			typeof(int),
			typeof(RatingControl),
			new FrameworkPropertyMetadata(1, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the value that determines if the user can remove the rating.
	/// </summary>
	public bool IsClearEnabled
	{
		get => (bool)GetValue(IsClearEnabledProperty);
		set => SetValue(IsClearEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsClearEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsClearEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsClearEnabled),
			typeof(bool),
			typeof(RatingControl), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the value that determines if the user can change the rating.
	/// </summary>
	public bool IsReadOnly
	{
		get => (bool)GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	/// <summary>
	/// Identifies the IsReadOnly dependency property.
	/// </summary>
	public static DependencyProperty IsReadOnlyProperty { get; } =
		DependencyProperty.Register(
			nameof(IsReadOnly),
			typeof(bool),
			typeof(RatingControl),
			new FrameworkPropertyMetadata(false, OnPropertyChanged));

	/// <summary>
	/// Gets or sets info about the visual states of the items that represent a rating.
	/// </summary>
	public RatingItemInfo ItemInfo
	{
		get => (RatingItemInfo)GetValue(ItemInfoProperty);
		set => SetValue(ItemInfoProperty, value);
	}

	/// <summary>
	/// Identifies the ItemInfo dependency property.
	/// </summary>
	public static DependencyProperty ItemInfoProperty { get; } =
		DependencyProperty.Register(
			nameof(ItemInfo),
			typeof(RatingItemInfo),
			typeof(RatingControl),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the maximum allowed rating value.
	/// </summary>
	public int MaxRating
	{
		get => (int)GetValue(MaxRatingProperty);
		set => SetValue(MaxRatingProperty, value);
	}

	/// <summary>
	/// Identifies the MaxRating dependency property.
	/// </summary>
	public static DependencyProperty MaxRatingProperty { get; } =
		DependencyProperty.Register(
			nameof(MaxRating),
			typeof(int),
			typeof(RatingControl),
			new FrameworkPropertyMetadata(5, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the rating that is displayed in the control until the value is changed by a user action or some other operation.
	/// </summary>
	public double PlaceholderValue
	{
		get => (double)GetValue(PlaceholderValueProperty);
		set => SetValue(PlaceholderValueProperty, value);
	}

	/// <summary>
	/// Identifies the PlaceholderValue dependency property.
	/// </summary>
	public static DependencyProperty PlaceholderValueProperty { get; } =
		DependencyProperty.Register(
			nameof(PlaceholderValue),
			typeof(double),
			typeof(RatingControl),
			new FrameworkPropertyMetadata(-1.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the rating value.
	/// </summary>
	public double Value
	{
		get => (double)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	/// <summary>
	/// Identifies the Value dependency property.
	/// </summary>
	public static DependencyProperty ValueProperty { get; } =
		DependencyProperty.Register(
			nameof(Value),
			typeof(double),
			typeof(RatingControl),
			new FrameworkPropertyMetadata(-1.0, OnPropertyChanged));

	/// <summary>
	/// Occurs when the Value property has changed.
	/// </summary>
	public event TypedEventHandler<RatingControl, object> ValueChanged;

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = sender as RatingControl;
		owner.OnPropertyChanged(args);
	}
}
