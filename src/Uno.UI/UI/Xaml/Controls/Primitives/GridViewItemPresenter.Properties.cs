// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlOM/Model/Microsoft.UI.Xaml.Controls.Primitives.cs, tag winui3/release/2.5.1

#nullable enable

using System;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.Boxes;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class GridViewItemPresenter
{

	#region SelectionCheckMarkVisualEnabled DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: control whether selection check mark is shown or not.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial bool SelectionCheckMarkVisualEnabled { get; set; }

	private static object GetSelectionCheckMarkVisualEnabledDefaultValue() => Boxer.Box(ListViewBaseItemPresenter.GetDefaultSelectionCheckMarkVisualEnabled());

	#endregion

	#region CheckHintBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: checkmark behind the item, shown when haven't swiped far enough to select. Identical to below, only shown at 50% opacity.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckHintBrush { get; set; }

	#endregion

	#region CheckSelectingBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: checkmark behind the item, shown when swiped far enough to select.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckSelectingBrush { get; set; }

	#endregion

	#region CheckBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: checkmark above the item, shown when selected. Not same color as above 2 in Light Theme.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBrush { get; set; }

	#endregion

	#region DragBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: color of multi-drag DND overlay. Semitransparent.
	/// </summary>
	// TODO Uno: https://github.com/unoplatform/uno/issues/25922
	[Uno.NotImplemented]
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? DragBackground { get; set; }

	#endregion

	#region DragForeground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: color of multi-drag DND overlay item count.
	/// </summary>
	// TODO Uno: https://github.com/unoplatform/uno/issues/25922
	[Uno.NotImplemented]
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? DragForeground { get; set; }

	#endregion

	#region FocusBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: shown when has keyboard focus.
	/// </summary>
	// TODO Uno: https://github.com/unoplatform/uno/issues/25923
	[Uno.NotImplemented]
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? FocusBorderBrush { get; set; }

	#endregion

	#region PlaceholderBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: background fill of placeholder item.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? PlaceholderBackground { get; set; }

	#endregion

	#region PointerOverBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: background fill of pointer over. NOTE! Backgrounds can and do mix if, say, user mousing over placeholder.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? PointerOverBackground { get; set; }

	#endregion

	#region SelectedBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: selection border color. When selected: Background of item.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedBackground { get; set; }

	#endregion

	#region SelectedForeground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: foreground set on the ContentPresenter for: Pointer over, Selecting, Selected, SelectedSwiping, SelectedUnfocused.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedForeground { get; set; }

	#endregion

	#region SelectedPointerOverBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: only when selected: Selection background color becomes this on pointer over, pointer press.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedPointerOverBackground { get; set; }

	#endregion

	#region SelectedPointerOverBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: in same visual states as above, except it's the selection border color. Identical to above.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedPointerOverBorderBrush { get; set; }

	#endregion

	#region SelectedBorderThickness DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: thickness of selection border.
	/// </summary>
	[GeneratedDependencyProperty(LocalCache = false, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Thickness SelectedBorderThickness { get; set; }

	// Instances get the rounded/non-rounded default from ListViewBaseItemPresenter.GetDefaultValue2.
	private static Thickness GetSelectedBorderThicknessDefaultValue() => new(0);

	#endregion

	#region DisabledOpacity DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: opacity of ContentPresenter (only) when disabled.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = 0.55, LocalCache = false, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial double DisabledOpacity { get; set; }

	#endregion

	#region DragOpacity DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: opacity of InnerDragContent when dragging.
	/// </summary>
	// TODO Uno: https://github.com/unoplatform/uno/issues/25922
	[Uno.NotImplemented]
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial double DragOpacity { get; set; }

	private static object GetDragOpacityDefaultValue() => Boxer.Box(ListViewBaseItemPresenter.GetDefaultDragOpacity());

	#endregion

	#region ReorderHintOffset DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: amount containers move for reorder hints.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial double ReorderHintOffset { get; set; }

	private static object GetReorderHintOffsetDefaultValue() => Boxer.Box(ListViewBaseItemPresenter.GetDefaultGridViewItemReorderHintOffset());

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
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, GridViewItemPresenterHorizontalContentAlignmentProperty!, ContentPresenter.HorizontalContentAlignmentProperty, slot: 0),
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
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, GridViewItemPresenterVerticalContentAlignmentProperty!, ContentPresenter.VerticalContentAlignmentProperty, slot: 1),
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
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: default(Thickness))
			{
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, GridViewItemPresenterPaddingProperty!, ContentPresenter.PaddingProperty, slot: 2),
			});

	#endregion

	#region PointerOverBackgroundMargin DependencyProperty

	/// <summary>
	/// Gets or sets the pointer over background margin.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Thickness PointerOverBackgroundMargin { get; set; }

	private static Thickness GetPointerOverBackgroundMarginDefaultValue() => new(0);

	#endregion

	#region ContentMargin DependencyProperty

	/// <summary>
	/// Gets or sets the content margin.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Thickness ContentMargin { get; set; }

	private static Thickness GetContentMarginDefaultValue() => new(0);

	#endregion
}
