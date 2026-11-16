// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\phone\lib\PivotPanel_Partial.cpp, commit 4a1c6184c

#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class PivotPanel
{
	/// <summary>
	/// Initializes a new instance of the PivotPanel class.
	/// </summary>
	public PivotPanel()
	{
		m_availableWidth = 0.0;
		m_headerHeight = 0.0;
	}

	internal void SetParentPivot(Pivot? pivot)
	{
		m_parentPivotWeakRef = null;

		if (pivot != null)
		{
			m_parentPivotWeakRef = new WeakReference<Pivot>(pivot);
		}
	}

	internal void SetSectionWidth(double availableWidth)
	{
		m_availableWidth = availableWidth;

		InvalidateMeasure();

		// The snap points are based on window width, we know them
		// after a measure pass has occurred on the ScrollViewer's parent
		// and this function is called.
#if !HAS_UNO // TODO Uno: IScrollSnapPointsInfo is not implemented on PivotPanel.
		RaiseSnapPointsChangedEvents();
#endif
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
#if !HAS_UNO // TODO Uno: Pivot does not track header secondary content relationships.
		if (m_parentPivotWeakRef is not null && m_parentPivotWeakRef.TryGetTarget(out var parentPivot))
		{
			parentPivot.InvalidateHeaderSecondaryContentRelationships();
		}
#endif

		var spChildrenVect = Children;

		var vectSize = spChildrenVect.Count;

		for (int childIdx = 0; childIdx < vectSize; childIdx++)
		{
			var spChild = spChildrenVect[childIdx];
			bool didArrange = false;

			if (spChild is FrameworkElement spChildAsFE)
			{
				var childName = spChildAsFE.Name;

				// Why StringReference requries this const cast is beyond
				// my abilities to understand.

				if (childName == Pivot.c_LayoutElementName)
				{
					Rect finalArrangingRect = default;
					finalArrangingRect.Width = m_availableWidth;
					finalArrangingRect.Height = finalSize.Height;
					spChild.Arrange(finalArrangingRect);
					didArrange = true;
				}
				else if (childName == Pivot.c_PivotItemsPresenterName)
				{
					Rect finalArrangingRect = default;
					finalArrangingRect.Y = m_headerHeight;
					finalArrangingRect.Height = finalSize.Height - m_headerHeight;
					// Lay all the elements out with the fixed width
					// they were measured with.
					finalArrangingRect.Width = m_availableWidth;

					spChild.Arrange(finalArrangingRect);
					didArrange = true;
				}
				else if (childName == Pivot.c_HeadersControlName)
				{
					Rect finalArrangingRect = default;
					finalArrangingRect.Height = m_headerHeight;
					finalArrangingRect.Width = finalSize.Width;

					spChild.Arrange(finalArrangingRect);
					didArrange = true;
				}
			}

			// Not sure what these other UI elements are doing in
			// our panel.
			if (!didArrange)
			{
				Rect finalArrangingRect = default;
				finalArrangingRect.Width = finalSize.Width;
				finalArrangingRect.Height = finalSize.Height;
				spChild.Arrange(finalArrangingRect);
			}
		}

		return finalSize;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		UIElement? spLayoutElement = null;
		UIElement? spItemsPresenter = null;
		UIElement? spHeaderControl = null;

		Size desiredSize = default;

		var spChildrenVect = Children;

		var vectSize = spChildrenVect.Count;

		// First we iterate through the collection and find the
		// header to measure it first. We do this to store its
		// desired size for laying out the ItensPresenter properly.
		for (int childIdx = 0; childIdx < vectSize; childIdx++)
		{
			var spChild = spChildrenVect[childIdx];
			bool isExpectedElement = false;

			if (spChild is FrameworkElement spChildAsFE)
			{
				var childName = spChildAsFE.Name;

				if (childName == Pivot.c_LayoutElementName)
				{
					isExpectedElement = true;
					spLayoutElement = spChild;
				}
				else if (childName == Pivot.c_HeadersControlName)
				{
					isExpectedElement = true;
					spHeaderControl = spChild;
				}
				else if (childName == Pivot.c_PivotItemsPresenterName)
				{
					isExpectedElement = true;
					spItemsPresenter = spChild;
				}
			}

			if (!isExpectedElement)
			{
				spChild.Measure(availableSize);
			}
		}

		// In the Windows 10 template, we expect spLayoutElement which
		// will have spHeaderControl and spItemsPresenter as descendants.
		// Layout under spLayoutElement is flexible and follow xaml rules.
		if ((spHeaderControl is null && spLayoutElement is null) || (spItemsPresenter is null && spLayoutElement is null))
		{
			throw new InvalidOperationException(
				$"PivotPanel expects a '{Pivot.c_LayoutElementName}' child, or '{Pivot.c_HeadersControlName}' and '{Pivot.c_PivotItemsPresenterName}' children.");
		}

		// In windows 10, we measure the layout element instead of measuring directly the
		// items presenter and the header panel.
		if (spLayoutElement is not null)
		{
			Size constrainedAvailableSize = new(m_availableWidth, availableSize.Height);
			spLayoutElement.Measure(constrainedAvailableSize);
			desiredSize = spLayoutElement.DesiredSize;
		}

		if (spHeaderControl is not null)
		{
			spHeaderControl.Measure(availableSize);
			// Grab the desired height of the header so we can
			// lay everthing out in the arrange pass, subtracting
			// and offsetting the height from the item presenter panel.
			var desiredHeaderSize = spHeaderControl.DesiredSize;
			m_headerHeight = desiredHeaderSize.Height;
		}

		// Sets the available width to be the container's width to
		// lay out the PivotItems with a fixed size, even though
		// we're in a ScrollViewer that will measure with infinte
		// extents.
		if (spItemsPresenter is not null)
		{
			Size constrainedItemsPresenterSize = default;
			constrainedItemsPresenterSize.Height = availableSize.Height - m_headerHeight;
			constrainedItemsPresenterSize.Width = m_availableWidth;
			spItemsPresenter.Measure(constrainedItemsPresenterSize);

			desiredSize = spItemsPresenter.DesiredSize;
		}

		Size pDesiredSize = default;

		// Set the desired height to be the size of the header and item
		// presenter to avoid returning infinite sizes.
		pDesiredSize.Height = desiredSize.Height + m_headerHeight;

		// Sets the desired width to be a large multiple of the
		// container's width to allow scrolling and align snap points.
		// If placed in an infinite panel we instead fall back on the
		// desired item presenter width.
		if (double.IsPositiveInfinity(m_availableWidth))
		{
			m_availableWidth = desiredSize.Width;
		}

		uint panelMultiplier = Pivot.GetPivotPanelMultiplier();
		if (m_parentPivotWeakRef is not null && m_parentPivotWeakRef.TryGetTarget(out var parentPivot))
		{
			panelMultiplier = parentPivot.GetPivotPanelMultiplierImpl();
		}

		pDesiredSize.Width = m_availableWidth * panelMultiplier;

		return pDesiredSize;
	}

#if !HAS_UNO // TODO Uno: IScrollSnapPointsInfo is not implemented on PivotPanel, as Pivot does not pan its ScrollViewer between sections.
	bool AreHorizontalSnapPointsRegular => true;

	bool AreVerticalSnapPointsRegular => true;

	IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
	{
		// NOTE: This method should never be called, both
		// horizontal and vertical SnapPoints are ALWAYS regular.
		throw new NotImplementedException();
	}

	float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset)
	{
		// For now the PivotPanel will simply return a evenly spaced grid,
		// the vertical and horizontal snap points will be identical.
		offset = (float)(m_availableWidth / 2);
		return (float)m_availableWidth;
	}

	void RaiseSnapPointsChangedEvents()
	{
		VerticalSnapPointsChanged?.Invoke(this, this);
		HorizontalSnapPointsChanged?.Invoke(this, this);
	}
#endif
}
