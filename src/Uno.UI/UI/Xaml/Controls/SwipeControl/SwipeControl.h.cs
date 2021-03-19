// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

namespace Windows.UI.Xaml.Controls
{
	public partial class SwipeControl
	{

		enum class CreatedContent { Left, Top, Bottom, Right, None };

class SwipeControl :
    public ReferenceTracker<SwipeControl, winrt.implementation.SwipeControlT, winrt.cloaked<winrt.IInteractionTrackerOwner>>,
    public SwipeControlProperties
{
public:
    SwipeControl();
    virtual ~SwipeControl();

#pragma region ISwipeControl


    void Close();

#pragma endregion

#pragma region FrameworkElementOverrides
    void OnApplyTemplate();

    void OnPropertyChanged( winrt.DependencyPropertyChangedEventArgs& args);
    winrt.Size MeasureOverride(winrt.Size & availableSize);
#pragma endregion

#pragma region IInteractionTrackerOwner
    void CustomAnimationStateEntered(
        winrt.InteractionTracker & sender,
        winrt.InteractionTrackerCustomAnimationStateEnteredArgs & args);

    void RequestIgnored(
        winrt.InteractionTracker & sender,
        winrt.InteractionTrackerRequestIgnoredArgs & args);

    void IdleStateEntered(
        winrt.InteractionTracker & sender,
        winrt.InteractionTrackerIdleStateEnteredArgs & args);

    void InteractingStateEntered(
        winrt.InteractionTracker & sender,
        winrt.InteractionTrackerInteractingStateEnteredArgs & args);

    void InertiaStateEntered(
        winrt.InteractionTracker & sender,
        winrt.InteractionTrackerInertiaStateEnteredArgs & args);

    void ValuesChanged(
        winrt.InteractionTracker & sender,
        winrt.InteractionTrackerValuesChangedArgs & args);
#pragma endregion

    winrt.SwipeItems GetCurrentItems() { return m_currentItems.get(); }

#pragma region TestHookHelpers
    static winrt.SwipeControl GetLastInteractedWithSwipeControl();
    bool GetIsOpen();
    bool GetIsIdle();
#pragma endregion

private:
    void OnLeftItemsCollectionChanged( winrt.DependencyPropertyChangedEventArgs& /args/);
    void OnRightItemsCollectionChanged( winrt.DependencyPropertyChangedEventArgs& /args/);
    void OnBottomItemsCollectionChanged( winrt.DependencyPropertyChangedEventArgs& /args/);
    void OnTopItemsCollectionChanged( winrt.DependencyPropertyChangedEventArgs& /args/);
    void OnLoaded( winrt.DependencyObject& /sender/,  winrt.RoutedEventArgs& /args/);

    void AttachEventHandlers();
    void DetachEventHandlers();
    void OnSizeChanged( winrt.DependencyObject& sender,  winrt.SizeChangedEventArgs& args);
    void OnSwipeContentStackPanelSizeChanged( winrt.DependencyObject& sender,  winrt.SizeChangedEventArgs& args);
    void OnPointerPressedEvent( winrt.DependencyObject& sender,  winrt.PointerRoutedEventArgs& args);
    void InputEaterGridTapped( winrt.DependencyObject& /sender/,  winrt.TappedRoutedEventArgs& args);

    void AttachDismissingHandlers();
    void DetachDismissingHandlers();
    void DismissSwipeOnAcceleratorKeyActivator( winrt.Windows.UI.Core.CoreDispatcher & sender,  winrt.AcceleratorKeyEventArgs & args);

    // Used on platforms where we have XamlRoot.
    void CurrentXamlRootChanged( winrt.XamlRoot & sender,  winrt.XamlRootChangedEventArgs & args);
    

    // Used on platforms where we don't have XamlRoot.
    void DismissSwipeOnCoreWindowKeyDown( winrt.CoreWindow & sender,  winrt.KeyEventArgs & args);
    void CurrentWindowSizeChanged( winrt.DependencyObject & sender,  winrt.WindowSizeChangedEventArgs& args);
    void CurrentWindowVisibilityChanged( winrt.CoreWindow & sender,  winrt.VisibilityChangedEventArgs args);
    void DismissSwipeOnAnExternalCoreWindowTap( winrt.CoreWindow& sender,  winrt.PointerEventArgs& args);

    void DismissSwipeOnAnExternalTap(winrt.Point & tapPoint);

    void GetTemplateParts();

    void InitializeInteractionTracker();
    void ConfigurePositionInertiaRestingValues();

    winrt.Visual FindVisualInteractionSourceVisual();
    void EnsureClip();

    void CloseWithoutAnimation();
    void CloseIfNotRemainOpenExecuteItem();

    void CreateLeftContent();
    void CreateRightContent();
    void CreateTopContent();
    void CreateBottomContent();
    void CreateContent( winrt.SwipeItems& items);

    void AlignStackPanel();
    void PopulateContentItems();
    void SetupExecuteExpressionAnimation();
    void SetupClipAnimation();
    void UpdateColors();

    winrt.AppBarButton GetSwipeItemButton( winrt.SwipeItem& swipeItem);
    void UpdateColorsIfExecuteItem();
    void UpdateColorsIfRevealItems();
    void UpdateExecuteForegroundColor( winrt.SwipeItem& swipeItem);
    void UpdateExecuteBackgroundColor( winrt.SwipeItem& swipeItem);

    void OnLeftItemsChanged( winrt.IObservableVector<winrt.SwipeItem>& sender,  winrt.IVectorChangedEventArgs args);
    void OnRightItemsChanged( winrt.IObservableVector<winrt.SwipeItem>& sender,  winrt.IVectorChangedEventArgs args);
    void OnTopItemsChanged( winrt.IObservableVector<winrt.SwipeItem>& sender,  winrt.IVectorChangedEventArgs args);
    void OnBottomItemsChanged( winrt.IObservableVector<winrt.SwipeItem>& sender,  winrt.IVectorChangedEventArgs args);

    void TryGetSwipeVisuals();
    void UpdateIsOpen(bool isOpen);
    void UpdateThresholdReached(float value);

    void ThrowIfHasVerticalAndHorizontalContent(bool IsHorizontal = false);

    std.string GetAnimationTarget(winrt.UIElement child);

    winrt.SwipeControl GetThis();

    tracker_ref<winrt.Grid> m_rootGrid{ this };
    tracker_ref<winrt.Grid> m_content{ this };
    tracker_ref<winrt.Grid> m_inputEater{ this };
    tracker_ref<winrt.Grid> m_swipeContentRoot{ this };
    tracker_ref<winrt.StackPanel> m_swipeContentStackPanel{ this };

    tracker_ref<winrt.InteractionTracker> m_interactionTracker{ this };
    tracker_ref<winrt.VisualInteractionSource> m_visualInteractionSource{ this };
    tracker_ref<winrt.Compositor> m_compositor{ this };

    tracker_ref<winrt.Visual> m_mainContentVisual{ this };
    tracker_ref<winrt.Visual> m_swipeContentRootVisual{ this };
    tracker_ref<winrt.Visual> m_swipeContentVisual{ this };
    tracker_ref<winrt.InsetClip> m_insetClip{ this };

    tracker_ref<winrt.ExpressionAnimation> m_swipeAnimation{ this };
    tracker_ref<winrt.ExpressionAnimation> m_executeExpressionAnimation{ this };
    tracker_ref<winrt.ExpressionAnimation> m_clipExpressionAnimation{ this };
    tracker_ref<winrt.ExpressionAnimation> m_maxPositionExpressionAnimation{ this };
    tracker_ref<winrt.ExpressionAnimation> m_minPositionExpressionAnimation{ this };

    tracker_ref<winrt.Style> m_swipeItemStyle{ this };

    // Cache the current content object to minimize work if there are multiple swipes in the same direction.
    tracker_ref<winrt.SwipeItems> m_currentItems{ this };

    winrt.event_token m_loadedToken{};
    winrt.event_token m_leftItemsChangedToken{};
    winrt.event_token m_rightItemsChangedToken{};
    winrt.event_token m_topItemsChangedToken{};
    winrt.event_token m_bottomItemsChangedToken{};
    winrt.event_token m_onSizeChangedToken{};
    winrt.event_token m_onSwipeContentStackPanelSizeChangedToken{};
    winrt.event_token m_inputEaterTappedToken{};
    tracker_ref<winrt.DependencyObject> m_onPointerPressedEventHandler{ this };

    // Used on platforms where we have XamlRoot.
    RoutedEventHandler_revoker m_xamlRootPointerPressedEventRevoker{};
    RoutedEventHandler_revoker m_xamlRootKeyDownEventRevoker{};
    winrt.IXamlRoot.Changed_revoker m_xamlRootChangedRevoker{};


    // Used on platforms where we don't have XamlRoot.
    winrt.ICoreWindow.PointerPressed_revoker m_coreWindowPointerPressedRevoker;
    winrt.ICoreWindow.KeyDown_revoker m_coreWindowKeyDownRevoker;
    winrt.ICoreWindow.VisibilityChanged_revoker m_windowMinimizeRevoker;
    winrt.IWindow.SizeChanged_revoker m_windowSizeChangedRevoker;

    winrt.CoreAcceleratorKeys.AcceleratorKeyActivated_revoker m_acceleratorKeyActivatedRevoker;

    bool m_hasInitialLoadedEventFired{ false };

    bool m_lastActionWasClosing{ false };
    bool m_lastActionWasOpening{ false };
    bool m_isInteracting{ false };
    bool m_isIdle{ true };
    bool m_isOpen{ false };
    bool m_thresholdReached{ false };
    //Near content = left or top
    //Far content = right or bottom
    bool m_blockNearContent{ false };
    bool m_blockFarContent{ false };
    bool m_isHorizontal{ true };
    CreatedContent m_createdContent{ CreatedContent.None };

    static bool IsTranslationFacadeAvailableForSwipeControl( winrt.UIElement& element);
    static wstring_view DirectionToInset( CreatedContent& createdContent);

    static  wstring_view s_isNearOpenPropertyName{ "isNearOpen"sv };
    static inline  std.string isNearOpenPropertyName() { return s_isNearOpenPropertyName.data(); }
    static  wstring_view s_isFarOpenPropertyName{ "isFarOpen"sv };
    static inline  std.string isFarOpenPropertyName() { return s_isFarOpenPropertyName.data(); }
    static  wstring_view s_isNearContentPropertyName{ "isNearContent"sv };
    static inline  std.string isNearContentPropertyName() { return s_isNearContentPropertyName.data(); }
    static  wstring_view s_blockNearContentPropertyName{ "blockNearContent"sv };
    static inline  std.string blockNearContentPropertyName() { return s_blockNearContentPropertyName.data(); }
    static  wstring_view s_blockFarContentPropertyName{ "blockFarContent"sv };
    static inline  std.string blockFarContentPropertyName() { return s_blockFarContentPropertyName.data(); }

    static  wstring_view s_hasLeftContentPropertyName{ "hasLeftContent"sv };
    static inline  std.string hasLeftContentPropertyName() { return s_hasLeftContentPropertyName.data(); }
    static  wstring_view s_hasRightContentPropertyName{ "hasRightContent"sv };
    static inline  std.string hasRightContentPropertyName() { return s_hasRightContentPropertyName.data(); }
    static  wstring_view s_hasTopContentPropertyName{ "hasTopContent"sv };
    static inline  std.string hasTopContentPropertyName() { return s_hasTopContentPropertyName.data(); }
    static  wstring_view s_hasBottomContentPropertyName{ "hasBottomContent"sv };
    static inline  std.string hasBottomContentPropertyName() { return s_hasBottomContentPropertyName.data(); }
    static  wstring_view s_isHorizontalPropertyName{ "isHorizontal"sv };
    static inline  std.string isHorizontalPropertyName() { return s_isHorizontalPropertyName.data(); }

    static  wstring_view s_trackerPropertyName{ "tracker"sv };
    static inline  std.string trackerPropertyName() { return s_trackerPropertyName.data(); }
    static  wstring_view s_foregroundVisualPropertyName{ "foregroundVisual"sv };
    static inline  std.string foregroundVisualPropertyName() { return s_foregroundVisualPropertyName.data(); }
    static  wstring_view s_swipeContentVisualPropertyName{ "swipeContentVisual"sv };
    static inline  std.string swipeContentVisualPropertyName() { return s_swipeContentVisualPropertyName.data(); }
    static  wstring_view s_swipeContentSizeParameterName{ "swipeContentVisual"sv };
    static inline  std.string swipeContentSizeParameterName() { return s_swipeContentSizeParameterName.data(); }
    static  wstring_view s_swipeRootVisualPropertyName{ "swipeRootVisual"sv };
    static inline  std.string swipeRootVisualPropertyName() { return s_swipeRootVisualPropertyName.data(); }
    static  wstring_view s_maxThresholdPropertyName{ "maxThreshold"sv };
    static inline  std.string maxThresholdPropertyName() { return s_maxThresholdPropertyName.data(); }

    static  wstring_view s_minPositionPropertyName{ "minPosition"sv };
    static  wstring_view s_maxPositionPropertyName{ "maxPosition"sv };

    static  wstring_view s_leftInsetTargetName{ "LeftInset"sv };
    static  wstring_view s_rightInsetTargetName{ "RightInset"sv };
    static  wstring_view s_topInsetTargetName{ "TopInset"sv };
    static  wstring_view s_bottomInsetTargetName{ "BottomInset"sv };

    static  wstring_view s_translationPropertyName{ "Translation"sv };
    static  wstring_view s_offsetPropertyName{ "Offset"sv };

    static  wstring_view s_rootGridName{ "RootGrid"sv };
    static  wstring_view s_inputEaterName{ "InputEater"sv };
    static  wstring_view s_ContentRootName{ "ContentRoot"sv };
    static  wstring_view s_swipeContentRootName{ "SwipeContentRoot"sv };
    static  wstring_view s_swipeContentStackPanelName{ "SwipeContentStackPanel"sv };
    static  wstring_view s_swipeItemStyleName{ "SwipeItemStyle"sv };


    static  wstring_view s_swipeItemBackgroundResourceName{ "SwipeItemBackground"sv };
    static  wstring_view s_swipeItemForegroundResourceName{ "SwipeItemForeground"sv };
    static  wstring_view s_executeSwipeItemPreThresholdBackgroundResourceName{ "SwipeItemPreThresholdExecuteBackground"sv };
    static  wstring_view s_executeSwipeItemPostThresholdBackgroundResourceName{ "SwipeItemPostThresholdExecuteBackground"sv };
    static  wstring_view s_executeSwipeItemPreThresholdForegroundResourceName{ "SwipeItemPreThresholdExecuteForeground"sv };
    static  wstring_view s_executeSwipeItemPostThresholdForegroundResourceName{ "SwipeItemPostThresholdExecuteForeground"sv };
};
}}
