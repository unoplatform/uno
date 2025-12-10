// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPager.properties.cpp, tag winui3/release/1.8-stable

using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class PipsPager
{
	/// <summary>
	/// The maximum number of pips shown in the PipsPager at one time.
	/// </summary>
	public int MaxVisiblePips
	{
		get => (int)GetValue(MaxVisiblePipsProperty);
		set => SetValue(MaxVisiblePipsProperty, value);
	}

	/// <summary>
	/// Identifies the MaxVisiblePips dependency property.
	/// </summary>
	public static DependencyProperty MaxVisiblePipsProperty { get; } =
		DependencyProperty.Register(
			nameof(MaxVisiblePips),
			typeof(int),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(5, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the style to apply to the Next button.
	/// </summary>
	public Style NextButtonStyle
	{
		get => (Style)GetValue(NextButtonStyleProperty);
		set => SetValue(NextButtonStyleProperty, value);
	}

	/// <summary>
	/// Identifies the NextButtonStyle dependency property.
	/// </summary>
	public static DependencyProperty NextButtonStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(NextButtonStyle),
			typeof(Style),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the display state of the Next button.
	/// </summary>
	public PipsPagerButtonVisibility NextButtonVisibility
	{
		get => (PipsPagerButtonVisibility)GetValue(NextButtonVisibilityProperty);
		set => SetValue(NextButtonVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the NextButtonVisibility dependency property.
	/// </summary>
	public static DependencyProperty NextButtonVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(NextButtonVisibility),
			typeof(PipsPagerButtonVisibility),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(PipsPagerButtonVisibility.Collapsed, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the style for the default, unselected pips in the PipsPager.
	/// </summary>
	public Style NormalPipStyle
	{
		get => (Style)GetValue(NormalPipStyleProperty);
		set => SetValue(NormalPipStyleProperty, value);
	}

	/// <summary>
	/// Identifies the NormalPipStyle dependency property.
	/// </summary>
	public static DependencyProperty NormalPipStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(NormalPipStyle),
			typeof(Style),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the maximum number of pages supported by the PipsPager.
	/// </summary>
	public int NumberOfPages
	{
		get => (int)GetValue(NumberOfPagesProperty);
		set => SetValue(NumberOfPagesProperty, value);
	}

	/// <summary>
	/// Identifies the NumberOfPages dependency property.
	/// </summary>
	public static DependencyProperty NumberOfPagesProperty { get; } =
		DependencyProperty.Register(
			nameof(NumberOfPages),
			typeof(int),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(-1, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the orientation of the pips and navigation buttons in the PipsPager.
	/// </summary>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Identifies the Orientation dependency property.
	/// </summary>
	public static DependencyProperty OrientationProperty { get; } =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(Orientation.Horizontal, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the style to apply to the Previous button.
	/// </summary>
	public Style PreviousButtonStyle
	{
		get => (Style)GetValue(PreviousButtonStyleProperty);
		set => SetValue(PreviousButtonStyleProperty, value);
	}

	/// <summary>
	/// Identifies the PreviousButtonStyle dependency property.
	/// </summary>
	public static DependencyProperty PreviousButtonStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(PreviousButtonStyle),
			typeof(Style),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the display state of the Previous button.
	/// </summary>
	public PipsPagerButtonVisibility PreviousButtonVisibility
	{
		get => (PipsPagerButtonVisibility)GetValue(PreviousButtonVisibilityProperty);
		set => SetValue(PreviousButtonVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the PreviousButtonVisibility dependency property.
	/// </summary>
	public static DependencyProperty PreviousButtonVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(PreviousButtonVisibility),
			typeof(PipsPagerButtonVisibility),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(PipsPagerButtonVisibility.Collapsed, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the 0 based index of the currently selected pip in the PipsPager. A pip is always selected.
	/// </summary>
	public int SelectedPageIndex
	{
		get => (int)GetValue(SelectedPageIndexProperty);
		set => SetValue(SelectedPageIndexProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedPageIndex dependency property.
	/// </summary>
	public static DependencyProperty SelectedPageIndexProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedPageIndex),
			typeof(int),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the style to apply to the selected pip in the PipsPager.
	/// </summary>
	public Style SelectedPipStyle
	{
		get => (Style)GetValue(SelectedPipStyleProperty);
		set => SetValue(SelectedPipStyleProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedPipStyle dependency property.
	/// </summary>
	public static DependencyProperty SelectedPipStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedPipStyle),
			typeof(Style),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a PipsPager. Not intended for general use.
	/// </summary>
	public PipsPagerTemplateSettings TemplateSettings =>
		(PipsPagerTemplateSettings)GetValue(TemplateSettingsProperty);

	internal static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			nameof(TemplateSettings),
			typeof(PipsPagerTemplateSettings),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the wrap mode, which determines how the items in the pager are wrapped.
	/// </summary>
	public PipsPagerWrapMode WrapMode
	{
		get => (PipsPagerWrapMode)this.GetValue(WrapModeProperty);
		set => SetValue(WrapModeProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="WrapMode"/> dependency property, which determines how the items in the pager are wrapped.
	/// </summary>
	public static DependencyProperty WrapModeProperty { get; } =
		DependencyProperty.Register(
			nameof(WrapMode), typeof(PipsPagerWrapMode),
			typeof(PipsPager),
			new FrameworkPropertyMetadata(default(PipsPagerWrapMode)));

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (PipsPager)sender;
		owner.OnPropertyChanged(args);
	}

	// Events

	/// <summary>
	/// Occurs after the selected index changes on the PipsPager.
	/// </summary>
	public event TypedEventHandler<PipsPager, PipsPagerSelectedIndexChangedEventArgs> SelectedIndexChanged;
}
