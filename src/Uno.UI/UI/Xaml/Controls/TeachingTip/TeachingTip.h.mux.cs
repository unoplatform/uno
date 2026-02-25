// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TeachingTip\TeachingTip.h, tag winui3/release/1.5.0, commit c8bd154c0

#nullable enable

using System;
using System.Numerics;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;

#pragma warning disable IDE0051 // Some of the constants in this file are defined in the WinUI source but are not used.

namespace Microsoft.UI.Xaml.Controls;

partial class TeachingTip
{
	private FrameworkElement m_target;

	internal bool m_isIdle = true;

	private readonly long m_automationNameChangedRevoker;
	private readonly long m_automationIdChangedRevoker;
	private readonly SerialDisposable m_acceleratorKeyActivatedRevoker = new();
	private readonly SerialDisposable m_previewKeyDownForF6Revoker = new();
	// This handler is not required for Winui3 because the framework bug this works around has been fixed.
	private readonly SerialDisposable m_popupPreviewKeyDownForF6Revoker = new();
	private readonly SerialDisposable m_lightDismissIndicatorPopupPreviewKeyDownForF6Revoker = new();
	private readonly SerialDisposable m_closeButtonClickedRevoker = new();
	private readonly SerialDisposable m_alternateCloseButtonClickedRevoker = new();
	private readonly SerialDisposable m_actionButtonClickedRevoker = new();
	private readonly SerialDisposable m_contentSizeChangedRevoker = new();
	private readonly SerialDisposable m_effectiveViewportChangedRevoker = new();
	private readonly SerialDisposable m_targetEffectiveViewportChangedRevoker = new();
	private readonly SerialDisposable m_targetLayoutUpdatedRevoker = new();
	private readonly SerialDisposable m_targetLoadedRevoker = new();
	private readonly SerialDisposable m_popupOpenedRevoker = new();
	private readonly SerialDisposable m_popupClosedRevoker = new();
	private readonly SerialDisposable m_lightDismissIndicatorPopupClosedRevoker = new();
	private readonly SerialDisposable m_tailOcclusionGridLoadedRevoker = new();
	private readonly SerialDisposable m_xamlRootChangedRevoker = new();
	private readonly SerialDisposable m_actualThemeChangedRevoker = new();

	private readonly SerialDisposable m_TargetUnloadedRevoker = new();


	float TopLeftCornerRadius() => (float)GetTeachingTipCornerRadius().TopLeft;
	float TopRightCornerRadius() => (float)GetTeachingTipCornerRadius().TopRight;

	private Border m_container;

	internal Popup m_popup;
	private Popup m_lightDismissIndicatorPopup;
#if false // Unused in WinUI
	private ContentControl m_popupContentControl;
#endif

	private UIElement m_rootElement;
	private Grid m_tailOcclusionGrid;
	private Grid m_contentRootGrid;
	private Grid m_nonHeroContentRootGrid;
	private Border m_heroContentBorder;
	private Button m_actionButton;
	private Button m_alternateCloseButton;
	private Button m_closeButton;
	private Polygon m_tailPolygon;
	private Grid m_tailEdgeBorder;
#if false // Unused in WinUI
	private UIElement m_titleTextBlock;
	private UIElement m_subtitleTextBlock;
#endif

	private ManagedWeakReference m_previouslyFocusedElement;

	private KeyFrameAnimation m_expandAnimation;
	private KeyFrameAnimation m_contractAnimation;
	private KeyFrameAnimation m_expandElevationAnimation;
	private KeyFrameAnimation m_contractElevationAnimation;
	private CompositionEasingFunction m_expandEasingFunction;
	private CompositionEasingFunction m_contractEasingFunction;

	private TeachingTipPlacementMode m_currentEffectiveTipPlacementMode = TeachingTipPlacementMode.Auto;
	private TeachingTipPlacementMode m_currentEffectiveTailPlacementMode = TeachingTipPlacementMode.Auto;
	private TeachingTipHeroContentPlacementMode m_currentHeroContentEffectivePlacementMode = TeachingTipHeroContentPlacementMode.Auto;

