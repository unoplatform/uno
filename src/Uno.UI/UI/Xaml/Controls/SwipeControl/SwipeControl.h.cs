// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeControl.h

using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SwipeControl
	{
		private enum CreatedContent { Left, Top, Bottom, Right, None };

		//class SwipeControl :
		//    public ReferenceTracker<SwipeControl, winrt.implementation.SwipeControlT, winrt.cloaked<winrt.IInteractionTrackerOwner>>,
		//    public SwipeControlProperties
		//{
		//public:
		//    SwipeControl();
		//    virtual ~SwipeControl();

		//#pragma region ISwipeControl


		//    void Close();

		//#pragma endregion

		//#pragma region FrameworkElementOverrides
		//    void OnApplyTemplate();

		//    void OnPropertyChanged( winrt.DependencyPropertyChangedEventArgs& args);
		//    winrt.Size MeasureOverride(winrt.Size & availableSize);
		//#pragma endregion

		//#pragma region IInteractionTrackerOwner
		//    void CustomAnimationStateEntered(
		//        winrt.InteractionTracker & sender,
		//        winrt.InteractionTrackerCustomAnimationStateEnteredArgs & args);

		//    void RequestIgnored(
		//        winrt.InteractionTracker & sender,
		//        winrt.InteractionTrackerRequestIgnoredArgs & args);

		//    void IdleStateEntered(
		//        winrt.InteractionTracker & sender,
		//        winrt.InteractionTrackerIdleStateEnteredArgs & args);

		//    void InteractingStateEntered(
		//        winrt.InteractionTracker & sender,
		//        winrt.InteractionTrackerInteractingStateEnteredArgs & args);

		//    void InertiaStateEntered(
		//        winrt.InteractionTracker & sender,
		//        winrt.InteractionTrackerInertiaStateEnteredArgs & args);

		//    void ValuesChanged(
		//        winrt.InteractionTracker & sender,
		//        winrt.InteractionTrackerValuesChangedArgs & args);
		//#pragma endregion

		//    winrt.SwipeItems GetCurrentItems() { return m_currentItems.get(); }

		//#pragma region TestHookHelpers
		//    static winrt.SwipeControl GetLastInteractedWithSwipeControl();
		//    bool GetIsOpen();
		//    bool GetIsIdle();
		//#pragma endregion

		//private:
		//    void OnLeftItemsCollectionChanged( winrt.DependencyPropertyChangedEventArgs& /args/);
		//    void OnRightItemsCollectionChanged( winrt.DependencyPropertyChangedEventArgs& /args/);
		//    void OnBottomItemsCollectionChanged( winrt.DependencyPropertyChangedEventArgs& /args/);
		//    void OnTopItemsCollectionChanged( winrt.DependencyPropertyChangedEventArgs& /args/);
		//    void OnLoaded( winrt.DependencyObject& /sender/,  winrt.RoutedEventArgs& /args/);

		//    void AttachEventHandlers();
		//    void DetachEventHandlers();
		//    void OnSizeChanged( winrt.DependencyObject& sender,  winrt.SizeChangedEventArgs& args);
		//    void OnSwipeContentStackPanelSizeChanged( winrt.DependencyObject& sender,  winrt.SizeChangedEventArgs& args);
		//    void OnPointerPressedEvent( winrt.DependencyObject& sender,  winrt.PointerRoutedEventArgs& args);
		//    void InputEaterGridTapped( winrt.DependencyObject& /sender/,  winrt.TappedRoutedEventArgs& args);

		//    void AttachDismissingHandlers();
		//    void DetachDismissingHandlers();
		//    void DismissSwipeOnAcceleratorKeyActivator( winrt.Windows.UI.Core.CoreDispatcher & sender,  winrt.AcceleratorKeyEventArgs & args);

		//    // Used on platforms where we have XamlRoot.
		//    void CurrentXamlRootChanged( winrt.XamlRoot & sender,  winrt.XamlRootChangedEventArgs & args);


		//    // Used on platforms where we don't have XamlRoot.
		//    void DismissSwipeOnCoreWindowKeyDown( winrt.CoreWindow & sender,  winrt.KeyEventArgs & args);
		//    void CurrentWindowSizeChanged( winrt.DependencyObject & sender,  winrt.WindowSizeChangedEventArgs& args);
		//    void CurrentWindowVisibilityChanged( winrt.CoreWindow & sender,  winrt.VisibilityChangedEventArgs args);
		//    void DismissSwipeOnAnExternalCoreWindowTap( winrt.CoreWindow& sender,  winrt.PointerEventArgs& args);

		//    void DismissSwipeOnAnExternalTap(winrt.Point & tapPoint);

		//    void GetTemplateParts();

		//    void InitializeInteractionTracker();
		//    void ConfigurePositionInertiaRestingValues();

		//    winrt.Visual FindVisualInteractionSourceVisual();
		//    void EnsureClip();

		//    void CloseWithoutAnimation();
		//    void CloseIfNotRemainOpenExecuteItem();

		//    void CreateLeftContent();
		//    void CreateRightContent();
		//    void CreateTopContent();
		//    void CreateBottomContent();
		//    void CreateContent( winrt.SwipeItems& items);

		//    void AlignStackPanel();
		//    void PopulateContentItems();
		//    void SetupExecuteExpressionAnimation();
		//    void SetupClipAnimation();
		//    void UpdateColors();

		//    winrt.AppBarButton GetSwipeItemButton( winrt.SwipeItem& swipeItem);
		//    void UpdateColorsIfExecuteItem();
		//    void UpdateColorsIfRevealItems();
		//    void UpdateExecuteForegroundColor( winrt.SwipeItem& swipeItem);
		//    void UpdateExecuteBackgroundColor( winrt.SwipeItem& swipeItem);

		//    void OnLeftItemsChanged( winrt.IObservableVector<winrt.SwipeItem>& sender,  winrt.IVectorChangedEventArgs args);
		//    void OnRightItemsChanged( winrt.IObservableVector<winrt.SwipeItem>& sender,  winrt.IVectorChangedEventArgs args);
		//    void OnTopItemsChanged( winrt.IObservableVector<winrt.SwipeItem>& sender,  winrt.IVectorChangedEventArgs args);
		//    void OnBottomItemsChanged( winrt.IObservableVector<winrt.SwipeItem>& sender,  winrt.IVectorChangedEventArgs args);

		//    void TryGetSwipeVisuals();
		//    void UpdateIsOpen(bool isOpen);
		//    void UpdateThresholdReached(float value);

		//    void ThrowIfHasVerticalAndHorizontalContent(bool IsHorizontal = false);

		//    std.string GetAnimationTarget(winrt.UIElement child);

		//    winrt.SwipeControl GetThis();

		private Grid m_rootGrid;
		private Grid m_content;
		private Grid m_inputEater;
		private Grid m_swipeContentRoot;
		private StackPanel m_swipeContentStackPanel;

		//private InteractionTracker m_interactionTracker;
		//private VisualInteractionSource m_visualInteractionSource;
		//private Compositor m_compositor;

		//private Visual m_mainContentVisual;
		//private Visual m_swipeContentRootVisual;
		//private Visual m_swipeContentVisual;
		//private InsetClip m_insetClip;

		//private ExpressionAnimation m_swipeAnimation;
		//private ExpressionAnimation m_executeExpressionAnimation;
		//private ExpressionAnimation m_clipExpressionAnimation;
		//private ExpressionAnimation m_maxPositionExpressionAnimation;
		//private ExpressionAnimation m_minPositionExpressionAnimation;

		private Style m_swipeItemStyle;

		// Cache the current content object to minimize work if there are multiple swipes in the same direction.
		private SwipeItems m_currentItems;

		//private IDisposable m_loadedToken;

		//private IDisposable m_leftItemsChangedToken;
		//private IDisposable m_rightItemsChangedToken;
		//private IDisposable m_topItemsChangedToken;
		//private IDisposable m_bottomItemsChangedToken;
		//private IDisposable m_onSizeChangedToken;
		//private IDisposable m_onSwipeContentStackPanelSizeChangedToken;
		//private IDisposable m_inputEaterTappedToken;
		private PointerEventHandler m_onPointerPressedEventHandler;

		// Used on platforms where we have XamlRoot.
		private IDisposable m_xamlRootPointerPressedEventRevoker;
		private IDisposable m_xamlRootKeyDownEventRevoker;
		private IDisposable m_xamlRootChangedRevoker;


		// Used on platforms where we don't have XamlRoot.
		//private IDisposable m_coreWindowPointerPressedRevoker;
		//private IDisposable m_coreWindowKeyDownRevoker;
		//private IDisposable m_windowMinimizeRevoker;
		//private IDisposable m_windowSizeChangedRevoker;

		private IDisposable m_acceleratorKeyActivatedRevoker;

		private bool m_hasInitialLoadedEventFired;

		private bool m_lastActionWasClosing;
		private bool m_lastActionWasOpening;
		private bool m_isInteracting;
		private bool m_isIdle = true;
		private bool m_isOpen;

		private bool m_thresholdReached;

		//Near content = left or top
		//Far content = right or bottom
		private bool m_blockNearContent;
		private bool m_blockFarContent;
		private bool m_isHorizontal = true;
		private CreatedContent m_createdContent = CreatedContent.None;

		//static bool IsTranslationFacadeAvailableForSwipeControl(winrt.UIElement& element);
		//static string DirectionToInset(CreatedContent& createdContent);

		//private const string s_isNearOpenPropertyName = "isNearOpen";
		//private static string isNearOpenPropertyName() { return s_isNearOpenPropertyName; }

		//private const string s_isFarOpenPropertyName = "isFarOpen";
		//private static string isFarOpenPropertyName() { return s_isFarOpenPropertyName; }

		//private const string s_isNearContentPropertyName = "isNearContent";
		//private static string isNearContentPropertyName() { return s_isNearContentPropertyName; }

		//private const string s_blockNearContentPropertyName = "blockNearContent";
		//private static string blockNearContentPropertyName() { return s_blockNearContentPropertyName; }

		//private const string s_blockFarContentPropertyName = "blockFarContent";
		//private static string blockFarContentPropertyName() { return s_blockFarContentPropertyName; }

		//private const string s_hasLeftContentPropertyName = "hasLeftContent";
		//private static string hasLeftContentPropertyName() { return s_hasLeftContentPropertyName; }

		//private const string s_hasRightContentPropertyName = "hasRightContent";
		//private static string hasRightContentPropertyName() { return s_hasRightContentPropertyName; }

		//private const string s_hasTopContentPropertyName = "hasTopContent";
		//private static string hasTopContentPropertyName() { return s_hasTopContentPropertyName; }

		//private const string s_hasBottomContentPropertyName = "hasBottomContent";
		//private static string hasBottomContentPropertyName() { return s_hasBottomContentPropertyName; }

		//private const string s_isHorizontalPropertyName = "isHorizontal";
		//private static string isHorizontalPropertyName() { return s_isHorizontalPropertyName; }

		//private const string s_trackerPropertyName = "tracker";
		//private static string trackerPropertyName() { return s_trackerPropertyName; }

		//private const string s_foregroundVisualPropertyName = "foregroundVisual";
		//private static string foregroundVisualPropertyName() { return s_foregroundVisualPropertyName; }

		//private const string s_swipeContentVisualPropertyName = "swipeContentVisual";
		//private static string swipeContentVisualPropertyName() { return s_swipeContentVisualPropertyName; }

		//private const string s_swipeContentSizeParameterName = "swipeContentVisual";
		//private static string swipeContentSizeParameterName() { return s_swipeContentSizeParameterName; }

		//private const string s_swipeRootVisualPropertyName = "swipeRootVisual";
		//private static string swipeRootVisualPropertyName() { return s_swipeRootVisualPropertyName; }

		//private const string s_maxThresholdPropertyName = "maxThreshold";
		//private static string maxThresholdPropertyName() { return s_maxThresholdPropertyName; }

		//private const string s_minPositionPropertyName = "minPosition";
		//private const string s_maxPositionPropertyName = "maxPosition";

		//private const string s_leftInsetTargetName = "LeftInset";
		//private const string s_rightInsetTargetName = "RightInset";
		//private const string s_topInsetTargetName = "TopInset";
		//private const string s_bottomInsetTargetName = "BottomInset";

		//private const string s_translationPropertyName = "Translation";
		//private const string s_offsetPropertyName = "Offset";

		private const string s_rootGridName = "RootGrid";
		private const string s_inputEaterName = "InputEater";
		private const string s_ContentRootName = "ContentRoot";
		private const string s_swipeContentRootName = "SwipeContentRoot";
		private const string s_swipeContentStackPanelName = "SwipeContentStackPanel";
		private const string s_swipeItemStyleName = "SwipeItemStyle";


		private const string s_swipeItemBackgroundResourceName = "SwipeItemBackground";
		private const string s_swipeItemForegroundResourceName = "SwipeItemForeground";
		private const string s_executeSwipeItemPreThresholdBackgroundResourceName = "SwipeItemPreThresholdExecuteBackground";
		private const string s_executeSwipeItemPostThresholdBackgroundResourceName = "SwipeItemPostThresholdExecuteBackground";
		private const string s_executeSwipeItemPreThresholdForegroundResourceName = "SwipeItemPreThresholdExecuteForeground";
		private const string s_executeSwipeItemPostThresholdForegroundResourceName = "SwipeItemPostThresholdExecuteForeground";
	}
}
