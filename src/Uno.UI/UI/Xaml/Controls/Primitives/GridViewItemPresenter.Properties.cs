// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlOM/Model/Microsoft.UI.Xaml.Controls.Primitives.cs, tag winui3/release/2.5.1

#nullable enable

using System;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class GridViewItemPresenter
{

	#region SelectionCheckMarkVisualEnabled DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: control whether selection check mark is shown or not.
	/// </summary>
	public bool SelectionCheckMarkVisualEnabled
	{
		get => GetSelectionCheckMarkVisualEnabledValue();
		set => SetSelectionCheckMarkVisualEnabledValue(value);
	}

	private static bool GetSelectionCheckMarkVisualEnabledDefaultValue() => ListViewBaseItemPresenter.GetDefaultSelectionCheckMarkVisualEnabled();

	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectionCheckMarkVisualEnabledProperty { get; } = CreateSelectionCheckMarkVisualEnabledProperty();

	#endregion

	#region CheckHintBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: checkmark behind the item, shown when haven't swiped far enough to select. Identical to below, only shown at 50% opacity.
	/// </summary>
	public Brush? CheckHintBrush
	{
		get => GetCheckHintBrushValue();
		set => SetCheckHintBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckHintBrushProperty { get; } = CreateCheckHintBrushProperty();

	#endregion

	#region CheckSelectingBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: checkmark behind the item, shown when swiped far enough to select.
	/// </summary>
	public Brush? CheckSelectingBrush
	{
		get => GetCheckSelectingBrushValue();
		set => SetCheckSelectingBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckSelectingBrushProperty { get; } = CreateCheckSelectingBrushProperty();

	#endregion

	#region CheckBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: checkmark above the item, shown when selected. Not same color as above 2 in Light Theme.
	/// </summary>
	public Brush? CheckBrush
	{
		get => GetCheckBrushValue();
		set => SetCheckBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBrushProperty { get; } = CreateCheckBrushProperty();

	#endregion

	#region DragBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: color of multi-drag DND overlay. Semitransparent.
	/// </summary>
	public Brush? DragBackground
	{
		get => GetDragBackgroundValue();
		set => SetDragBackgroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty DragBackgroundProperty { get; } = CreateDragBackgroundProperty();

	#endregion

	#region DragForeground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: color of multi-drag DND overlay item count.
	/// </summary>
	public Brush? DragForeground
	{
		get => GetDragForegroundValue();
		set => SetDragForegroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty DragForegroundProperty { get; } = CreateDragForegroundProperty();

	#endregion

	#region FocusBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: shown when has keyboard focus.
	/// </summary>
	public Brush? FocusBorderBrush
	{
		get => GetFocusBorderBrushValue();
		set => SetFocusBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty FocusBorderBrushProperty { get; } = CreateFocusBorderBrushProperty();

	#endregion

	#region PlaceholderBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: background fill of placeholder item.
	/// </summary>
	public Brush? PlaceholderBackground
	{
		get => GetPlaceholderBackgroundValue();
		set => SetPlaceholderBackgroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty PlaceholderBackgroundProperty { get; } = CreatePlaceholderBackgroundProperty();

	#endregion

	#region PointerOverBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: background fill of pointer over. NOTE! Backgrounds can and do mix if, say, user mousing over placeholder.
	/// </summary>
	public Brush? PointerOverBackground
	{
		get => GetPointerOverBackgroundValue();
		set => SetPointerOverBackgroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty PointerOverBackgroundProperty { get; } = CreatePointerOverBackgroundProperty();

	#endregion

	#region SelectedBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: selection border color. When selected: Background of item.
	/// </summary>
	public Brush? SelectedBackground
	{
		get => GetSelectedBackgroundValue();
		set => SetSelectedBackgroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedBackgroundProperty { get; } = CreateSelectedBackgroundProperty();

	#endregion

	#region SelectedForeground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: foreground set on the ContentPresenter for: Pointer over, Selecting, Selected, SelectedSwiping, SelectedUnfocused.
	/// </summary>
	public Brush? SelectedForeground
	{
		get => GetSelectedForegroundValue();
		set => SetSelectedForegroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedForegroundProperty { get; } = CreateSelectedForegroundProperty();

	#endregion

	#region SelectedPointerOverBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: only when selected: Selection background color becomes this on pointer over, pointer press.
	/// </summary>
	public Brush? SelectedPointerOverBackground
	{
		get => GetSelectedPointerOverBackgroundValue();
		set => SetSelectedPointerOverBackgroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedPointerOverBackgroundProperty { get; } = CreateSelectedPointerOverBackgroundProperty();

	#endregion

	#region SelectedPointerOverBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: in same visual states as above, except it's the selection border color. Identical to above.
	/// </summary>
	public Brush? SelectedPointerOverBorderBrush
	{
		get => GetSelectedPointerOverBorderBrushValue();
		set => SetSelectedPointerOverBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedPointerOverBorderBrushProperty { get; } = CreateSelectedPointerOverBorderBrushProperty();

	#endregion

	#region SelectedBorderThickness DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: thickness of selection border.
	/// </summary>
	public Thickness SelectedBorderThickness
	{
		get => GetSelectedBorderThicknessValue();
		set => SetSelectedBorderThicknessValue(value);
	}

	// Instances get the rounded/non-rounded default from ListViewBaseItemPresenter.GetDefaultValue2.
	private static Thickness GetSelectedBorderThicknessDefaultValue() => new(0);

	[GeneratedDependencyProperty(LocalCache = false, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedBorderThicknessProperty { get; } = CreateSelectedBorderThicknessProperty();

	#endregion

	#region DisabledOpacity DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: opacity of ContentPresenter (only) when disabled.
	/// </summary>
	public double DisabledOpacity
	{
		get => GetDisabledOpacityValue();
		set => SetDisabledOpacityValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = 0.55f, LocalCache = false, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty DisabledOpacityProperty { get; } = CreateDisabledOpacityProperty();

	#endregion

	#region DragOpacity DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: opacity of InnerDragContent when dragging.
	/// </summary>
	public double DragOpacity
	{
		get => GetDragOpacityValue();
		set => SetDragOpacityValue(value);
	}

	private static double GetDragOpacityDefaultValue() => ListViewBaseItemPresenter.GetDefaultDragOpacity();

	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty DragOpacityProperty { get; } = CreateDragOpacityProperty();

	#endregion

	#region ReorderHintOffset DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: amount containers move for reorder hints.
	/// </summary>
	public double ReorderHintOffset
	{
		get => GetReorderHintOffsetValue();
		set => SetReorderHintOffsetValue(value);
	}

	private static double GetReorderHintOffsetDefaultValue() => ListViewBaseItemPresenter.GetDefaultGridViewItemReorderHintOffset();

	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty ReorderHintOffsetProperty { get; } = CreateReorderHintOffsetProperty();

	#endregion

	#region GridViewItemPresenterHorizontalContentAlignment DependencyProperty

	/// <summary>
	/// Gets or sets the list view item presenter horizontal content alignment.
	/// </summary>
	public HorizontalAlignment GridViewItemPresenterHorizontalContentAlignment
	{
		get => (HorizontalAlignment)GetValue(GridViewItemPresenterHorizontalContentAlignmentProperty);
		set => SetValue(GridViewItemPresenterHorizontalContentAlignmentProperty, value);
	}

	// Forwards to ContentPresenter.HorizontalContentAlignment, as CListViewBaseItemChrome::GetValue/SetValue do.
	public static DependencyProperty GridViewItemPresenterHorizontalContentAlignmentProperty { get; } =
		DependencyProperty.Register(
			name: nameof(GridViewItemPresenterHorizontalContentAlignment),
			propertyType: typeof(HorizontalAlignment),
			ownerType: typeof(GridViewItemPresenter),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: default(HorizontalAlignment))
			{
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, GridViewItemPresenterHorizontalContentAlignmentProperty!, ContentPresenter.HorizontalContentAlignmentProperty),
			});

	#endregion

	#region GridViewItemPresenterVerticalContentAlignment DependencyProperty

	/// <summary>
	/// Gets or sets the list view item presenter vertical content alignment.
	/// </summary>
	public VerticalAlignment GridViewItemPresenterVerticalContentAlignment
	{
		get => (VerticalAlignment)GetValue(GridViewItemPresenterVerticalContentAlignmentProperty);
		set => SetValue(GridViewItemPresenterVerticalContentAlignmentProperty, value);
	}

	// Forwards to ContentPresenter.VerticalContentAlignment, as CListViewBaseItemChrome::GetValue/SetValue do.
	public static DependencyProperty GridViewItemPresenterVerticalContentAlignmentProperty { get; } =
		DependencyProperty.Register(
			name: nameof(GridViewItemPresenterVerticalContentAlignment),
			propertyType: typeof(VerticalAlignment),
			ownerType: typeof(GridViewItemPresenter),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: default(VerticalAlignment))
			{
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, GridViewItemPresenterVerticalContentAlignmentProperty!, ContentPresenter.VerticalContentAlignmentProperty),
			});

	#endregion

	#region GridViewItemPresenterPadding DependencyProperty

	/// <summary>
	/// Gets or sets the list view item presenter padding.
	/// </summary>
	public Thickness GridViewItemPresenterPadding
	{
		get => (Thickness)GetValue(GridViewItemPresenterPaddingProperty);
		set => SetValue(GridViewItemPresenterPaddingProperty, value);
	}

	// Forwards to ContentPresenter.Padding, as CListViewBaseItemChrome::GetValue/SetValue do.
	public static DependencyProperty GridViewItemPresenterPaddingProperty { get; } =
		DependencyProperty.Register(
			name: nameof(GridViewItemPresenterPadding),
			propertyType: typeof(Thickness),
			ownerType: typeof(GridViewItemPresenter),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: Thickness.Empty)
			{
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, GridViewItemPresenterPaddingProperty!, ContentPresenter.PaddingProperty),
			});

	#endregion

	#region PointerOverBackgroundMargin DependencyProperty

	/// <summary>
	/// Gets or sets the pointer over background margin.
	/// </summary>
	public Thickness PointerOverBackgroundMargin
	{
		get => GetPointerOverBackgroundMarginValue();
		set => SetPointerOverBackgroundMarginValue(value);
	}

	private static Thickness GetPointerOverBackgroundMarginDefaultValue() => new(0);

	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty PointerOverBackgroundMarginProperty { get; } = CreatePointerOverBackgroundMarginProperty();

	#endregion

	#region ContentMargin DependencyProperty

	/// <summary>
	/// Gets or sets the content margin.
	/// </summary>
	public Thickness ContentMargin
	{
		get => GetContentMarginValue();
		set => SetContentMarginValue(value);
	}

	private static Thickness GetContentMarginDefaultValue() => new(0);

	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty ContentMarginProperty { get; } = CreateContentMarginProperty();

	#endregion
}
