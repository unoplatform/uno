// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlOM/Model/Microsoft.UI.Xaml.Controls.Primitives.cs, tag winui3/release/2.5.1

#nullable enable

using System;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewItemPresenter
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

	[GeneratedDependencyProperty(DefaultValue = true, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
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

	[GeneratedDependencyProperty(DefaultValue = 0.8f, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
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

	[GeneratedDependencyProperty(DefaultValue = 10.0, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty ReorderHintOffsetProperty { get; } = CreateReorderHintOffsetProperty();

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
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, ListViewItemPresenterHorizontalContentAlignmentProperty!, ContentPresenter.HorizontalContentAlignmentProperty),
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
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, ListViewItemPresenterVerticalContentAlignmentProperty!, ContentPresenter.VerticalContentAlignmentProperty),
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
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: Thickness.Empty)
			{
				PropMethodCall = (instance, isGet, value) => ForwardAlias(instance, isGet, value, ListViewItemPresenterPaddingProperty!, ContentPresenter.PaddingProperty),
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

	#region SelectedPressedBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: background/Border color becomes this when pressed and selected.
	/// </summary>
	public Brush? SelectedPressedBackground
	{
		get => GetSelectedPressedBackgroundValue();
		set => SetSelectedPressedBackgroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedPressedBackgroundProperty { get; } = CreateSelectedPressedBackgroundProperty();

	#endregion

	#region PressedBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: background/Border color becomes this when pressed.
	/// </summary>
	public Brush? PressedBackground
	{
		get => GetPressedBackgroundValue();
		set => SetPressedBackgroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty PressedBackgroundProperty { get; } = CreatePressedBackgroundProperty();

	#endregion

	#region CheckBoxBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the check box background.
	/// </summary>
	public Brush? CheckBoxBrush
	{
		get => GetCheckBoxBrushValue();
		set => SetCheckBoxBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxBrushProperty { get; } = CreateCheckBoxBrushProperty();

	#endregion

	#region FocusSecondaryBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: shown when has keyboard focus.
	/// </summary>
	public Brush? FocusSecondaryBorderBrush
	{
		get => GetFocusSecondaryBorderBrushValue();
		set => SetFocusSecondaryBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty FocusSecondaryBorderBrushProperty { get; } = CreateFocusSecondaryBorderBrushProperty();

	#endregion

	#region CheckMode DependencyProperty

	/// <summary>
	/// Gets or sets the check mode.
	/// </summary>
	public ListViewItemPresenterCheckMode CheckMode
	{
		get => GetCheckModeValue();
		set => SetCheckModeValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = ListViewItemPresenterCheckMode.Inline, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckModeProperty { get; } = CreateCheckModeProperty();

	#endregion

	#region PointerOverForeground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: foreground set on the ContentPresenter for: Pointer over.
	/// </summary>
	public Brush? PointerOverForeground
	{
		get => GetPointerOverForegroundValue();
		set => SetPointerOverForegroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty PointerOverForegroundProperty { get; } = CreatePointerOverForegroundProperty();

	#endregion

	#region RevealBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush set for reveal background.
	/// </summary>
	public Brush? RevealBackground
	{
		get => GetRevealBackgroundValue();
		set => SetRevealBackgroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty RevealBackgroundProperty { get; } = CreateRevealBackgroundProperty();

	#endregion

	#region RevealBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush set for reveal border.
	/// </summary>
	public Brush? RevealBorderBrush
	{
		get => GetRevealBorderBrushValue();
		set => SetRevealBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty RevealBorderBrushProperty { get; } = CreateRevealBorderBrushProperty();

	#endregion

	#region RevealBorderThickness DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: thickness for reveal border.
	/// </summary>
	public Thickness RevealBorderThickness
	{
		get => GetRevealBorderThicknessValue();
		set => SetRevealBorderThicknessValue(value);
	}

	private static Thickness GetRevealBorderThicknessDefaultValue() => new(0);

	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty RevealBorderThicknessProperty { get; } = CreateRevealBorderThicknessProperty();

	#endregion

	#region RevealBackgroundShowsAboveContent DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: draw reveal over content or under Content.
	/// </summary>
	public bool RevealBackgroundShowsAboveContent
	{
		get => GetRevealBackgroundShowsAboveContentValue();
		set => SetRevealBackgroundShowsAboveContentValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = false, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty RevealBackgroundShowsAboveContentProperty { get; } = CreateRevealBackgroundShowsAboveContentProperty();

	#endregion

	#region SelectedDisabledBackground DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected disabled background.
	/// </summary>
	public Brush? SelectedDisabledBackground
	{
		get => GetSelectedDisabledBackgroundValue();
		set => SetSelectedDisabledBackgroundValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedDisabledBackgroundProperty { get; } = CreateSelectedDisabledBackgroundProperty();

	#endregion

	#region CheckPressedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pressed selection checkmark.
	/// </summary>
	public Brush? CheckPressedBrush
	{
		get => GetCheckPressedBrushValue();
		set => SetCheckPressedBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckPressedBrushProperty { get; } = CreateCheckPressedBrushProperty();

	#endregion

	#region CheckDisabledBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the disabled selection checkmark.
	/// </summary>
	public Brush? CheckDisabledBrush
	{
		get => GetCheckDisabledBrushValue();
		set => SetCheckDisabledBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckDisabledBrushProperty { get; } = CreateCheckDisabledBrushProperty();

	#endregion

	#region CheckBoxPointerOverBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pointer-over multi-select check box background.
	/// </summary>
	public Brush? CheckBoxPointerOverBrush
	{
		get => GetCheckBoxPointerOverBrushValue();
		set => SetCheckBoxPointerOverBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxPointerOverBrushProperty { get; } = CreateCheckBoxPointerOverBrushProperty();

	#endregion

	#region CheckBoxPressedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pressed multi-select check box background.
	/// </summary>
	public Brush? CheckBoxPressedBrush
	{
		get => GetCheckBoxPressedBrushValue();
		set => SetCheckBoxPressedBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxPressedBrushProperty { get; } = CreateCheckBoxPressedBrushProperty();

	#endregion

	#region CheckBoxDisabledBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the disabled multi-select check box background.
	/// </summary>
	public Brush? CheckBoxDisabledBrush
	{
		get => GetCheckBoxDisabledBrushValue();
		set => SetCheckBoxDisabledBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxDisabledBrushProperty { get; } = CreateCheckBoxDisabledBrushProperty();

	#endregion

	#region CheckBoxSelectedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected multi-select check box background.
	/// </summary>
	public Brush? CheckBoxSelectedBrush
	{
		get => GetCheckBoxSelectedBrushValue();
		set => SetCheckBoxSelectedBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxSelectedBrushProperty { get; } = CreateCheckBoxSelectedBrushProperty();

	#endregion

	#region CheckBoxSelectedPointerOverBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected pointer-over multi-select check box background.
	/// </summary>
	public Brush? CheckBoxSelectedPointerOverBrush
	{
		get => GetCheckBoxSelectedPointerOverBrushValue();
		set => SetCheckBoxSelectedPointerOverBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxSelectedPointerOverBrushProperty { get; } = CreateCheckBoxSelectedPointerOverBrushProperty();

	#endregion

	#region CheckBoxSelectedPressedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected pressed multi-select check box background.
	/// </summary>
	public Brush? CheckBoxSelectedPressedBrush
	{
		get => GetCheckBoxSelectedPressedBrushValue();
		set => SetCheckBoxSelectedPressedBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxSelectedPressedBrushProperty { get; } = CreateCheckBoxSelectedPressedBrushProperty();

	#endregion

	#region CheckBoxSelectedDisabledBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selected disabled multi-select check box background.
	/// </summary>
	public Brush? CheckBoxSelectedDisabledBrush
	{
		get => GetCheckBoxSelectedDisabledBrushValue();
		set => SetCheckBoxSelectedDisabledBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxSelectedDisabledBrushProperty { get; } = CreateCheckBoxSelectedDisabledBrushProperty();

	#endregion

	#region CheckBoxBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the multi-select check box border.
	/// </summary>
	public Brush? CheckBoxBorderBrush
	{
		get => GetCheckBoxBorderBrushValue();
		set => SetCheckBoxBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxBorderBrushProperty { get; } = CreateCheckBoxBorderBrushProperty();

	#endregion

	#region CheckBoxPointerOverBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pointer-over multi-select check box border.
	/// </summary>
	public Brush? CheckBoxPointerOverBorderBrush
	{
		get => GetCheckBoxPointerOverBorderBrushValue();
		set => SetCheckBoxPointerOverBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxPointerOverBorderBrushProperty { get; } = CreateCheckBoxPointerOverBorderBrushProperty();

	#endregion

	#region CheckBoxPressedBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pressed multi-select check box border.
	/// </summary>
	public Brush? CheckBoxPressedBorderBrush
	{
		get => GetCheckBoxPressedBorderBrushValue();
		set => SetCheckBoxPressedBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxPressedBorderBrushProperty { get; } = CreateCheckBoxPressedBorderBrushProperty();

	#endregion

	#region CheckBoxDisabledBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the disabled multi-select check box border.
	/// </summary>
	public Brush? CheckBoxDisabledBorderBrush
	{
		get => GetCheckBoxDisabledBorderBrushValue();
		set => SetCheckBoxDisabledBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxDisabledBorderBrushProperty { get; } = CreateCheckBoxDisabledBorderBrushProperty();

	#endregion

	#region CheckBoxCornerRadius DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: corner radius set on the multi-select check box.
	/// </summary>
	public CornerRadius CheckBoxCornerRadius
	{
		get => GetCheckBoxCornerRadiusValue();
		set => SetCheckBoxCornerRadiusValue(value);
	}

	private static CornerRadius GetCheckBoxCornerRadiusDefaultValue() => ListViewBaseItemChrome.s_defaultCheckBoxCornerRadius;

	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty CheckBoxCornerRadiusProperty { get; } = CreateCheckBoxCornerRadiusProperty();

	#endregion

	#region SelectionIndicatorCornerRadius DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: corner radius set on the multi-select check box.
	/// </summary>
	public CornerRadius SelectionIndicatorCornerRadius
	{
		get => GetSelectionIndicatorCornerRadiusValue();
		set => SetSelectionIndicatorCornerRadiusValue(value);
	}

	private static CornerRadius GetSelectionIndicatorCornerRadiusDefaultValue() => ListViewBaseItemChrome.s_defaultSelectionIndicatorCornerRadius;

	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectionIndicatorCornerRadiusProperty { get; } = CreateSelectionIndicatorCornerRadiusProperty();

	#endregion

	#region SelectionIndicatorVisualEnabled DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: control whether selection indicator is shown or not.
	/// </summary>
	public bool SelectionIndicatorVisualEnabled
	{
		get => GetSelectionIndicatorVisualEnabledValue();
		set => SetSelectionIndicatorVisualEnabledValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = false, LocalCache = false, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectionIndicatorVisualEnabledProperty { get; } = CreateSelectionIndicatorVisualEnabledProperty();

	#endregion

	#region SelectionIndicatorMode DependencyProperty

	/// <summary>
	/// Gets or sets the selection indicator mode.
	/// </summary>
	public ListViewItemPresenterSelectionIndicatorMode SelectionIndicatorMode
	{
		get => GetSelectionIndicatorModeValue();
		set => SetSelectionIndicatorModeValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = ListViewItemPresenterSelectionIndicatorMode.Overlay, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectionIndicatorModeProperty { get; } = CreateSelectionIndicatorModeProperty();

	#endregion

	#region SelectionIndicatorBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the selection indicator.
	/// </summary>
	public Brush? SelectionIndicatorBrush
	{
		get => GetSelectionIndicatorBrushValue();
		set => SetSelectionIndicatorBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectionIndicatorBrushProperty { get; } = CreateSelectionIndicatorBrushProperty();

	#endregion

	#region SelectionIndicatorPointerOverBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pointer-over selection indicator.
	/// </summary>
	public Brush? SelectionIndicatorPointerOverBrush
	{
		get => GetSelectionIndicatorPointerOverBrushValue();
		set => SetSelectionIndicatorPointerOverBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectionIndicatorPointerOverBrushProperty { get; } = CreateSelectionIndicatorPointerOverBrushProperty();

	#endregion

	#region SelectionIndicatorPressedBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pressed selection indicator.
	/// </summary>
	public Brush? SelectionIndicatorPressedBrush
	{
		get => GetSelectionIndicatorPressedBrushValue();
		set => SetSelectionIndicatorPressedBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectionIndicatorPressedBrushProperty { get; } = CreateSelectionIndicatorPressedBrushProperty();

	#endregion

	#region SelectionIndicatorDisabledBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the disabled selection indicator.
	/// </summary>
	public Brush? SelectionIndicatorDisabledBrush
	{
		get => GetSelectionIndicatorDisabledBrushValue();
		set => SetSelectionIndicatorDisabledBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectionIndicatorDisabledBrushProperty { get; } = CreateSelectionIndicatorDisabledBrushProperty();

	#endregion

	#region SelectedBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the outer selection border.
	/// </summary>
	public Brush? SelectedBorderBrush
	{
		get => GetSelectedBorderBrushValue();
		set => SetSelectedBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedBorderBrushProperty { get; } = CreateSelectedBorderBrushProperty();

	#endregion

	#region SelectedPressedBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the outer selection pressed border.
	/// </summary>
	public Brush? SelectedPressedBorderBrush
	{
		get => GetSelectedPressedBorderBrushValue();
		set => SetSelectedPressedBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedPressedBorderBrushProperty { get; } = CreateSelectedPressedBorderBrushProperty();

	#endregion

	#region SelectedDisabledBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the outer selection disabled border.
	/// </summary>
	public Brush? SelectedDisabledBorderBrush
	{
		get => GetSelectedDisabledBorderBrushValue();
		set => SetSelectedDisabledBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedDisabledBorderBrushProperty { get; } = CreateSelectedDisabledBorderBrushProperty();

	#endregion

	#region SelectedInnerBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the inner selection border.
	/// </summary>
	public Brush? SelectedInnerBorderBrush
	{
		get => GetSelectedInnerBorderBrushValue();
		set => SetSelectedInnerBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty SelectedInnerBorderBrushProperty { get; } = CreateSelectedInnerBorderBrushProperty();

	#endregion

	#region PointerOverBorderBrush DependencyProperty

	/// <summary>
	/// Gets or sets a value that describes the following: brush for the pointer-over outer border.
	/// </summary>
	public Brush? PointerOverBorderBrush
	{
		get => GetPointerOverBorderBrushValue();
		set => SetPointerOverBorderBrushValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = null, Options = FrameworkPropertyMetadataOptions.AffectsRender, ChangedCallbackName = nameof(OnChromePropertyChanged))]
	public static DependencyProperty PointerOverBorderBrushProperty { get; } = CreatePointerOverBorderBrushProperty();

	#endregion
}
