// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno;
using Uno.UI;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsRepeater : FrameworkElement
	{
		// Change to 'true' to turn on debugging outputs in Output window
		private static bool s_IsDebugOutputEnabled = false;

		private static Point ClearedElementsArrangePosition = new Point(-10000.0f, -10000.0f);

		// A convention we use in the ItemsRepeater codebase for an invalid Rect value.
		internal static Rect InvalidRect = new Rect(-1, -1, -1, -1);

		//public Microsoft.UI.Xaml.Controls.IElementFactoryShim ItemTemplateShim() { return m_itemTemplateWrapper; };
		internal ViewManager ViewManager => m_viewManager;
		public AnimationManager& AnimationManager() { return m_animationManager; }

		private bool IsProcessingCollectionChange => m_processingItemsSourceChange != null;

		AnimationManager m_animationManager ;
		ViewManager m_viewManager ;
		std..ViewportManager m_viewportManager ;

		ItemsSourceView m_itemsSourceView ;

		Microsoft.UI.Xaml.Controls.IElementFactoryShim m_itemTemplateWrapper ;

		VirtualizingLayoutContext m_layoutContext ;
		IInspectable m_layoutState ;
		// Value is different from null only while we are on the OnItemsSourceChanged call stack.
		NotifyCollectionChangedEventArgs m_processingItemsSourceChange ;

		Size m_lastAvailableSize;
		bool m_isLayoutInProgress;
		// The value of _layoutOrigin is expected to be set by the layout
		// when it gets measured. It should not be used outside of measure.
		Point m_layoutOrigin;

		// Event revokers
		ItemsSourceView.CollectionChanged_revoker m_itemsSourceViewChanged;
		Layout.MeasureInvalidated_revoker m_measureInvalidated;
		Layout.ArrangeInvalidated_revoker m_arrangeInvalidated;

		// Cached Event args to avoid creation cost every time
		private ItemsRepeaterElementPreparedEventArgs m_elementPreparedArgs;
		private ItemsRepeaterElementClearingEventArgs m_elementClearingArgs;
		private ItemsRepeaterElementIndexChangedEventArgs m_elementIndexChangedArgs;

		// Loaded events fire on the first tick after an element is put into the tree 
		// while unloaded is posted on the UI tree and may be processed out of sync with subsequent loaded
		// events. We keep these counters to detect out-of-sync unloaded events and take action to rectify.
		int _loadedCounter;
		int _unloadedCounter;

		// Bug in framework's reference tracking causes crash during
		// UIAffinityQueue cleanup. To avoid that bug, take a strong ref
		IElementFactory m_itemTemplate;
		Layout m_layout;
		ElementAnimator m_animator;

		// Bug where DataTemplate with no content causes a crash.
		// See: https://github.com/microsoft/microsoft-ui-xaml/issues/776
		// Solution: Have flag that is only true when DataTemplate exists but it is empty.
		bool m_isItemTemplateEmpty;


		public ItemsRepeater()
		{
			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_ItemsRepeater);

			//if (SharedHelpers.IsRS5OrHigher())
			{
				m_viewportManager = new ViewportManagerWithPlatformFeatures(this);
			}
			//else
			//{
			//	m_viewportManager = std.new ViewportManagerDownLevel(this);
			//}

			

			AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
			//if (SharedHelpers.IsRS3OrHigher())
			{
				TabFocusNavigation(winrt.KeyboardNavigationMode.Once);
				XYFocusKeyboardNavigation(winrt.XYFocusKeyboardNavigationMode.Enabled);
			}

			Loaded(this, OnLoaded);
			Unloaded(this, OnUnloaded);

			// Initialize the cached layout to the default value
			var layout = Layout as VirtualizingLayout;
			OnLayoutChanged(null, layout);
		}

		~ItemsRepeater()
		{
			m_itemTemplate = null;
			m_animator = null;
			m_layout = null;
		}

		#region IUIElementOverrides

		AutomationPeer OnCreateAutomationPeer()
		{
			return new RepeaterAutomationPeer(this);
		}

		#endregion

		#region IUIElementOverrides7

		IEnumerable<DependencyObject> GetChildrenInTabFocusOrder()
		{
			return CreateChildrenInTabFocusOrderIterable();
		}

		#endregion

		#region IUIElementOverrides8

		private void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs e)
		{
			m_viewportManager.OnBringIntoViewRequested(e);
		}

		#endregion

		#region IFrameworkElementOverrides

		protected override Size MeasureOverride(Size availableSize)
		{
			if (m_isLayoutInProgress)
			{
				throw new InvalidOperationException("Reentrancy detected during layout.");
			}

			if (IsProcessingCollectionChange)
			{
				throw new InvalidOperationException("Cannot run layout in the middle of a collection change.");
			}

			m_viewportManager.OnOwnerMeasuring();

			m_isLayoutInProgress = true;
			using var layoutInProgress = Uno.Disposables.Disposable.Create(() => m_isLayoutInProgress = false);

			m_viewManager.PrunePinnedElements();
			Rect extent = default;
			Size desiredSize = default;

			var layout = Layout;
			if (layout != null)
			{
				var layoutContext = GetLayoutContext();

				// Expensive operation, do it only in debug builds.
#if DEBUG
				var virtualContext = (VirtualizingLayoutContext) layoutContext;
				virtualContext.Indent(Indent());
#endif

				// Checking if we have an data template and it is empty
				if (m_isItemTemplateEmpty)
				{
					// Has no content, so we will draw nothing and request zero space
					extent = new Rect(m_layoutOrigin.X, m_layoutOrigin.Y, 0, 0);
				}
				else
				{
					desiredSize = layout.Measure(layoutContext, availableSize);
					extent = new Rect(m_layoutOrigin.X, m_layoutOrigin.Y, desiredSize.Width, desiredSize.Height);
				}

				// Clear var recycle candidate elements that have not been kept alive by layout - i.e layout did not
				// call GetElementAt(index).
				var children = Children;
				for (var i = 0; i < children.Count; ++i)
				{
					var element = children[i];
					var virtInfo = GetVirtualizationInfo(element);

					if (virtInfo.Owner() == ElementOwner.Layout &&
						virtInfo.AutoRecycleCandidate() &&
						!virtInfo.KeepAlive())
					{
						REPEATER_TRACE_INFO("AutoClear - %d \n", virtInfo.Index());
						ClearElementImpl(element);
					}
				}
			}

			m_viewportManager.SetLayoutExtent(extent);
			m_lastAvailableSize = availableSize;
			return desiredSize;
		}

		protected  override Size ArrangeOverride(Size finalSize)
		{
			if (m_isLayoutInProgress)
			{
				throw new InvalidOperationException("Reentrancy detected during layout.");
			}

			if (IsProcessingCollectionChange())
			{
				throw new InvalidOperationException("Cannot run layout in the middle of a collection change.");
			}

			m_isLayoutInProgress = true;
			using var layoutInProgress = Uno.Disposables.Disposable.Create(() => m_isLayoutInProgress = false);

			Size arrangeSize = default;
			var layout = Layout;
			if (layout != null)
			{
				arrangeSize = layout.Arrange(GetLayoutContext(), finalSize);
			}

			// The view manager might clear elements during this call.
			// That's why we call it before arranging cleared elements
			// off screen.
			m_viewManager.OnOwnerArranged();

			var children = Children;
			for (var i = 0; i < children.Count; ++i)
			{
				var element = children[i];
				var virtInfo = GetVirtualizationInfo(element);
				virtInfo.KeepAlive(false);

				if (virtInfo.Owner() == ElementOwner.ElementFactory ||
					virtInfo.Owner() == ElementOwner.PinnedPool)
				{
					// Toss it away. And arrange it with size 0 so that XYFocus won't use it.
					element.Arrange(new Rect(
						ClearedElementsArrangePosition.X - (float)(element.DesiredSize().Width),
						ClearedElementsArrangePosition.Y - (float)(element.DesiredSize().Height),
						0.0,
						0.0));
				}
				else
				{
					var newBounds = CachedVisualTreeHelpers.GetLayoutSlot(element as FrameworkElement);

					if (virtInfo.ArrangeBounds() != ItemsRepeater.InvalidRect &&
						newBounds != virtInfo.ArrangeBounds())
					{
						m_animationManager.OnElementBoundsChanged(element, virtInfo.ArrangeBounds(), newBounds);
					}

					virtInfo.ArrangeBounds(newBounds);
				}
			}

			m_viewportManager.OnOwnerArranged();
			m_animationManager.OnOwnerArranged();

			return arrangeSize;
		}

		#endregion

		#region IRepeater interface.

		private ItemsSourceView ItemsSourceView()
		{
			return m_itemsSourceView.get();
		}

		public int GetElementIndex(UIElement element)
		{
			return GetElementIndexImpl(element);
		}

		UIElement TryGetElement(int index)
		{
			return GetElementFromIndexImpl(index);
		}

		void PinElement(UIElement const& element)
		{
			m_viewManager.UpdatePin(element, true /* addPin */);
		}

		void UnpinElement(UIElement const& element)
		{
			m_viewManager.UpdatePin(element, false /* addPin */);
		}

		UIElement GetOrCreateElement(int index)
		{
			return GetOrCreateElementImpl(index);
		}

		#endregion

		UIElement GetElementImpl(int index, bool forceCreate, bool suppressAutoRecycle)
		{
			var element = m_viewManager.GetElement(index, forceCreate, suppressAutoRecycle);
			return element;
		}

		void ClearElementImpl(const UIElement  & element)
		{
			// Clearing an element due to a collection change
			// is more strict in that pinned elements will be forcibly
			// unpinned and sent back to the view generator.
			const bool isClearedDueToCollectionChange =
				IsProcessingCollectionChange() &&
				(m_processingItemsSourceChange.get().Action() == NotifyCollectionChangedAction.Remove ||
					m_processingItemsSourceChange.get().Action() == NotifyCollectionChangedAction.Replace ||
					m_processingItemsSourceChange.get().Action() == NotifyCollectionChangedAction.Reset);

			m_viewManager.ClearElement(element, isClearedDueToCollectionChange);
			m_viewportManager.OnElementCleared(element);
		}

		int GetElementIndexImpl(const UIElement  & element)
		{
			// Verify that element is actually a child of this ItemsRepeater
			var  const parent  = VisualTreeHelper.GetParent(element);
			if (parent == this)
			{
				var virtInfo = TryGetVirtualizationInfo(element);
				return m_viewManager.GetElementIndex(virtInfo);
			}

			return -1;
		}

		UIElement GetElementFromIndexImpl(int index)
		{
			UIElement result = null;

			var children = Children();
			for (unsigned i = 0u; i < children.Size() && !result; ++i)
			{
				var element = children.GetAt(i);
				var virtInfo = TryGetVirtualizationInfo(element);
				if (virtInfo && virtInfo.IsRealized() && virtInfo.Index() == index)
				{
					result = element;
				}
			}

			return result;
		}

		UIElement GetOrCreateElementImpl(int index)
		{
			if (index >= 0 && index >= ItemsSourceView().Count())
			{
				throw hresult_invalid_argument("Argument index is invalid.");
			}

			if (m_isLayoutInProgress)
			{
				throw hresult_error(E_FAIL, "GetOrCreateElement invocation is not allowed during layout.");
			}

			var element = GetElementFromIndexImpl(index);
			const bool isAnchorOutsideRealizedRange = !element;

			if (isAnchorOutsideRealizedRange)
			{
				if (!Layout())
				{
					throw hresult_error(E_FAIL, "Cannot make an Anchor when there is no attached layout.");
				}

				element = GetLayoutContext().GetOrCreateElementAt(index);
				element.Measure({
					std.numeric_limits<float>.infinity(), std.numeric_limits<float>.infinity()
				});
			}

			m_viewportManager.OnMakeAnchor(element, isAnchorOutsideRealizedRange);
			InvalidateMeasure();

			return element;
		}

		/*static*/
		com_ptr<VirtualizationInfo> TryGetVirtualizationInfo(const UIElement  & element)
		{
			var value = element.GetValue(GetVirtualizationInfoProperty());
			return get_self<VirtualizationInfo>(value).get_strong();
		}

		/*static*/
		com_ptr<VirtualizationInfo> GetVirtualizationInfo(const UIElement  & element)
		{
			var result = TryGetVirtualizationInfo(element);

			if (!result)
			{
				result = CreateAndInitializeVirtualizationInfo(element);
			}

			return result;
		}

		/* static */
		com_ptr<VirtualizationInfo> CreateAndInitializeVirtualizationInfo(const UIElement  & element)
		{
			MUX_ASSERT(!TryGetVirtualizationInfo(element));
			var result = new VirtualizationInfo();
			(element.SetValue(GetVirtualizationInfoProperty(), result as IInspectable));
			return result;
		}

		void OnPropertyChanged(const DependencyPropertyChangedEventArgs  & args)
		{
			IDependencyProperty property = args.Property();

			if (property == s_ItemsSourceProperty)
			{
				if (args.NewValue() != args.OldValue())
				{
					var newValue = args.NewValue();
					var newDataSource = newValue.try_as<ItemsSourceView>();
					if (newValue && !newDataSource)
					{
						newDataSource = ItemsSourceView(newValue);
					}

					OnDataSourcePropertyChanged(m_itemsSourceView.get(), newDataSource);
				}
			}
			else if (property == s_ItemTemplateProperty)
			{
				(OnItemTemplateChanged(args.OldValue(). as<IElementFactory > (), args.NewValue() as IElementFactory));
			}
			else if (property == s_LayoutProperty)
			{
				(OnLayoutChanged(args.OldValue(). as<Layout > (), args.NewValue() as Layout));
			}
			else if (property == s_AnimatorProperty)
			{
				(OnAnimatorChanged(args.OldValue(). as<ElementAnimator > (), args.NewValue() as ElementAnimator));
			}
			else if (property == s_HorizontalCacheLengthProperty)
			{
				m_viewportManager.HorizontalCacheLength(unbox_value<double>(args.NewValue()));
			}
			else if (property == s_VerticalCacheLengthProperty)
			{
				m_viewportManager.VerticalCacheLength(unbox_value<double>(args.NewValue()));
			}
		}

		void OnElementPrepared(const UIElement  & element, int index)
		{
			m_viewportManager.OnElementPrepared(element);
			if (m_elementPreparedEventSource)
			{
				if (!m_elementPreparedArgs)
				{
					m_elementPreparedArgs = ItemsRepeaterElementPreparedEventArgs(this, new ItemsRepeaterElementPreparedEventArgs(element, index));
				}
				else
				{
					get_self<ItemsRepeaterElementPreparedEventArgs>(m_elementPreparedArgs.get()).Update(element, index);
				}

				m_elementPreparedEventSource(this, m_elementPreparedArgs.get());
			}
		}

		void OnElementClearing(const UIElement  & element)
		{
			if (m_elementClearingEventSource)
			{
				if (!m_elementClearingArgs)
				{
					m_elementClearingArgs = ItemsRepeaterElementClearingEventArgs(this, new ItemsRepeaterElementClearingEventArgs(element));
				}
				else
				{
					get_self<ItemsRepeaterElementClearingEventArgs>(m_elementClearingArgs.get()).Update(element);
				}

				m_elementClearingEventSource(this, m_elementClearingArgs.get());
			}
		}

		void OnElementIndexChanged(const UIElement  & element, int oldIndex, int newIndex)
		{
			if (m_elementIndexChangedEventSource)
			{
				if (!m_elementIndexChangedArgs)
				{
					m_elementIndexChangedArgs = ItemsRepeaterElementIndexChangedEventArgs(this, new ItemsRepeaterElementIndexChangedEventArgs(element, oldIndex, newIndex));
				}
				else
				{
					get_self<ItemsRepeaterElementIndexChangedEventArgs>(m_elementIndexChangedArgs.get()).Update(element, oldIndex, newIndex);
				}

				m_elementIndexChangedEventSource(this, m_elementIndexChangedArgs.get());
			}
		}

		// Provides an indentation based on repeater elements in the UI Tree that
		// can be used to make logging a little easier to read.
		int Indent()
		{
			int indent = 1;

			// Expensive, so we do it only in debug builds.
#ifdef _DEBUG
			(var parent = this.Parent() as FrameworkElement);
			while (parent && !parent.try_as<ItemsRepeater>())
			{
				(parent = parent.Parent() as FrameworkElement);
			}

			if (parent)
			{
				(var parentRepeater = get_self<ItemsRepeater>(parent as ItemsRepeater));
				indent = parentRepeater.Indent();
			}

#endif

			return indent * 4;
		}

		void OnLoaded(const IInspectable  & /*sender*/, const RoutedEventArgs  & /*args*/)
		{
			// If we skipped an unload event, reset the scrollers now and invalidate measure so that we get a new
			// layout pass during which we will hookup new scrollers.
			if (_loadedCounter > _unloadedCounter)
			{
				InvalidateMeasure();
				m_viewportManager.ResetScrollers();
			}

			++_loadedCounter;
		}

		void OnUnloaded(const IInspectable  & /*sender*/, const RoutedEventArgs  & /*args*/)
		{
			++_unloadedCounter;
			// Only reset the scrollers if this unload event is in-sync.
			if (_unloadedCounter == _loadedCounter)
			{
				m_viewportManager.ResetScrollers();
			}
		}

		void OnDataSourcePropertyChanged(const ItemsSourceView  & oldValue, const ItemsSourceView  & newValue)
		{
			if (m_isLayoutInProgress)
			{
				throw hresult_error(E_FAIL, "Cannot set ItemsSourceView during layout.");
			}

			m_itemsSourceView.set(newValue);

			if (oldValue)
			{
				m_itemsSourceViewChanged.revoke();
			}



			if (newValue)
			{
				m_itemsSourceViewChanged = newValue.CollectionChanged(auto_revoke,  {
					this, &ItemsRepeater.OnItemsSourceViewChanged
				});
			}

			if (var const layout  = Layout())
			{
				var  const args  = NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Reset,
					null /* newItems */,
					null /* oldItems */,
					-1 /* newIndex */,
					-1 /* oldIndex */);
				args.Action();
				var  const processingChange  = gsl.finally([this]()
				{
					m_processingItemsSourceChange.set(null);
				});
				m_processingItemsSourceChange.set(args);

				if (var const virtualLayout  = layout.try_as<VirtualizingLayout>())
				{
					virtualLayout.OnItemsChangedCore(GetLayoutContext(), newValue, args);
				}
				else if (var const nonVirtualLayout  = layout.try_as<NonVirtualizingLayout>())
				{
					// Walk through all the elements and make sure they are cleared for
					// non-virtualizing layouts.
					for ( const auto 
					&element: Children())
					{
						if (GetVirtualizationInfo(element).IsRealized())
						{
							ClearElementImpl(element);
						}
					}

					Children().Clear();
				}

				InvalidateMeasure();
			}
		}

		void OnItemTemplateChanged(const IElementFactory  & oldValue, const IElementFactory  & newValue)
		{
			if (m_isLayoutInProgress && oldValue)
			{
				throw hresult_error(E_FAIL, "ItemTemplate cannot be changed during layout.");
			}

			// Since the ItemTemplate has changed, we need to re-evaluate all the items that
			// have already been created and are now in the tree. The easiest way to do that
			// would be to do a reset.. Note that this has to be done before we change the template
			// so that the cleared elements go back into the old template.
			if (var const layout  = Layout())
			{
				var  const args  = NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Reset,
					null /* newItems */,
					null /* oldItems */,
					-1 /* newIndex */,
					-1 /* oldIndex */);
				args.Action();
				var  const processingChange  = gsl.finally([this]()
				{
					m_processingItemsSourceChange.set(null);
				});
				m_processingItemsSourceChange.set(args);

				if (var const virtualLayout  = layout.try_as<VirtualizingLayout>())
				{
					virtualLayout.OnItemsChangedCore(GetLayoutContext(), newValue, args);
				}
				else if (var const nonVirtualLayout  = layout.try_as<NonVirtualizingLayout>())
				{
					// Walk through all the elements and make sure they are cleared for
					// non-virtualizing layouts.
					for (var  const
					&child : Children())
					{
						if (GetVirtualizationInfo(child).IsRealized())
						{
							ClearElementImpl(child);
						}
					}
				}
			}

			if (!SharedHelpers.IsRS5OrHigher())
			{
				// Bug in framework's reference tracking causes crash during
				// UIAffinityQueue cleanup. To avoid that bug, take a strong ref
				m_itemTemplate = newValue;
			}

			// Clear flag for bug #776
			m_isItemTemplateEmpty = false;
			m_itemTemplateWrapper = newValue.try_as<IElementFactoryShim>();
			if (!m_itemTemplateWrapper)
			{
				// ItemTemplate set does not implement IElementFactoryShim. We also 
				// want to support DataTemplate and DataTemplateSelectors automagically.
				if (var dataTemplate = newValue.try_as<DataTemplate>())
				{
					m_itemTemplateWrapper = new ItemTemplateWrapper(dataTemplate);
					( if (!dataTemplate.LoadContent() as FrameworkElement)) {
						// We have a DataTemplate which is empty, so we need to set it to true
						m_isItemTemplateEmpty = true;
					}
				}
				else if (var selector = newValue.try_as<DataTemplateSelector>())
				{
					m_itemTemplateWrapper = new ItemTemplateWrapper(selector);
				}
				else
				{
					throw hresult_invalid_argument("ItemTemplate");
				}
			}

			InvalidateMeasure();
		}

		void OnLayoutChanged(const Layout  & oldValue, const Layout  & newValue)
		{
			if (m_isLayoutInProgress)
			{
				throw hresult_error(E_FAIL, "Layout cannot be changed during layout.");
			}

			m_viewManager.OnLayoutChanging();
			m_animationManager.OnLayoutChanging();

			if (oldValue)
			{
				oldValue.UninitializeForContext(GetLayoutContext());
				m_measureInvalidated.revoke();
				m_arrangeInvalidated.revoke();

				// Walk through all the elements and make sure they are cleared
				var children = Children();
				for (unsigned i = 0u; i < children.Size(); ++i)
				{
					var element = children.GetAt(i);
					if (GetVirtualizationInfo(element).IsRealized())
					{
						ClearElementImpl(element);
					}
				}

				m_layoutState.set(null);
			}

			if (!SharedHelpers.IsRS5OrHigher())
			{
				// Bug in framework's reference tracking causes crash during
				// UIAffinityQueue cleanup. To avoid that bug, take a strong ref
				m_layout = newValue;
			}

			if (newValue)
			{
				newValue.InitializeForContext(GetLayoutContext());
				m_measureInvalidated = newValue.MeasureInvalidated(auto_revoke,  {
					this, &ItemsRepeater.InvalidateMeasureForLayout
				});
				m_arrangeInvalidated = newValue.ArrangeInvalidated(auto_revoke,  {
					this, &ItemsRepeater.InvalidateArrangeForLayout
				});
			}

			bool isVirtualizingLayout = newValue != null && newValue.try_as<VirtualizingLayout>() != null;
			m_viewportManager.OnLayoutChanged(isVirtualizingLayout);
			InvalidateMeasure();
		}

		void OnAnimatorChanged(const ElementAnimator  & /* oldValue */, const ElementAnimator  & newValue)
		{
			m_animationManager.OnAnimatorChanged(newValue);
			if (!SharedHelpers.IsRS5OrHigher())
			{
				// Bug in framework's reference tracking causes crash during
				// UIAffinityQueue cleanup. To avoid that bug, take a strong ref
				m_animator = newValue;
			}
		}

		void OnItemsSourceViewChanged(const IInspectable  & sender, const NotifyCollectionChangedEventArgs  & args)
		{
			if (m_isLayoutInProgress)
			{
				// Bad things will follow if the data changes while we are in the middle of a layout pass.
				throw hresult_error(E_FAIL, "Changes in data source are not allowed during layout.");
			}

			if (IsProcessingCollectionChange())
			{
				throw hresult_error(E_FAIL, "Changes in the data source are not allowed during another change in the data source.");
			}

			m_processingItemsSourceChange.set(args);
			var processingChange = gsl.finally([this]()
			{
				m_processingItemsSourceChange.set(null);
			});

			m_animationManager.OnItemsSourceChanged(sender, args);
			m_viewManager.OnItemsSourceChanged(sender, args);

			if (var layout = Layout())
			{
				if (var virtualLayout = layout.try_as<VirtualizingLayout>())
				{
					virtualLayout.OnItemsChangedCore(GetLayoutContext(), sender, args);
				}
				else
				{
					// NonVirtualizingLayout
					InvalidateMeasure();
				}
			}
		}

		void InvalidateMeasureForLayout(Layout const&, IInspectable const&)
		{
			InvalidateMeasure();
		}

		void InvalidateArrangeForLayout(Layout const&, IInspectable const&)
		{
			InvalidateArrange();
		}

		VirtualizingLayoutContext GetLayoutContext()
		{
			if (!m_layoutContext)
			{
				m_layoutContext.set(new RepeaterLayoutContext(this));
			}

			return m_layoutContext.get();
		}

		IEnumerable<DependencyObject> CreateChildrenInTabFocusOrderIterable()
		{
			var children = Children();
			if (children.Size() > 0u)
			{
				return new ChildrenInTabFocusOrderIterable(this);
			}

			return null;
		}
	}
}
