// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/1.4.2

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemChrome
{
	/// <summary>
	/// Measures the chrome elements for the new rounded style.
	/// Called by the presenter during its MeasureOverride.
	/// MUX Reference: CListViewBaseItemChrome::MeasureNewStyle
	/// </summary>
	internal Size MeasureNewStyle(Size availableSize)
	{
		var contentMargin = _contentMargin;
		var totalSize = new Size();

		// Ensure backplate if rounded chrome is enabled
		if (_backplateRectangle == null && IsRoundedListViewBaseItemChromeEnabled)
		{
			EnsureBackplate();
		}

		var templateChild = GetTemplateChildIfExists();
		var parent = GetParentListViewBaseItemNoRef();

		if (parent == null)
		{
			return availableSize;
		}

		// Calculate available size for content
		var contentAvailableSize = DeflateSize(availableSize, contentMargin);

		// Measure template child if present
		if (templateChild != null)
		{
			templateChild.Measure(contentAvailableSize);
			totalSize = templateChild.DesiredSize;
		}

		// Add content margin back
		totalSize = InflateSize(totalSize, contentMargin);

		// Measure chrome elements
		MeasureChromeElements(availableSize);

		return totalSize;
	}

	/// <summary>
	/// Arranges the chrome elements for the new rounded style.
	/// Called by the presenter during its ArrangeOverride.
	/// MUX Reference: CListViewBaseItemChrome::ArrangeNewStyle
	/// </summary>
	internal Size ArrangeNewStyle(Size finalSize)
	{
		var contentMargin = _contentMargin;
		var parent = GetParentListViewBaseItemNoRef();

		if (parent == null)
		{
			return finalSize;
		}

		// Calculate content bounds
		var contentBounds = new Rect(
			contentMargin.Left,
			contentMargin.Top,
			Math.Max(0, finalSize.Width - contentMargin.Left - contentMargin.Right),
			Math.Max(0, finalSize.Height - contentMargin.Top - contentMargin.Bottom));

		// Arrange template child
		var templateChild = GetTemplateChildIfExists();
		if (templateChild != null)
		{
			templateChild.Arrange(contentBounds);
		}

		// Arrange chrome elements
		ArrangeChromeElements(finalSize);

		return finalSize;
	}

	/// <summary>
	/// Measures chrome elements.
	/// </summary>
	private void MeasureChromeElements(Size availableSize)
	{
		// Measure backplate
		_backplateRectangle?.Measure(availableSize);

		// Measure outer border
		_outerBorder?.Measure(availableSize);

		// Measure inner selection border
		_innerSelectionBorder?.Measure(availableSize);

		// Measure multi-select checkbox
		if (_multiSelectCheckBoxRectangle != null)
		{
			_multiSelectCheckBoxRectangle.Measure(new Size(
				ListViewBaseItemChromeConstants.MultiSelectSquareSize.Width,
				ListViewBaseItemChromeConstants.MultiSelectSquareSize.Height));
		}

		// Measure selection indicator
		if (_selectionIndicatorRectangle != null)
		{
			_selectionIndicatorRectangle.Measure(new Size(
				ListViewBaseItemChromeConstants.SelectionIndicatorSize.Width,
				availableSize.Height));
		}
	}

	/// <summary>
	/// Arranges chrome elements.
	/// </summary>
	private void ArrangeChromeElements(Size finalSize)
	{
		// Arrange backplate (full size minus margins)
		if (_backplateRectangle != null)
		{
			var backplateMargin = ListViewBaseItemChromeConstants.BackplateMargin;
			var backplateBounds = new Rect(
				backplateMargin.Left,
				backplateMargin.Top,
				Math.Max(0, finalSize.Width - backplateMargin.Left - backplateMargin.Right),
				Math.Max(0, finalSize.Height - backplateMargin.Top - backplateMargin.Bottom));
			_backplateRectangle.Arrange(backplateBounds);
		}

		// Arrange outer border (full size)
		_outerBorder?.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));

		// Arrange inner selection border (full size)
		_innerSelectionBorder?.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));

		// Arrange multi-select checkbox
		if (_multiSelectCheckBoxRectangle != null)
		{
			var checkBoxMargin = _checkMode == ListViewItemPresenterCheckMode.Inline
				? ListViewBaseItemChromeConstants.MultiSelectSquareInlineMargin
				: ListViewBaseItemChromeConstants.MultiSelectSquareOverlayMargin;

			var checkBoxBounds = new Rect(
				checkBoxMargin.Left,
				(finalSize.Height - ListViewBaseItemChromeConstants.MultiSelectSquareSize.Height) / 2,
				ListViewBaseItemChromeConstants.MultiSelectSquareSize.Width,
				ListViewBaseItemChromeConstants.MultiSelectSquareSize.Height);
			_multiSelectCheckBoxRectangle.Arrange(checkBoxBounds);
		}

		// Arrange selection indicator
		if (_selectionIndicatorRectangle != null)
		{
			var indicatorMargin = ListViewBaseItemChromeConstants.SelectionIndicatorMargin;
			var indicatorHeight = GetSelectionIndicatorHeightFromAvailableHeight((float)finalSize.Height);

			var indicatorBounds = new Rect(
				indicatorMargin.Left,
				(finalSize.Height - indicatorHeight) / 2,
				ListViewBaseItemChromeConstants.SelectionIndicatorSize.Width,
				indicatorHeight);
			_selectionIndicatorRectangle.Arrange(indicatorBounds);
		}
	}

	#region Size Helpers

	private static Size DeflateSize(Size size, Thickness margin)
	{
		return new Size(
			Math.Max(0, size.Width - margin.Left - margin.Right),
			Math.Max(0, size.Height - margin.Top - margin.Bottom));
	}

	private static Size InflateSize(Size size, Thickness margin)
	{
		return new Size(
			size.Width + margin.Left + margin.Right,
			size.Height + margin.Top + margin.Bottom);
	}

	#endregion

	/// <summary>
	/// Gets the template child element if it exists.
	/// MUX Reference: CListViewBaseItemChrome::GetTemplateChildIfExists
	/// </summary>
	private UIElement? GetTemplateChildIfExists()
	{
		return _owner.Content as UIElement;
	}

	/// <summary>
	/// Calculates the selection indicator height based on available height.
	/// MUX Reference: CListViewBaseItemChrome::GetSelectionIndicatorHeightFromAvailableHeight
	/// </summary>
	private float GetSelectionIndicatorHeightFromAvailableHeight(float availableHeight)
	{
		// The selection indicator shrinks as the item gets smaller
		var shrinkage = ListViewBaseItemChromeConstants.SelectionIndicatorHeightShrinkage;
		return (float)Math.Max(
			ListViewBaseItemChromeConstants.SelectionIndicatorSize.Height,
			availableHeight - shrinkage * 2);
	}
}