	private Rect m_currentBoundsInCoreWindowSpace = Rect.Empty;
	private Rect m_currentTargetBoundsInCoreWindowSpace = Rect.Empty;

	private Size m_currentXamlRootSize = Size.Empty;

	private bool m_ignoreNextIsOpenChanged;
	private bool m_isTemplateApplied;
	private bool m_createNewPopupOnOpen;

	private bool m_isExpandAnimationPlaying;
	private bool m_isContractAnimationPlaying;

	private bool m_hasF6BeenInvoked;

	private bool m_useTestWindowBounds;
	private Rect m_testWindowBoundsInCoreWindowSpace = Rect.Empty;
	private bool m_useTestScreenBounds;
	private Rect m_testScreenBoundsInCoreWindowSpace = Rect.Empty;

	// Uno specific: Shadows are not working correctly here, so we keep this field false for now.
	// They are enabled in WinUI. #15751
	private bool m_tipShouldHaveShadow;

	private bool m_tipFollowsTarget;
	private bool m_returnTopForOutOfWindowPlacement = true;

	private float m_contentElevation = 32.0f;
	private float m_tailElevation = 0.0f;
#if false // Unused in WinUI
	private bool m_tailShadowTargetsShadowTarget;
#endif

	private TimeSpan m_expandAnimationDuration = TimeSpan.FromMilliseconds(300);
	private TimeSpan m_contractAnimationDuration = TimeSpan.FromMilliseconds(200);

	private TeachingTipCloseReason m_lastCloseReason = TeachingTipCloseReason.Programmatic;

#if false // Unused in WinUI
	private static bool IsPlacementTop(TeachingTipPlacementMode placement) =>
		placement == TeachingTipPlacementMode.Top ||
		placement == TeachingTipPlacementMode.TopLeft ||
		placement == TeachingTipPlacementMode.TopRight;
#endif

	static bool IsPlacementBottom(TeachingTipPlacementMode placement) =>
		placement == TeachingTipPlacementMode.Bottom ||
		placement == TeachingTipPlacementMode.BottomLeft ||
		placement == TeachingTipPlacementMode.BottomRight;

	static bool IsPlacementLeft(TeachingTipPlacementMode placement) =>
		placement == TeachingTipPlacementMode.Left ||
		placement == TeachingTipPlacementMode.LeftTop ||
		placement == TeachingTipPlacementMode.LeftBottom;

	static bool IsPlacementRight(TeachingTipPlacementMode placement) =>
		placement == TeachingTipPlacementMode.Right ||
		placement == TeachingTipPlacementMode.RightTop ||
		placement == TeachingTipPlacementMode.RightBottom;

	// These values are shifted by one because this is the 1px highlight that sits adjacent to the tip border.
	private Thickness BottomPlacementTopRightHighlightMargin(double width, double height) =>
		new((width / 2) + (TailShortSideLength() - 1.0f), 0, (TopRightCornerRadius() - 1.0f), 0);

	private Thickness BottomRightPlacementTopRightHighlightMargin(double width, double height) =>
		new(MinimumTipEdgeToTailEdgeMargin() + TailLongSideLength() - 1.0f, 0, (TopRightCornerRadius() - 1.0f), 0);

	private Thickness BottomLeftPlacementTopRightHighlightMargin(double width, double height) =>
		new(width - (MinimumTipEdgeToTailEdgeMargin() + 1.0f), 0, (TopRightCornerRadius() - 1.0f), 0);

	static private Thickness OtherPlacementTopRightHighlightMargin(double width, double height) =>
		new(0, 0, 0, 0);

	private Thickness BottomPlacementTopLeftHighlightMargin(double width, double height) =>
		new((TopLeftCornerRadius() - 1.0f), 0, (width / 2) + (TailShortSideLength() - 1.0f), 0);

	private Thickness BottomRightPlacementTopLeftHighlightMargin(double width, double height) =>
		new((TopLeftCornerRadius() - 1.0f), 0, width - (MinimumTipEdgeToTailEdgeMargin() + 1.0f), 0);

