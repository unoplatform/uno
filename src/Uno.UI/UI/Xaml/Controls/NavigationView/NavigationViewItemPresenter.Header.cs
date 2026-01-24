// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemPresenter.h, commit 65718e2813

using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class NavigationViewItemPresenter
{
	private bool m_isChevronPressed;
	private double m_compactPaneLengthValue = 40;
	private double m_leftIndentation;
	private uint m_trackedPointerId;

	private NavigationViewItemHelper<NavigationViewItemPresenter> m_helper;
	private Grid m_contentGrid;
	private ContentPresenter m_infoBadgePresenter;
	private Grid m_expandCollapseChevron;

	private readonly SerialDisposable m_expandCollapseChevronPointerPressedRevoker = new();
	private readonly SerialDisposable m_expandCollapseChevronPointerReleasedRevoker = new();
	private readonly SerialDisposable m_expandCollapseChevronPointerExitedRevoker = new();
	private readonly SerialDisposable m_expandCollapseChevronPointerCanceledRevoker = new();
	private readonly SerialDisposable m_expandCollapseChevronPointerCaptureLostRevoker = new();

	private Storyboard m_chevronExpandedStoryboard;
	private Storyboard m_chevronCollapsedStoryboard;
}
