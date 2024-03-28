// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

internal sealed partial class ScrollBarController : IScrollController
{
	// Private constants

	// Default amount to scroll when hitting the SmallIncrement/SmallDecrement buttons: 1/8 of the viewport size.
	// This amount can be overridden by setting the ScrollBar.SmallChange property to something else than double.NaN.
	private const double s_defaultViewportToSmallChangeRatio = 8.0;

	// Inertia decay rate for SmallChange / LargeChange animated Value changes.
	private const float s_inertiaDecayRate = 0.9995f;

	// Additional velocity required with decay s_inertiaDecayRate to move Position by one pixel.
	private const double s_velocityNeededPerPixel = 7.600855902349023;

	// Additional velocity at Minimum and Maximum positions to ensure hitting the extreme Value.
	private const double s_minMaxEpsilon = 0.001;

	private ScrollBar m_scrollBar;
	private int m_lastOffsetChangeCorrelationIdForScrollTo = -1;
	private int m_lastOffsetChangeCorrelationIdForScrollBy = -1;
	private int m_lastOffsetChangeCorrelationIdForAddScrollVelocity = -1;
	private int m_operationsCount;
	private double m_lastScrollBarValue;
	private double m_lastOffset;
	private bool m_canScroll;
	private bool m_isScrollingWithMouse;
	private bool m_isScrollable;

	// Event Tokens
	private SerialDisposable m_scrollBarScrollToken;
	//private long m_visibilityChangedToken;
	private long m_scrollBarIsEnabledChangedToken;
#if DEBUG
	// For testing purposes only
	private long m_scrollBarIndicatorModeChangedToken;
	private long m_scrollBarVisibilityChangedToken;
#endif //DBG
};

