// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls.Primitives
{

	public partial class LoopingSelector : Control
	{

		// public
		//public partial void OnPropertyChanged(
		//	 xaml.IDependencyPropertyChangedEventArgs pArgs);

		// UIA functions
		//public partial void AutomationGetIsScrollable(out bool pIsScrollable);
		//public partial void AutomationGetScrollPercent(out double pScrollPercent);
		//public partial void AutomationGetScrollViewSize(out double pScrollPercent);
		//public partial void AutomationSetScrollPercent( double scrollPercent);
		//public partial void AutomationTryGetSelectionUIAPeer(out xaml.Automation.Peers.IAutomationPeer ppPeer);
		//public partial void AutomationScroll( xaml.Automation.ScrollAmount scrollAmount);
		//public partial void AutomationFillCollectionWithRealizedItems( wfc.IList<DependencyObject> pVector);
		//public partial void AutomationGetContainerUIAPeerForItem(
		//	DependencyObject pItem,
		//	out xaml_automation_peers.ILoopingSelectorItemAutomationPeer ppPeer);
		//public partial void AutomationTryScrollItemIntoView( DependencyObject pItem);
		//public partial void AutomationClearPeerMap();
		//public partial void AutomationRealizeItemForAP( uint itemIdxToRealize);

		//// Used by LoopingSelectorItem to ScrollIntoView/Select
		//public partial void AutomationScrollToVisualIdx( int visualIdx, bool ignoreScrollingState = false);
		//public partial void AutomationGetSelectedItem(out LoopingSelectorItem ppItemNoRef);

		//public partial void VisualIndexToItemIndex( uint visualIndex, out uint itemIndex);

		// protected

		// FrameworkElementOverrides
		//protected partial void MeasureOverrideImpl(wf.Size availableSize, out wf.Size returnValue);
		//protected partial void ArrangeOverrideImpl(wf.Size finalSize, _Out_opt_ wf.Size returnValue);
		//protected partial void OnApplyTemplateImpl();

		// UIElementOverrides
		//protected partial void OnCreateAutomationPeerImpl(
		//	out  xaml.Automation.Peers.IAutomationPeer returnValue);

		// IControlOverrides
		//protected partial void OnKeyDownImpl( xaml_input.IKeyRoutedEventArgs pEventArgs);

		// private
		private enum ListEnd
		{
			Head,
			Tail
		};

		private enum ItemState
		{
			ManipulationInProgress,
			Expanded,
			LostFocus
		};

		// Template-part related state
		//EventRegistrationToken _viewChangedToken;
		//EventRegistrationToken _viewChangingToken;
		//EventRegistrationToken _pressedToken;
		//EventRegistrationToken _focusLost;
		//EventRegistrationToken _focusGot;
		//EventRegistrationToken _pointerEnteredToken;
		//EventRegistrationToken _pointerExitedToken;
		//EventRegistrationToken _upButtonClickedToken;
		//EventRegistrationToken _downButtonClickedToken;

		//private static char c_scrollViewerTemplatePart[];
		//private static char c_upButtonTemplatePart[];
		//private static char c_downButtonTemplatePart[];
		private static int c_automationLargeIncrement = 5;
		private static int c_automationSmallIncrement = 1;
		//private static double c_targetScreenWidth;

		private LoopingSelectorPanel _tpPanel;
		private ScrollViewer _tpScrollViewer;
		//private ScrollViewerPrivate _tpScrollViewerPrivate;
		private ScrollViewer _tpScrollViewerPrivate;
		private ButtonBase _tpUpButton;
		private ButtonBase _tpDownButton;

		// We keep a weak to the AP when it is created in order to be able
		// to update the map of items to data automation peers it maintains.
		private WeakReference<LoopingSelectorAutomationPeer> _wrAP;

		// We subscribe to the routed Got/LostFocus events. Focus moving between
		// subelements generates both a Lost and Got event. This boolean
		// tracks whether we actually have focus. When a manipulation begins
		// it does not always trigger a focus event (framework bug?). If the
		// control does not have focus it programatically forces focus.
		private bool _hasFocus;

		// Indicates whether the panel is sized. Reset
		// when the Items or ShouldLoop property is changed, or the control is
		// resized.
		private bool _isSized;

		// Indicates whether there is a pending setup operation. This operation
		// sets up realized bounds, realization starting idx, and the scroll
		// position.
		private bool _isSetupPending;

		// Before the first layout pass the scrollviewer isn't fully
		// initialized and calls to ScrollToOffsetWithOptionalAnimation
		// produce no effect. We prevent initialization of the scrollviewer's
		// scroll position and the realization of items until after it has
		// been fully initialized.
		private bool _isScrollViewerInitialized;

		// Normalization calls SetScrollPosition to jump the viewport back
		// to the center of the scrollable region. This causes an extra ViewChanged
		// event to occur. Because Normalization doesn't affect the itme Balance and
		// a balance had to of just occured we skip the next balance as an optimization.
		private bool _skipNextBalance;

		// When using UI automation, we want to be able to scroll without selecting.
		// We'll use this to temporarily disable updating the selected item
		// while scrolling using UI automation.
		private bool _skipSelectionChangeUntilFinalViewChanged;

		// When the ScrollViewer first initializes it invalidates its layout when
		// first setting up the ScrollContentPresenter. We skip the next arrange
		// as an optimization.
		private bool _skipNextArrange;

		// Item state keeps track of the visual state of all the items. This
		// allows us to properly transition between expanded and normal modes.
		private ItemState _itemState;

		// Where the viewport currently sits. Kept as object state because
		// ScrollViewer's extents will become inaccurate if a SetScrollPosition
		// has been called but an invalidate scroll info pass (internal to ScrollViewer)
		// hasn't occurred. These values are kept accurate in this situation.
		private double _unpaddedExtentTop;
		private double _unpaddedExtentBottom;

		// These values are updated as we add and remove items and serve
		// as the record of where our UI ELements are laid out.
		private double _realizedTop;
		private double _realizedBottom;

		// The top and bottom index don't loop. They are a purely
		// visual index, which is modded with the logical item count
		// to obtain a logical index.
		private int _realizedTopIdx;
		private int _realizedBottomIdx;

		// We store the visual index to ensure in cases where
		// multiple logical items are displayed on the screen
		// that more than one is not in the visual 'Selected'
		// state.
		private int _realizedMidpointIdx;

		// We cache this values so we're not continuously
		// looking them up.
		private uint _itemCount;
		private double _scaledItemHeight;
		private double _itemHeight;
		private double _itemWidth;

		// If the ItemWidth property is not explictly set, we fall back to a computed value equal to the width of the LoopingSelector.
		// This allows the LoopingSelectorItems to stretch to the available space if there is no explict width given.
		// Ordinarily, it is not necessary to set the Width of a UIElement if the desired behavior is for it to stretch to the available
		// space, but due to the fact that the LoopingSelectorPanel is a Canvas we always need to specify a width for the LoopingSelectorItems.
		private double _itemWidthFallback;

		// The panel size when calculated in EnsureSetup.
		private double _panelSize;

		// This isn't just _panelSize/2, it is snappoint aligned.
		private double _panelMidpointScrollPosition;

		// Used to guard against Balencing and other operations.
		// When SetScrollPosition is called it will synchronously flush
		// pending ViewChanging events.
		private bool _isWithinScrollChange;

		// Used to guard against Balencing and other operations when we are in
		// ArrangeOverride.
		private bool _isWithinArrangeOverride;

		// Prevents reaction to setting the index property in response to a
		// manipulation.
		private bool _disablePropertyChange;

		// Used to detect orientation changes.
		private double _lastArrangeSizeHeight;

		// When moving from a small viewport to a large viewport during an orientation change,
		// the viewport vertical offset might get coerced and change. If that's the case,
		// we defer the scroll operation to the next layout pass because it's too late by then.
		private double _delayScrollPositionY;

		// Track LSIs. All LSIs are in the Canvas's children, but recycled
		// LSIs are kept far offscreen.
		private readonly Stack<LoopingSelectorItem> _recycledItems = new Stack<LoopingSelectorItem>();
		private readonly LinkedList<LoopingSelectorItem> _realizedItems = new LinkedList<LoopingSelectorItem>();

		// LoopingSelectorAutomationPeer asks for items to be realized, but they're not brought into the canvas.
		// This map, indexed with the modded visual index, keeps track of those
		private readonly IDictionary<int, LoopingSelectorItem> _realizedItemsForAP = new Dictionary<int, LoopingSelectorItem>();

		// Automation cached values for property changed events.
		private double _tpPreviousScrollPosition;

		//private EventSource<SelectionChangedEventHandler> _selectionChangedEventSource;

		// Cached instance of the ICanvasStatics interface for performance
		// sensative operations.
		//private wrl.ComPtr<xaml_controls.ICanvasStatics> _spCanvasStatics;

		//private void OnViewChanged( DependencyObject pSender,  xaml_controls.IScrollViewerViewChangedEventArgs pEventArgs);
		//private void OnViewChanging( DependencyObject pSender,  xaml_controls.IScrollViewerViewChangingEventArgs pEventArgs);
		//private void OnPressed( DependencyObject pSender,  xaml.Input.IPointerRoutedEventArgs pEventArgs);
		//private void OnGotFocus( DependencyObject pSender,  xaml.IRoutedEventArgs pEventArgs);
		//private void OnLostFocus( DependencyObject pSender,  xaml.IRoutedEventArgs pEventArgs);
		//private void OnItemTapped( DependencyObject pSender,  xaml_input.ITappedRoutedEventArgs pEventArgs);
		//private void OnPointerEntered( DependencyObject pSender,  xaml_input.IPointerRoutedEventArgs pEventArgs);
		//private void OnPointerExited( DependencyObject pSender,  xaml_input.IPointerRoutedEventArgs pEventArgs);
		//private void OnUpButtonClicked( DependencyObject pSender,  xaml.IRoutedEventArgs pEventArgs);
		//private void OnDownButtonClicked( DependencyObject pSender,  xaml.IRoutedEventArgs pEventArgs);

		//private partial void RaiseOnSelectionChanged( DependencyObject pOldItem,  DependencyObject pNewItem);
		//private partial void IsTemplateAndItemsAttached(out bool shouldBalance);
		//private partial void IsSetupForAutomation(out bool isSetup);
		//private partial void GoToState( string strState,  bool useTransitions);

		//// Balance is the entry point for virtualization maintenance, calling
		//// Normalize, EnsureSetup, and UpdateSelectedItem when needed.
		//private partial void Balance( bool isOnSnapPoint);

		//// SetSelectedIndex handled the programatic changing of the
		//// index.
		//private partial void SetSelectedIndex( int oldIdx,  int newIdx);

		//// Change the selected item
		//private partial void SelectNextItem();
		//private partial void SelectPreviousItem();

		//private partial void HandlePageDownKeyPress();
		//private partial void HandlePageUpKeyPress();
		//private partial void HandleEndKeyPress();
		//private partial void HandleHomeKeyPress();

		//private partial void UpdateSelectedItem(bool ignoreScrollingState = false);
		//private partial void Normalize();
		//private partial void EnsureSetup();

		//// Helper automation functions
		//private partial void AutomationRaiseStructureChanged();
		//private partial void AutomationRaiseExpandCollapse();
		//private partial void AutomationRaiseSelectionChanged();

		// Helper functions
		//private partial void ClearAllItems();
		//private partial void HasFocus(out bool pHasFocus);
		//private partial void IsAscendantOfTarget( xaml.IDependencyObject pChild, out bool pIsChildOfTarget);
		//private partial void SizePanel();
		//private partial void SetScrollPosition( double offset,  bool useAnimation);
		//private partial void GetPanelChildren(out  wfc.IList<xaml.UIElement> ppChildren, out uint count);
		//private partial void MeasureExtent(out double extentTop, out double extentBottom);
		//private partial void Trim( ListEnd end);
		//private partial void Add( ListEnd end);
		//private partial void TransitionItemsState( ItemState state);
		//private partial void UpdateVisualSelectedItem( uint oldIdx,  uint newIdx);
		//private partial void RealizeItem( uint itemIdxToRealize, out  ILoopingSelectorItem ppItem);
		//private partial void RecycleItem( ILoopingSelectorItem pItem);
		//private partial void SetPosition( UIElement pItem,  double offset);
		//private partial void ShiftChildren( double delta);
		//private partial void SetupSnapPoints( double offset,  double size);
		//private partial void GetMaximumAddIndexPosition(out int headIdx, out int tailIdx);

		//private partial void ExpandIfNecessary();

		//private partial void RetreiveItemFromAPRealizedItems( uint moddeItemdIdx, out ILoopingSelectorItem ppItem);

		// Mods negative numbers in a way that makes sense
		// for generating an index position from an infinite
		// sequence.
		private uint PositiveMod(int x, int n)
		{
			return (uint)((x % n + n) % n);
		}

		//private partial void RequestInteractionSound(xaml.ElementSoundKind soundKind);
	};

	//ActivatableClassWithFactory(LoopingSelector, LoopingSelectorFactory);

}
