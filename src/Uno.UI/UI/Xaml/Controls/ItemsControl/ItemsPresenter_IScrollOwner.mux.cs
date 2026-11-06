// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsPresenter_IScrollOwner.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

#if !HAS_UNO // TODO Uno: Uno's ScrollViewer does not use IScrollOwner with the ItemsPresenter

using System;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsPresenter
{
	// IScrollOwner implementation.

	public void InvalidateScrollInfo()
	{
		var spScrollOwner = m_ScrollData.ScrollOwner;

		if (spScrollOwner is not null)
		{
			// We have layout cycle if pass it to the owner
			// Since we are controlling scrolling data is is safe to not pass it to the owner

			// Need to invalidate our own measure to ensure that we update the scroll information
			// which includes the scroll bars
			InvalidateMeasure();
		}
	}

	// Notifies the scroll owner that an ArrangeOverride execution occurred
	// after its IManipulationDataProvider::UpdateInManipulation(...) call.
	public void NotifyLayoutRefreshed()
	{
		var spScrollOwner = m_ScrollData.ScrollOwner;
		if (spScrollOwner is not null)
		{
			spScrollOwner.NotifyLayoutRefreshed();
		}
	}

	// Called to notify the scroll owner of a new horizontal offset request.
	public void NotifyHorizontalOffsetChanging(double targetHorizontalOffset, double targetVerticalOffset)
	{
		var spScrollOwner = m_ScrollData.ScrollOwner;
		if (spScrollOwner is not null)
		{
			var isHorizontal = IsHorizontal();
			spScrollOwner.NotifyHorizontalOffsetChanging(isHorizontal ? targetHorizontalOffset + 2 : targetHorizontalOffset,
														 isHorizontal ? targetVerticalOffset : targetVerticalOffset + 2);
		}
	}

	// Called to notify the scroll owner of a new vertical offset request.
	public void NotifyVerticalOffsetChanging(double targetHorizontalOffset, double targetVerticalOffset)
	{
		var spScrollOwner = m_ScrollData.ScrollOwner;
		if (spScrollOwner is not null)
		{
			var isHorizontal = IsHorizontal();
			spScrollOwner.NotifyVerticalOffsetChanging(isHorizontal ? targetHorizontalOffset + 2 : targetHorizontalOffset,
													   isHorizontal ? targetVerticalOffset : targetVerticalOffset + 2);
		}
	}

	// Scrolls the content within the scroll owner to the specified
	// horizontal offset position.
	public void ScrollToHorizontalOffset(double offset)
	{
		var isHorizontal = IsHorizontal();
		SetHorizontalOffset(isHorizontal ? offset + 2 : offset);
	}

	// Scrolls the content within the scroll owner to the specified vertical
	// offset position.
	public void ScrollToVerticalOffset(double offset)
	{
		var isHorizontal = IsHorizontal();
		SetVerticalOffset(isHorizontal ? offset : offset + 2);
	}

	// Sets reference to the IScrollInfo implementation
	public IScrollInfo? ScrollInfo
	{
		get
		{
			return Panel as IScrollInfo;
		}
		set
		{
			var spPanelsInfo = (IScrollInfo)Panel!;

			if (!ReferenceEquals(spPanelsInfo, value))
			{
				throw new InvalidOperationException("E_FAIL");
			}
		}
	}

	// Returns zoom factor
	public float ZoomFactor
	{
		get
		{
			var value = 0.0f;
			var spScrollOwner = m_ScrollData.ScrollOwner;

			if (spScrollOwner is not null)
			{
				value = spScrollOwner.ZoomFactor;
			}

			return value;
		}
	}

	// Called when this DM container wants the DM handler to process the current
	// pure inertia input message, by forwarding it to DirectManipulation.
	// The handler must set the isHandled flag to True if the message was handled.
	// Unfortunately, callers to this method must determine whether or not DM will treat
	// the current input message as a pure inertia manipulation.
	// PLEASE NOTE: You won't find an input message as a parameter to this function.
	// The implementation just calls ProcessInputMessage on the manipulation handler
	// (in most cases).
	public void ProcessPureInertiaInputMessage(ZoomDirection zoomDirection)
	{
		var spScrollOwner = m_ScrollData.ScrollOwner;

		if (spScrollOwner is not null)
		{
			spScrollOwner.ProcessPureInertiaInputMessage(zoomDirection);
		}
	}

	// Returns true if currently in DM zooming
	public bool IsInDirectManipulationZoom()
	{
		var bIsInDirectManipulationZoom = false;
		var spScrollOwner = m_ScrollData.ScrollOwner;

		if (spScrollOwner is not null)
		{
			bIsInDirectManipulationZoom = spScrollOwner.IsInDirectManipulationZoom();
		}

		return bIsInDirectManipulationZoom;
	}

	// We cannot invalidate the grandchild directly. So this property is
	// informing that we are invalidating the child so the grandchild can
	// use it.
	public bool IsInChildInvalidateMeasure()
	{
		var bIsInChildInvalidateMeasure = false;
		var spScrollOwner = m_ScrollData.ScrollOwner;

		if (spScrollOwner is not null)
		{
			bIsInChildInvalidateMeasure = spScrollOwner.IsInChildInvalidateMeasure();
		}

		return bIsInChildInvalidateMeasure;
	}
}

#endif