	private Thickness BottomLeftPlacementTopLeftHighlightMargin(double width, double height) =>
		new((TopLeftCornerRadius() - 1.0f), 0, MinimumTipEdgeToTailEdgeMargin() + TailLongSideLength() - 1.0f, 0);

	private Thickness TopEdgePlacementTopLeftHighlightMargin(double width, double height) =>
		new((TopLeftCornerRadius() - 1.0f), 1, (TopRightCornerRadius() - 1.0f), 0);

	// Shifted by one since the tail edge's border is not accounted for automatically.
	private Thickness LeftEdgePlacementTopLeftHighlightMargin(double width, double height) =>
		new((TopLeftCornerRadius() - 1.0f), 1, (TopRightCornerRadius() - 2.0f), 0);

	private Thickness RightEdgePlacementTopLeftHighlightMargin(double width, double height) =>
		new((TopLeftCornerRadius() - 2.0f), 1, (TopRightCornerRadius() - 1.0f), 0);

	static private double UntargetedTipFarPlacementOffset(float farWindowCoordinateInCoreWindowSpace, double tipSize, double offset) =>
		farWindowCoordinateInCoreWindowSpace - (tipSize + s_untargetedTipWindowEdgeMargin + offset);

	static private double UntargetedTipCenterPlacementOffset(float nearWindowCoordinateInCoreWindowSpace, float farWindowCoordinateInCoreWindowSpace, double tipSize, double nearOffset, double farOffset) =>
		((nearWindowCoordinateInCoreWindowSpace + farWindowCoordinateInCoreWindowSpace) / 2) - (tipSize / 2) + nearOffset - farOffset;

	static private double UntargetedTipNearPlacementOffset(float nearWindowCoordinateInCoreWindowSpace, double offset) =>
		s_untargetedTipWindowEdgeMargin + nearWindowCoordinateInCoreWindowSpace + offset;

	private const string s_scaleTargetName = "Scale";
	private const string s_translationTargetName = "Translation";

	private const string s_containerName = "Container";
	private const string s_popupName = "Popup";
	private const string s_tailOcclusionGridName = "TailOcclusionGrid";
	private const string s_contentRootGridName = "ContentRootGrid";
	private const string s_nonHeroContentRootGridName = "NonHeroContentRootGrid";
	private const string s_shadowTargetName = "ShadowTarget";
	private const string s_heroContentBorderName = "HeroContentBorder";
	private const string s_titlesStackPanelName = "TitlesStackPanel";
	private const string s_titleTextBoxName = "TitleTextBlock";
	private const string s_subtitleTextBoxName = "SubtitleTextBlock";
	private const string s_alternateCloseButtonName = "AlternateCloseButton";
	private const string s_mainContentPresenterName = "MainContentPresenter";
	private const string s_actionButtonName = "ActionButton";
	private const string s_closeButtonName = "CloseButton";
	private const string s_tailPolygonName = "TailPolygon";
	private const string s_tailEdgeBorderName = "TailEdgeBorder";
	private const string s_topTailPolygonHighlightName = "TopTailPolygonHighlight";
	private const string s_topHighlightLeftName = "TopHighlightLeft";
	private const string s_topHighlightRightName = "TopHighlightRight";

	private const string s_accentButtonStyleName = "AccentButtonStyle";
	private const string s_teachingTipTopHighlightBrushName = "TeachingTipTopHighlightBrush";

	private static readonly Vector2 s_expandAnimationEasingCurveControlPoint1 = new Vector2(0.1f, 0.9f);
	private static readonly Vector2 s_expandAnimationEasingCurveControlPoint2 = new Vector2(0.2f, 1.0f);
	private static readonly Vector2 s_contractAnimationEasingCurveControlPoint1 = new Vector2(0.7f, 0.0f);
	private static readonly Vector2 s_contractAnimationEasingCurveControlPoint2 = new Vector2(1.0f, 0.5f);

	//It is possible this should be exposed as a property, but you can adjust what it does with margin.
	private const float s_untargetedTipWindowEdgeMargin = 24;
	private const float s_defaultTipHeightAndWidth = 320;

	//Ideally this would be computed from layout but it is difficult to do.
	private const float s_tailOcclusionAmount = 2;
}
