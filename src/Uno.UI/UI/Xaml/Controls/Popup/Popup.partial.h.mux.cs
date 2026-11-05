// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Popup_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e

using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Popup
{
	private enum MajorPlacementMode
	{
		Auto,
		Top,
		Bottom,
		Left,
		Right,
	}

	private enum PreferredJustification
	{
		Auto,
		HorizontalCenter,
		VerticalCenter,
		Top,
		Bottom,
		Left,
		Right,
	}

	// TODO Uno: Investigate potential leak: the placement target's LayoutUpdated holds this popup until PlacementTarget changes.
	private readonly SerialDisposable m_placementTargetLayoutUpdatedToken = new();
	private readonly SerialDisposable m_childSizeChangedToken = new();
	private readonly SerialDisposable m_childLoadedToken = new();

	private bool m_placementAndJustificationCalculated;
	private MajorPlacementMode m_calculatedMajorPlacement;
	private PreferredJustification m_calculatedJustification;
}
