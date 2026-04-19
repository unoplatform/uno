// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollInputHelper.h, commit 5f9e85113

using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

using FxScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;
using FxZoomMode = Microsoft.UI.Xaml.Controls.ZoomMode;

namespace Microsoft.UI.Xaml.Controls;

partial class ScrollInputHelper
{
	// Property names inside the composition property set returned by get_SourcePropertySet.
	private const string s_horizontalOffsetPropertyName = "TranslationX";
	private const string s_verticalOffsetPropertyName = "TranslationY";
	private const string s_scalePropertyName = "Scale";

	private Action<bool, bool> m_infoChangedFunction;

	private UIElement m_sourceElement;
	private UIElement m_targetElement;
	private FxScrollViewer m_scrollViewer;
	private ScrollPresenter m_scrollPresenter;
	private FrameworkElement m_sourceContent;
	private RichEditBox m_richEditBox;
	private CompositionPropertySet m_internalSourcePropertySet;
	private CompositionPropertySet m_sourcePropertySet;
	private CompositionPropertySet m_scrollViewerPropertySet;
	private ExpressionAnimation m_internalTranslationXExpressionAnimation;
	private ExpressionAnimation m_internalTranslationYExpressionAnimation;
	private ExpressionAnimation m_internalScaleExpressionAnimation;
	private FxZoomMode m_manipulationZoomMode = FxZoomMode.Disabled;
	private HorizontalAlignment m_manipulationHorizontalAlignment = HorizontalAlignment.Stretch;
	private VerticalAlignment m_manipulationVerticalAlignment = VerticalAlignment.Stretch;
	private Size m_viewportSize = new Size(0.0f, 0.0f);
	private Size m_contentSize = new Size(0.0f, 0.0f);
	private Size m_outOfBoundsPanSize = new Size(0.0f, 0.0f);
	private bool m_isTargetElementInSource;
	private bool m_isScrollViewerInDirectManipulation;

	// Event Tokens.
	// C++ used winrt::event_token for both property-changed callbacks and regular events;
	// in C# we use long tokens for RegisterPropertyChangedCallback and stored delegate
	// references for regular events so they can be unsubscribed later.
	private long m_targetElementLoadedToken;
	private RoutedEventHandler m_targetElementLoadedHandler;
	private long m_sourceElementLoadedToken;
	private RoutedEventHandler m_sourceElementLoadedHandler;
	private long m_sourceControlTemplateChangedToken;
	private long m_sourceSizeChangedToken;
	private SizeChangedEventHandler m_sourceSizeChangedHandler;
	private long m_sourceContentSizeChangedToken;
	private SizeChangedEventHandler m_sourceContentSizeChangedHandler;
	private long m_scrollViewerContentHorizontalAlignmentChangedToken;
	private long m_scrollViewerContentVerticalAlignmentChangedToken;
	private long m_scrollViewerContentChangedToken;
	private long m_scrollPresenterContentChangedToken;
	private long m_scrollViewerHorizontalContentAlignmentChangedToken;
	private long m_scrollViewerVerticalContentAlignmentChangedToken;
	private long m_scrollViewerZoomModeChangedToken;
#if !HAS_UNO
	private long m_scrollViewerDirectManipulationStartedToken;
	private EventHandler<object> m_scrollViewerDirectManipulationStartedHandler;
	private long m_scrollViewerDirectManipulationCompletedToken;
	private EventHandler<object> m_scrollViewerDirectManipulationCompletedHandler;
#endif
	private long m_richEditBoxTextChangedToken;
	// Note: Uno declares RichEditBox.TextChanged as RoutedEventHandler (not TextChangedEventHandler).
	private RoutedEventHandler m_richEditBoxTextChangedHandler;
	private long m_renderingToken;
	private EventHandler<object> m_renderingHandler;
}
