// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IScrollOwner.h, commit 5f9e85113

namespace DirectUI
{
	/// <summary>
	///     Interface through which a panel
	///     communicates with its scrolling owner.
	/// </summary>
	internal interface IScrollOwner
	{
		// This function is called by an IScrollInfo attached to this
		// scrolling owner when any values of scrolling properties (Offset, Extent,
		// and ViewportSize) change.  The function schedules invalidation of
		// other elements like ScrollBars that are dependant on these
		// properties.
		void InvalidateScrollInfoImpl();

		// Called to notify the scroll owner that the first ArrangeOverride occurred after a
		// IManipulationDataProvider::UpdateInManipulation(...) call.
		// Marks the transition into or out of the 'manipulation arrangement' mode.
		void NotifyLayoutRefreshed();

		// Called to notify the scroll owner of a new horizontal offset request
		void NotifyHorizontalOffsetChanging(
			double targetHorizontalOffset,
			double targetVerticalOffset);

		// Called to notify the scroll owner of a new vertical offset request
		void NotifyVerticalOffsetChanging(
			double targetHorizontalOffset,
			double targetVerticalOffset);

		// Scrolls the content within the scroll owner to the specified
		// horizontal offset position.
		void ScrollToHorizontalOffsetImpl(double offset);

		// Scrolls the content within the scroll owner to the specified vertical
		// offset position.
		void ScrollToVerticalOffsetImpl(double offset);

		// Sets reference to the IScrollInfo implementation
		void SetScrollInfo(IScrollInfo value);

		// Returns reference to the IScrollInfo implementation
		IScrollInfo GetScrollInfo();

		// Returns zoom factor
		// this API API will go away late as we finish discovering all scenarios where we need to propagate new zoom factor.
		// deprecated.
		float GetZoomFactor();

		// Called when this DM container wants the DM handler to process the current
		// pure inertia input message, by forwarding it to DirectManipulation.
		void ProcessPureInertiaInputMessage(ZoomDirection zoomDirection);

		// Returns true if currently in DM zooming
		bool IsInDirectManipulationZoom();

		// We cannot invalidate the grandchild directly. So this property is
		// informing that we are invalidating the child so the grandchild can
		// use it.
		bool IsInChildInvalidateMeasure();
	}
}
