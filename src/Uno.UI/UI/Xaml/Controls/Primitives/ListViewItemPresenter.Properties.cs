// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlOM/Model/Microsoft.UI.Xaml.Controls.Primitives.cs, tag winui3/release/2.5.1

#nullable enable

using System;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.Boxes;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewItemPresenter
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

	private static object GetReorderHintOffsetDefaultValue() => Boxer.Box(ListViewBaseItemPresenter.GetDefaultListViewItemReorderHintOffset());

	#endregion

	#region ListViewItemPresenterHorizontalContentAlignment DependencyProperty

	/// <summary>
	/// Gets or sets the list view item presenter horizontal content alignment.
	/// </summary>
	public HorizontalAlignment ListViewItemPresenterHorizontalContentAlignment
	{
		get => (HorizontalAlignment)GetValue(ListViewItemPresenterHorizontalContentAlignmentProperty);
		set => SetValue(ListViewItemPresenterHorizontalContentAlignmentProperty, value);
	}

	// Forwards to ContentPresenter.HorizontalContentAlignment, as CListViewBaseItemChrome::GetValue/SetValue do.
	public static DependencyProperty ListViewItemPresenterHorizontalContentAlignmentProperty { get; } =
		DependencyProperty.Register(
			name: nameof(ListViewItemPresenterHorizontalContentAlignment),
			propertyType: typeof(HorizontalAlignment),
			ownerType: typeof(ListViewItemPresenter),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: default(HorizontalAlignment))
			{
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, ListViewItemPresenterHorizontalContentAlignmentProperty!, ContentPresenter.HorizontalContentAlignmentProperty, slot: 0),
			});

	#endregion

	#region ListViewItemPresenterVerticalContentAlignment DependencyProperty

	/// <summary>
	/// Gets or sets the list view item presenter vertical content alignment.
	/// </summary>
	public VerticalAlignment ListViewItemPresenterVerticalContentAlignment
	{
		get => (VerticalAlignment)GetValue(ListViewItemPresenterVerticalContentAlignmentProperty);
		set => SetValue(ListViewItemPresenterVerticalContentAlignmentProperty, value);
	}

	// Forwards to ContentPresenter.VerticalContentAlignment, as CListViewBaseItemChrome::GetValue/SetValue do.
	public static DependencyProperty ListViewItemPresenterVerticalContentAlignmentProperty { get; } =
		DependencyProperty.Register(
			name: nameof(ListViewItemPresenterVerticalContentAlignment),
			propertyType: typeof(VerticalAlignment),
			ownerType: typeof(ListViewItemPresenter),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: default(VerticalAlignment))
			{
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, ListViewItemPresenterVerticalContentAlignmentProperty!, ContentPresenter.VerticalContentAlignmentProperty, slot: 1),
			});

	#endregion

	#region ListViewItemPresenterPadding DependencyProperty

	/// <summary>
	/// Gets or sets the list view item presenter padding.
	/// </summary>
	public Thickness ListViewItemPresenterPadding
	{
		get => (Thickness)GetValue(ListViewItemPresenterPaddingProperty);
		set => SetValue(ListViewItemPresenterPaddingProperty, value);
	}

	// Forwards to ContentPresenter.Padding, as CListViewBaseItemChrome::GetValue/SetValue do.
	public static DependencyProperty ListViewItemPresenterPaddingProperty { get; } =
		DependencyProperty.Register(
			name: nameof(ListViewItemPresenterPadding),
			propertyType: typeof(Thickness),
			ownerType: typeof(ListViewItemPresenter),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: default(Thickness))
			{
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, ListViewItemPresenterPaddingProperty!, ContentPresenter.PaddingProperty, slot: 2),
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

	#region SelectedPressedBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: background/Border color becomes this when pressed and selected.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedPressedBackground { get; set; }

	#endregion

	#region PressedBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: background/Border color becomes this when pressed.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? PressedBackground { get; set; }

	#endregion

	#region CheckBoxBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the check box background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxBrush { get; set; }

	#endregion

	#region FocusSecondaryBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: shown when has keyboard focus.
	/// </summary>
	// TODO Uno: https://github.com/unoplatform/uno/issues/25923
	[Uno.NotImplemented]
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? FocusSecondaryBorderBrush { get; set; }

	#endregion

	#region CheckMode DependencyProperty

	/// <summary>
	/// Gets or sets the check mode.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = ListViewItemPresenterCheckMode.Inline, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial ListViewItemPresenterCheckMode CheckMode { get; set; }

	#endregion

	#region PointerOverForeground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: foreground set on the ContentPresenter for: Pointer over.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? PointerOverForeground { get; set; }

	#endregion

	#region RevealBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush set for reveal background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? RevealBackground { get; set; }

	#endregion

	#region RevealBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush set for reveal border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? RevealBorderBrush { get; set; }

	#endregion

	#region RevealBorderThickness DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: thickness for reveal border.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Thickness RevealBorderThickness { get; set; }

	private static Thickness GetRevealBorderThicknessDefaultValue() => new(0);

	#endregion

	#region RevealBackgroundShowsAboveContent DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: draw reveal over content or under Content.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = false, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial bool RevealBackgroundShowsAboveContent { get; set; }

	#endregion

	#region SelectedDisabledBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected disabled background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedDisabledBackground { get; set; }

	#endregion

	#region CheckPressedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pressed selection checkmark.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckPressedBrush { get; set; }

	#endregion

	#region CheckDisabledBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the disabled selection checkmark.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckDisabledBrush { get; set; }

	#endregion

	#region CheckBoxPointerOverBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pointer-over multi-select check box background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxPointerOverBrush { get; set; }

	#endregion

	#region CheckBoxPressedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pressed multi-select check box background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxPressedBrush { get; set; }

	#endregion

	#region CheckBoxDisabledBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the disabled multi-select check box background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxDisabledBrush { get; set; }

	#endregion

	#region CheckBoxSelectedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected multi-select check box background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxSelectedBrush { get; set; }

	#endregion

	#region CheckBoxSelectedPointerOverBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected pointer-over multi-select check box background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxSelectedPointerOverBrush { get; set; }

	#endregion

	#region CheckBoxSelectedPressedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected pressed multi-select check box background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxSelectedPressedBrush { get; set; }

	#endregion

	#region CheckBoxSelectedDisabledBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected disabled multi-select check box background.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxSelectedDisabledBrush { get; set; }

	#endregion

	#region CheckBoxBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the multi-select check box border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxBorderBrush { get; set; }

	#endregion

	#region CheckBoxPointerOverBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pointer-over multi-select check box border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxPointerOverBorderBrush { get; set; }

	#endregion

	#region CheckBoxPressedBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pressed multi-select check box border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxPressedBorderBrush { get; set; }

	#endregion

	#region CheckBoxDisabledBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the disabled multi-select check box border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? CheckBoxDisabledBorderBrush { get; set; }

	#endregion

	#region CheckBoxCornerRadius DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: corner radius set on the multi-select check box.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial CornerRadius CheckBoxCornerRadius { get; set; }

	private static CornerRadius GetCheckBoxCornerRadiusDefaultValue() => ListViewBaseItemPresenter.s_defaultCheckBoxCornerRadius;

	#endregion

	#region SelectionIndicatorCornerRadius DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: corner radius set on the multi-select check box.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial CornerRadius SelectionIndicatorCornerRadius { get; set; }

	private static CornerRadius GetSelectionIndicatorCornerRadiusDefaultValue() => ListViewBaseItemPresenter.s_defaultSelectionIndicatorCornerRadius;

	#endregion

	#region SelectionIndicatorVisualEnabled DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: control whether selection indicator is shown or not.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = false, LocalCache = false, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial bool SelectionIndicatorVisualEnabled { get; set; }

	#endregion

	#region SelectionIndicatorMode DependencyProperty

	/// <summary>
	/// Gets or sets the selection indicator mode.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = ListViewItemPresenterSelectionIndicatorMode.Overlay, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial ListViewItemPresenterSelectionIndicatorMode SelectionIndicatorMode { get; set; }

	#endregion

	#region SelectionIndicatorBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selection indicator.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectionIndicatorBrush { get; set; }

	#endregion

	#region SelectionIndicatorPointerOverBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pointer-over selection indicator.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectionIndicatorPointerOverBrush { get; set; }

	#endregion

	#region SelectionIndicatorPressedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pressed selection indicator.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectionIndicatorPressedBrush { get; set; }

	#endregion

	#region SelectionIndicatorDisabledBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the disabled selection indicator.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectionIndicatorDisabledBrush { get; set; }

	#endregion

	#region SelectedBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the outer selection border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedBorderBrush { get; set; }

	#endregion

	#region SelectedPressedBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the outer selection pressed border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedPressedBorderBrush { get; set; }

	#endregion

	#region SelectedDisabledBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the outer selection disabled border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedDisabledBorderBrush { get; set; }

	#endregion

	#region SelectedInnerBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the inner selection border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? SelectedInnerBorderBrush { get; set; }

	#endregion

	#region PointerOverBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pointer-over outer border.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public partial Brush? PointerOverBorderBrush { get; set; }

	#endregion
}
