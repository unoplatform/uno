using System;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TeachingTip
	{
		private bool m_isIdle = true;
		private FrameworkElement m_target;

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

		private static bool IsPlacementTop(TeachingTipPlacementMode placement)
		{
			return placement == TeachingTipPlacementMode.Top ||
				placement == TeachingTipPlacementMode.TopLeft ||
				placement == TeachingTipPlacementMode.TopRight;
		}
		static bool IsPlacementBottom(TeachingTipPlacementMode placement)
		{
			return placement == TeachingTipPlacementMode.Bottom ||
				placement == TeachingTipPlacementMode.BottomLeft ||
				placement == TeachingTipPlacementMode.BottomRight;
		}
		static bool IsPlacementLeft(TeachingTipPlacementMode placement)
		{
			return placement == TeachingTipPlacementMode.Left ||
				placement == TeachingTipPlacementMode.LeftTop ||
				placement == TeachingTipPlacementMode.LeftBottom;
		}
		static bool IsPlacementRight(TeachingTipPlacementMode placement)
		{
			return placement == TeachingTipPlacementMode.Right ||
				placement == TeachingTipPlacementMode.RightTop ||
				placement == TeachingTipPlacementMode.RightBottom;
		}

		// These values are shifted by one because this is the 1px highlight that sits adjacent to the tip border.
		inline winrt::Thickness BottomPlacementTopRightHighlightMargin(double width, double height) { return { (width / 2) + (TailShortSideLength() - 1.0f), 0, 3, 0 }; }
		inline winrt::Thickness BottomRightPlacementTopRightHighlightMargin(double width, double height) { return { MinimumTipEdgeToTailEdgeMargin() + TailLongSideLength() - 1.0f, 0, 3, 0 }; }
		inline winrt::Thickness BottomLeftPlacementTopRightHighlightMargin(double width, double height) { return { width - (MinimumTipEdgeToTailEdgeMargin() + 1.0f), 0, 3, 0 }; }
		static inline winrt::Thickness constexpr OtherPlacementTopRightHighlightMargin(double width, double height) { return { 0, 0, 0, 0 }; }

		inline winrt::Thickness BottomPlacementTopLeftHighlightMargin(double width, double height) { return { 3, 0, (width / 2) + (TailShortSideLength() - 1.0f), 0 }; }
		inline winrt::Thickness BottomRightPlacementTopLeftHighlightMargin(double width, double height) { return { 3, 0, width - (MinimumTipEdgeToTailEdgeMargin() + 1.0f), 0 }; }
		inline winrt::Thickness BottomLeftPlacementTopLeftHighlightMargin(double width, double height) { return { 3, 0, MinimumTipEdgeToTailEdgeMargin() + TailLongSideLength() - 1.0f, 0 }; }
		static inline winrt::Thickness constexpr TopEdgePlacementTopLeftHighlightMargin(double width, double height) { return { 3, 1, 3, 0 }; }
		// Shifted by one since the tail edge's border is not accounted for automatically.
		static inline winrt::Thickness constexpr LeftEdgePlacementTopLeftHighlightMargin(double width, double height) { return { 3, 1, 2, 0 }; }
		static inline winrt::Thickness constexpr RightEdgePlacementTopLeftHighlightMargin(double width, double height) { return { 2, 1, 3, 0 }; }

		static inline double constexpr UntargetedTipFarPlacementOffset(float farWindowCoordinateInCoreWindowSpace, double tipSize, double offset) { return farWindowCoordinateInCoreWindowSpace - (tipSize + s_untargetedTipWindowEdgeMargin + offset); }
		static inline double constexpr UntargetedTipCenterPlacementOffset(float nearWindowCoordinateInCoreWindowSpace, float farWindowCoordinateInCoreWindowSpace, double tipSize, double nearOffset, double farOffset) { return ((nearWindowCoordinateInCoreWindowSpace + farWindowCoordinateInCoreWindowSpace) / 2) - (tipSize / 2) + nearOffset - farOffset; }
		static inline double constexpr UntargetedTipNearPlacementOffset(float nearWindowCoordinateInCoreWindowSpace, double offset) { return s_untargetedTipWindowEdgeMargin + nearWindowCoordinateInCoreWindowSpace + offset; }

		static constexpr wstring_view s_scaleTargetName{ L"Scale"sv
	};
	static constexpr wstring_view s_translationTargetName{ L"Translation"sv
};

static constexpr wstring_view s_containerName{ L"Container"sv };
static constexpr wstring_view s_popupName{ L"Popup"sv };
static constexpr wstring_view s_tailOcclusionGridName{ L"TailOcclusionGrid"sv };
static constexpr wstring_view s_contentRootGridName{ L"ContentRootGrid"sv };
static constexpr wstring_view s_nonHeroContentRootGridName{ L"NonHeroContentRootGrid"sv };
static constexpr wstring_view s_shadowTargetName{ L"ShadowTarget"sv };
static constexpr wstring_view s_heroContentBorderName{ L"HeroContentBorder"sv };
static constexpr wstring_view s_titlesStackPanelName{ L"TitlesStackPanel"sv };
static constexpr wstring_view s_titleTextBoxName{ L"TitleTextBlock"sv };
static constexpr wstring_view s_subtitleTextBoxName{ L"SubtitleTextBlock"sv };
static constexpr wstring_view s_alternateCloseButtonName{ L"AlternateCloseButton"sv };
static constexpr wstring_view s_mainContentPresenterName{ L"MainContentPresenter"sv };
static constexpr wstring_view s_actionButtonName{ L"ActionButton"sv };
static constexpr wstring_view s_closeButtonName{ L"CloseButton"sv };
static constexpr wstring_view s_tailPolygonName{ L"TailPolygon"sv };
static constexpr wstring_view s_tailEdgeBorderName{ L"TailEdgeBorder"sv };
static constexpr wstring_view s_topTailPolygonHighlightName{ L"TopTailPolygonHighlight"sv };
static constexpr wstring_view s_topHighlightLeftName{ L"TopHighlightLeft"sv };
static constexpr wstring_view s_topHighlightRightName{ L"TopHighlightRight"sv };

static constexpr wstring_view s_accentButtonStyleName{ L"AccentButtonStyle" };
static constexpr wstring_view s_teachingTipTopHighlightBrushName{ L"TeachingTipTopHighlightBrush" };

static constexpr winrt::float2 s_expandAnimationEasingCurveControlPoint1{ 0.1f, 0.9f };
static constexpr winrt::float2 s_expandAnimationEasingCurveControlPoint2{ 0.2f, 1.0f };
static constexpr winrt::float2 s_contractAnimationEasingCurveControlPoint1{ 0.7f, 0.0f };
static constexpr winrt::float2 s_contractAnimationEasingCurveControlPoint2{ 1.0f, 0.5f };

//It is possible this should be exposed as a property, but you can adjust what it does with margin.
static constexpr float s_untargetedTipWindowEdgeMargin = 24;
static constexpr float s_defaultTipHeightAndWidth = 320;

//Ideally this would be computed from layout but it is difficult to do.
static constexpr float s_tailOcclusionAmount = 2;
	}
}
