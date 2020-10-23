// MUX Reference TeachingTip.h, commit dfbe38b

#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TeachingTip
	{
		private XamlRoot m_xamlRoot;

		internal bool m_isIdle = true;
		private FrameworkElement m_target;

		private Border m_container;
		internal Popup m_popup;
		private Popup m_lightDismissIndicatorPopup;
		private ContentControl m_popupContentControl;
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
		private UIElement m_titleTextBox;
		private UIElement m_subtitleTextBox;

		private WeakReference<DependencyObject> m_previouslyFocusedElement;

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

		private bool m_isTemplateApplied = false;
		private bool m_createNewPopupOnOpen = false;

		private bool m_isExpandAnimationPlaying = false;
		private bool m_isContractAnimationPlaying = false;

		private bool m_hasF6BeenInvoked = false;

		private bool m_useTestWindowBounds = false;
		private Rect m_testWindowBoundsInCoreWindowSpace = Rect.Empty;
		private bool m_useTestScreenBounds = false;
		private Rect m_testScreenBoundsInCoreWindowSpace = Rect.Empty;

		private bool m_tipShouldHaveShadow = true;

		private bool m_tipFollowsTarget = false;
		private bool m_returnTopForOutOfWindowPlacement = true;

		private float m_contentElevation = 32.0f;
		private float m_tailElevation = 0.0f;
		private bool m_tailShadowTargetsShadowTarget = false;

		private TimeSpan m_expandAnimationDuration = TimeSpan.FromMilliseconds(300);
		private TimeSpan m_contractAnimationDuration = TimeSpan.FromMilliseconds(200);

		private TeachingTipCloseReason m_lastCloseReason = TeachingTipCloseReason.Programmatic;

		private static bool IsPlacementTop(TeachingTipPlacementMode placement) =>
			placement == TeachingTipPlacementMode.Top ||
			placement == TeachingTipPlacementMode.TopLeft ||
			placement == TeachingTipPlacementMode.TopRight;
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
		private Thickness BottomPlacementTopRightHighlightMargin(double width, double height) => new Thickness((width / 2) + (TailShortSideLength() - 1.0f), 0, 3, 0);
		private Thickness BottomRightPlacementTopRightHighlightMargin(double width, double height) => new Thickness(MinimumTipEdgeToTailEdgeMargin() + TailLongSideLength() - 1.0f, 0, 3, 0);
		private Thickness BottomLeftPlacementTopRightHighlightMargin(double width, double height) => new Thickness(width - (MinimumTipEdgeToTailEdgeMargin() + 1.0f), 0, 3, 0);
		static private Thickness OtherPlacementTopRightHighlightMargin(double width, double height) => new Thickness(0, 0, 0, 0);

		private Thickness BottomPlacementTopLeftHighlightMargin(double width, double height) => new Thickness(3, 0, (width / 2) + (TailShortSideLength() - 1.0f), 0);
		private Thickness BottomRightPlacementTopLeftHighlightMargin(double width, double height) => new Thickness(3, 0, width - (MinimumTipEdgeToTailEdgeMargin() + 1.0f), 0);
		private Thickness BottomLeftPlacementTopLeftHighlightMargin(double width, double height) => new Thickness(3, 0, MinimumTipEdgeToTailEdgeMargin() + TailLongSideLength() - 1.0f, 0);
		static private Thickness TopEdgePlacementTopLeftHighlightMargin(double width, double height) => new Thickness(3, 1, 3, 0);
		// Shifted by one since the tail edge's border is not accounted for automatically.
		static private Thickness LeftEdgePlacementTopLeftHighlightMargin(double width, double height) => new Thickness(3, 1, 2, 0);
		static private Thickness RightEdgePlacementTopLeftHighlightMargin(double width, double height) => new Thickness(2, 1, 3, 0);

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
}
