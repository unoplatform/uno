// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SemanticZoom_Partial.h, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class SemanticZoom
{
	private const string c_zoomedInPresenterName = "ZoomedInPresenter";
	private const string c_zoomedOutPresenterName = "ZoomedOutPresenter";
	private const string c_scrollViewerName = "ScrollViewer";
	private const string c_zoomOutButtonName = "ZoomOutButton";
	private const string c_zoomOutButtonVisibleState = "ZoomOutButtonVisible";
	private const string c_zoomOutButtonHiddenState = "ZoomOutButtonHidden";

	// TODO Uno: DirectManipulation-driven phases are not ported because Skia does not
	// currently expose the DirectManipulation state-change handler used by WinUI.

	// Reference to the template part hosting the ZoomedInView.
	private ContentPresenter? m_tpZoomedInPresenterPart;

	// Reference to the template part hosting the ZoomedOutView.
	private ContentPresenter? m_tpZoomedOutPresenterPart;

	// Reference to the ScrollViewer.
	private ScrollViewer? m_tpScrollViewer;

	// Reference to the ZoomOutButton template part.
	private Button? m_tpZoomOutButton;

	// This field is set to true while this SemanticZoom is being initialized.
	private bool m_isInitializing = true;

	// If IsZoomedInViewActive changes while SemanticZoom is still being initialized,
	// this flag postpones the view change until the template has been applied.
	private bool m_isPendingViewChange;

	private bool m_isProcessingKeyboardInput;
	private bool m_isProcessingPointerInput;
	private bool m_emulatingGesture;
	private bool m_isProcessingViewChange;
	private bool m_viewsChangedDuringViewChange;
	private bool m_isZoomOutButtonEnabled = true;
	private bool m_restoringActiveView;
	private Point m_zoomPoint;

	internal bool GetIsProcessingKeyboardInput() => m_isProcessingKeyboardInput;

	internal bool GetIsProcessingPointerInput() => m_isProcessingPointerInput;

	internal bool GetIsCancellingJumpList() => false;
}
