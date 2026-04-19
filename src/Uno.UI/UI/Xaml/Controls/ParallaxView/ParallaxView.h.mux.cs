// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParallaxView.h, commit 5f9e85113

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ParallaxView
{
	private ScrollInputHelper m_scrollInputHelper;
	private Visual m_targetVisual;
	private CompositionPropertySet m_animatedVariables;
	private ExpressionAnimation m_horizontalSourceStartOffsetExpression;
	private ExpressionAnimation m_horizontalSourceEndOffsetExpression;
	private ExpressionAnimation m_verticalSourceStartOffsetExpression;
	private ExpressionAnimation m_verticalSourceEndOffsetExpression;
	private ExpressionAnimation m_horizontalParallaxExpressionInternal;
	private ExpressionAnimation m_verticalParallaxExpressionInternal;
	private bool m_isHorizontalAnimationStarted;
	private bool m_isVerticalAnimationStarted;

	// The currently hooked child element for HorizontalAlignment/VerticalAlignment property-changed callbacks.
	private FrameworkElement m_currentListeningChild;

	// Event Tokens
	// C++ uses winrt::event_token; in C# we store the handler delegate so the subscription can be removed.
	private RoutedEventHandler m_loadedHandler;
	private long m_loadedToken;
	private SizeChangedEventHandler m_sizeChangedHandler;
	private long m_sizeChangedToken;
	private long m_childHorizontalAlignmentChangedToken;
	private long m_childVerticalAlignmentChangedToken;

	// Property names being targeted for the ParallaxView.Child's Visual.
	// RedStone v1 case:
	private const string s_transformMatrixTranslateXPropertyName = "TransformMatrix._41";
	private const string s_transformMatrixTranslateYPropertyName = "TransformMatrix._42";
	// RedStone v2 and higher case:
	private const string s_translationPropertyName = "Translation";
	private const string s_translationXPropertyName = "Translation.X";
	private const string s_translationYPropertyName = "Translation.Y";
}
