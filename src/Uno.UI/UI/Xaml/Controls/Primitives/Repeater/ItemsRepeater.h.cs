// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsRepeater : FrameworkElement
		//public ReferenceTracker<ItemsRepeater, DeriveFromPanelHelper_base, ItemsRepeater, IItemsRepeater2>,
		//public ItemsRepeaterProperties
	{

		// TODO UNO using ItemsRepeaterProperties.Background;

		//#pragma region IUIElementOverrides

		//    AutomationPeer OnCreateAutomationPeer();

		//#pragma endregion

		//#pragma region IUIElementOverrides7

		//    IIterable<DependencyObject> GetChildrenInTabFocusOrder();

		//#pragma endregion

		//#pragma region IUIElementOverrides8

		//    void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs const& e);

		//#pragma endregion

		//#pragma region IFrameworkElementOverrides

		//    Size MeasureOverride(Size const& availableSize);
		//    Size ArrangeOverride(Size const& finalSize);

		//#pragma endregion

		//#pragma region IRepeater interface.

		//    ItemsSourceView ItemsSourceView();

		//    // Mapping APIs
		//    int GetElementIndex(UIElement const& element);
		//    UIElement TryGetElement(int index);
		//    UIElement GetOrCreateElement(int index);

		//#pragma endregion


		//Microsoft.UI.Xaml.Controls.IElementFactoryShim ItemTemplateShim() { return m_itemTemplateWrapper; };
		//internal ViewManager ViewManager => m_viewManager; 
		//internal AnimationManager AnimationManager => m_animationManager;

		//UIElement GetElementImpl(int index, bool forceCreate, bool suppressAutoRecycle);
		//void ClearElementImpl(const UIElement& element);

		//// Mapping APIs (exception based)
		//int GetElementIndexImpl(const UIElement& element);
		//UIElement GetElementFromIndexImpl(int index);


		//UIElement GetOrCreateElementImpl(int index);

		//static com_ptr<VirtualizationInfo> TryGetVirtualizationInfo(const UIElement& element);
		//static com_ptr<VirtualizationInfo> GetVirtualizationInfo(const UIElement& element);
		//static com_ptr<VirtualizationInfo> CreateAndInitializeVirtualizationInfo(const UIElement& element);

		internal object LayoutState
		{
			get => m_layoutState;
			set => m_layoutState = value;
		}

		internal Rect VisibleWindow => m_viewportManager.GetLayoutVisibleWindow();
		internal Rect RealizationWindow => m_viewportManager.GetLayoutRealizationWindow();
		internal UIElement SuggestedAnchor => m_viewportManager.SuggestedAnchor;
		internal UIElement MadeAnchor => m_viewportManager.MadeAnchor;

		public Point LayoutOrigin
		{
			get => m_layoutOrigin;
			set => m_layoutOrigin = value;
		}

		// Pinning APIs
		//void PinElement(UIElement const& element);
		//void UnpinElement(UIElement const& element);

		//void OnPropertyChanged(const DependencyPropertyChangedEventArgs& args);

		//void OnElementPrepared(const UIElement& element, int index);
		//void OnElementClearing(const UIElement& element);
		//void OnElementIndexChanged(const UIElement& element, int oldIndex, int newIndex);

		static DependencyProperty GetVirtualizationInfoProperty()
		{
			static GlobalDependencyProperty s_VirtualizationInfoProperty =
			InitializeDependencyProperty(
				"VirtualizationInfo",
				name_of<IInspectable>(),
				name_of<ItemsRepeater>(),
				true /* isAttached */,
				null /* defaultValue */);

			return s_VirtualizationInfoProperty;
		}

		//int Indent();

		//private:
		//void OnLoaded(const IInspectable& /*sender*/, const RoutedEventArgs& /*args*/);
		//void OnUnloaded(const IInspectable& /*sender*/, const RoutedEventArgs& /*args*/);

		//void OnDataSourcePropertyChanged(const ItemsSourceView& oldValue, const ItemsSourceView& newValue);
		//void OnItemTemplateChanged(const IElementFactory& oldValue, const IElementFactory& newValue);
		//void OnLayoutChanged(const Layout& oldValue, const Layout& newValue);
		//void OnAnimatorChanged(const ElementAnimator& oldValue, const ElementAnimator& newValue);

		//void OnItemsSourceViewChanged(const IInspectable& sender, const NotifyCollectionChangedEventArgs& args);
		//void InvalidateMeasureForLayout(Layout const& sender, IInspectable const& args);
		//void InvalidateArrangeForLayout(Layout const& sender, IInspectable const& args);

		//VirtualizingLayoutContext GetLayoutContext();
		//bool IsProcessingCollectionChange() const { return m_processingItemsSourceChange != null; }

		//IIterable<DependencyObject> CreateChildrenInTabFocusOrderIterable();

		//.AnimationManager m_animationManager{ this };
		//.ViewManager m_viewManager{ this };
		//std..ViewportManager m_viewportManager{ null };

		//ItemsSourceView m_itemsSourceView{ this };

		//Microsoft.UI.Xaml.Controls.IElementFactoryShim m_itemTemplateWrapper{ null };

		//VirtualizingLayoutContext m_layoutContext{ this };
		//IInspectable m_layoutState{ this };
		//// Value is different from null only while we are on the OnItemsSourceChanged call stack.
		//NotifyCollectionChangedEventArgs m_processingItemsSourceChange{ this };

		//Size m_lastAvailableSize{};
		//bool m_isLayoutInProgress{ false };
		//// The value of _layoutOrigin is expected to be set by the layout
		//// when it gets measured. It should not be used outside of measure.
		//Point m_layoutOrigin{};

		//// Event revokers
		//ItemsSourceView.CollectionChanged_revoker m_itemsSourceViewChanged{};
		//Layout.MeasureInvalidated_revoker m_measureInvalidated{};
		//Layout.ArrangeInvalidated_revoker m_arrangeInvalidated{};

		//// Cached Event args to avoid creation cost every time
		//ItemsRepeaterElementPreparedEventArgs m_elementPreparedArgs{ this };
		//ItemsRepeaterElementClearingEventArgs m_elementClearingArgs{ this };
		//ItemsRepeaterElementIndexChangedEventArgs m_elementIndexChangedArgs{ this };

		//// Loaded events fire on the first tick after an element is put into the tree 
		//// while unloaded is posted on the UI tree and may be processed out of sync with subsequent loaded
		//// events. We keep these counters to detect out-of-sync unloaded events and take action to rectify.
		//int _loadedCounter{};
		//int _unloadedCounter{};

		//// Bug in framework's reference tracking causes crash during
		//// UIAffinityQueue cleanup. To avoid that bug, take a strong ref
		//IElementFactory m_itemTemplate{ null };
		//Layout m_layout{ null };
		//ElementAnimator m_animator{ null };

		//// Bug where DataTemplate with no content causes a crash.
		//// See: https://github.com/microsoft/microsoft-ui-xaml/issues/776
		//// Solution: Have flag that is only true when DataTemplate exists but it is empty.
		//bool m_isItemTemplateEmpty{ false };
	}

}
