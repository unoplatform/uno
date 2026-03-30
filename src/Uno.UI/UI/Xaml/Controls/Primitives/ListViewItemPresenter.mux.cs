// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewItemPresenter_Partial.cpp, tag winui3/release/1.4.2

#nullable enable

using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class ListViewItemPresenter
{
	/// <summary>
	/// Called when the VisualStateManager requests a visual state change.
	/// MUX Reference: ListViewItemPresenter::GoToElementStateCoreImpl
	/// </summary>
	protected override bool GoToElementStateCore(string stateName, bool useTransitions)
	{
		// In WinUI:
		// CListViewBaseItemChrome* pChrome = static_cast<CListViewBaseItemChrome*>(GetHandle());
		// IFC_RETURN(pChrome->GoToChromedState(HStringUtil::GetRawBuffer(stateName, nullptr), useTransitions, &wentToState));
		// IFC_RETURN(ProcessAnimationCommands());

		var wentToState = Chrome.GoToChromedState(stateName, useTransitions);

		ProcessAnimationCommands();

		// In WinUI, this also calls __super::GoToElementStateCoreImpl
		// which goes to the ContentPresenter base class
		// TODO Uno: Consider if base.GoToElementStateCore should be called
		// The base ContentPresenter.GoToElementStateCore may handle template-based visual states

		return wentToState;
	}

	/// <summary>
	/// Gets or sets the padding for list view item content.
	/// This property maps to ContentPresenter.Padding in WinUI.
	/// MUX Reference: ListViewItemPresenter::get_ListViewItemPresenterPaddingImpl / put_ListViewItemPresenterPaddingImpl
	/// </summary>
	/// <remarks>
	/// In WinUI, these methods delegate to:
	/// GetValueByKnownIndex(KnownPropertyIndex::ContentPresenter_Padding, pValue);
	/// SetValueByKnownIndex(KnownPropertyIndex::ContentPresenter_Padding, value);
	/// </remarks>
	// The ListViewItemPresenterPadding property is already defined in generated code
	// and maps to a separate dependency property. In WinUI it aliases ContentPresenter.Padding.

	/// <summary>
	/// Gets or sets the horizontal alignment of list view item content.
	/// This property maps to ContentPresenter.HorizontalContentAlignment in WinUI.
	/// MUX Reference: ListViewItemPresenter::get_ListViewItemPresenterHorizontalContentAlignmentImpl / put_ListViewItemPresenterHorizontalContentAlignmentImpl
	/// </summary>
	/// <remarks>
	/// In WinUI, these methods delegate to:
	/// GetValueByKnownIndex(KnownPropertyIndex::ContentPresenter_HorizontalContentAlignment, pValue);
	/// SetValueByKnownIndex(KnownPropertyIndex::ContentPresenter_HorizontalContentAlignment, value);
	/// </remarks>
	// The ListViewItemPresenterHorizontalContentAlignment property is already defined in generated code

	/// <summary>
	/// Gets or sets the vertical alignment of list view item content.
	/// This property maps to ContentPresenter.VerticalContentAlignment in WinUI.
	/// MUX Reference: ListViewItemPresenter::get_ListViewItemPresenterVerticalContentAlignmentImpl / put_ListViewItemPresenterVerticalContentAlignmentImpl
	/// </summary>
	/// <remarks>
	/// In WinUI, these methods delegate to:
	/// GetValueByKnownIndex(KnownPropertyIndex::ContentPresenter_VerticalContentAlignment, pValue);
	/// SetValueByKnownIndex(KnownPropertyIndex::ContentPresenter_VerticalContentAlignment, value);
	/// </remarks>
	// The ListViewItemPresenterVerticalContentAlignment property is already defined in generated code

#if DEBUG
#pragma warning disable CS0169 // Field is never used (partial implementation)
	// MUX Reference: ListViewItemPresenter::SetRoundedListViewBaseItemChromeFallbackColor
	// Sets default fallback light theme colors for testing purposes, for cases where the properties
	// are not set in markup and the rounded corner style is forced.
	// This is only used in debug builds in WinUI.
	private void SetRoundedListViewBaseItemChromeFallbackColor(DependencyProperty property, uint color)
	{
		var currentValue = GetValue(property);
		if (currentValue == null)
		{
			var brush = new SolidColorBrush(Color.FromArgb(
				(byte)((color >> 24) & 0xFF),
				(byte)((color >> 16) & 0xFF),
				(byte)((color >> 8) & 0xFF),
				(byte)(color & 0xFF)));
			SetValue(property, brush);
		}
	}

	// MUX Reference: ListViewItemPresenter::SetRoundedListViewBaseItemChromeFallbackColors
	private bool m_roundedListViewBaseItemChromeFallbackColorsSet;
#pragma warning restore CS0169

	private void SetRoundedListViewBaseItemChromeFallbackColors()
	{
		if (ListViewBaseItemChrome.IsRoundedListViewBaseItemChromeForced())
		{
			SetRoundedListViewBaseItemChromeFallbackColor(PointerOverBorderBrushProperty, 0x37000000);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckPressedBrushProperty, 0xB2FFFFFF);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckDisabledBrushProperty, 0xFFFFFFFF);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxPointerOverBrushProperty, 0xFFF3F3F3);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxPressedBrushProperty, 0xFFEBEBEB);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxDisabledBrushProperty, 0x06000000);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxSelectedBrushProperty, 0xFF0070CB);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxSelectedPointerOverBrushProperty, 0xFF0065BD);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxSelectedPressedBrushProperty, 0xFF007DD5);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxSelectedDisabledBrushProperty, 0x37000000);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxBorderBrushProperty, 0x71000000);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxPointerOverBorderBrushProperty, 0x71000000);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxPressedBorderBrushProperty, 0x37000000);
			SetRoundedListViewBaseItemChromeFallbackColor(CheckBoxDisabledBorderBrushProperty, 0x37000000);
			SetRoundedListViewBaseItemChromeFallbackColor(SelectedDisabledBackgroundProperty, 0x06000000);
			SetRoundedListViewBaseItemChromeFallbackColor(SelectedBorderBrushProperty, 0xFF0070CB);
			SetRoundedListViewBaseItemChromeFallbackColor(SelectedPressedBorderBrushProperty, 0xFF007DD5);
			SetRoundedListViewBaseItemChromeFallbackColor(SelectedDisabledBorderBrushProperty, 0x37000000);
			SetRoundedListViewBaseItemChromeFallbackColor(SelectedInnerBorderBrushProperty, 0xFFFFFFFF);
		}

		if (ListViewBaseItemChrome.IsSelectionIndicatorVisualForced())
		{
			SetRoundedListViewBaseItemChromeFallbackColor(SelectionIndicatorBrushProperty, 0xFF0070CB);
			SetRoundedListViewBaseItemChromeFallbackColor(SelectionIndicatorPointerOverBrushProperty, 0xFF0070CB);
			SetRoundedListViewBaseItemChromeFallbackColor(SelectionIndicatorPressedBrushProperty, 0xFF0070CB);
			SetRoundedListViewBaseItemChromeFallbackColor(SelectionIndicatorDisabledBrushProperty, 0x37000000);
		}
	}
#endif
}
