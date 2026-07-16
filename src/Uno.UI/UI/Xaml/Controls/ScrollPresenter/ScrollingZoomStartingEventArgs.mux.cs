// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollingZoomStartingEventArgs.cpp, commit b8cfb8490

namespace Microsoft.UI.Xaml.Controls;

partial class ScrollingZoomStartingEventArgs
{
	/// <summary>
	/// Gets the correlation identifier associated with the imminent zoom factor change.
	/// </summary>
	public int CorrelationId => m_correlationId;

	internal void SetCorrelationId(int correlationId) => m_correlationId = correlationId;

	/// <summary>
	/// Gets the anticipated horizontal offset once all queued view changes are completed.
	/// </summary>
	public double HorizontalOffset => m_horizontalOffset;

	internal void SetHorizontalOffset(double horizontalOffset) => m_horizontalOffset = horizontalOffset;

	/// <summary>
	/// Gets the anticipated vertical offset once all queued view changes are completed.
	/// </summary>
	public double VerticalOffset => m_verticalOffset;

	internal void SetVerticalOffset(double verticalOffset) => m_verticalOffset = verticalOffset;

	/// <summary>
	/// Gets the anticipated zoom factor once all queued zoom factor changes are completed.
	/// </summary>
	public float ZoomFactor => m_zoomFactor;

	internal void SetZoomFactor(float zoomFactor) => m_zoomFactor = zoomFactor;
}
