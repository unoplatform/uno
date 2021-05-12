using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Uno;

namespace Windows.UI.Xaml.Controls.Primitives
{
	// This file is aimed to implement methods that should be implemented by the ModernCollectionBasePanel which is not present in Uno

	partial class CalendarPanel : ILayoutDataInfoProvider
	{
		internal class ContainersCache : IItemContainerMapping
		{
			private readonly Dictionary<int, (object item, UIElement container)> _byIndex = new Dictionary<int, (object item, UIElement container)>();
			private readonly Dictionary<object, (int index, UIElement container)> _byItem = new Dictionary<object, (int index, UIElement container)>();
			private readonly Dictionary<UIElement, (int index, object item)> _byContainer = new Dictionary<UIElement, (int index, object item)>();

			private CalendarViewGeneratorHost _host;

			internal CalendarViewGeneratorHost Host
			{
				get => _host;
				set
				{
					_host = value;
					_byIndex.Clear();
					_byIndex.Clear();
					_byContainer.Clear();
				}
			}

			internal (UIElement container, bool isNew) GetOrCreate(int index)
			{
				if (_byIndex.TryGetValue(index, out var cached))
				{
					return (cached.container, false);
				}
				else if (_host is null)
				{
					throw new InvalidOperationException("Host not set.");
				}
				else
				{
					var item = _host[index];
					var container = (UIElement)_host.GetContainerForItem(item, null);

					_byIndex[index] = (item, container);
					_byItem[item] = (index, container);
					_byContainer[container] = (index, item);

					_host.PrepareItemContainer(container, item);

					return (container, true);
				}
			}

			/// <inheritdoc />
			public object ItemFromContainer(DependencyObject container)
				=> container is UIElement elt && _byContainer.TryGetValue(elt, out var cached) ? cached.item : default;

			/// <inheritdoc />
			public DependencyObject ContainerFromItem(object item)
				=> _byItem.TryGetValue(item, out var cached) ? cached.container : default;

			/// <inheritdoc />
			public int IndexFromContainer(DependencyObject container)
				=> container is UIElement elt && _byContainer.TryGetValue(elt, out var cached) ? cached.index : default;

			/// <inheritdoc />
			public DependencyObject ContainerFromIndex(int index)
				=> _byIndex.TryGetValue(index, out var cached) ? cached.container : default;
		}

		internal event VisibleIndicesUpdatedEventCallback VisibleIndicesUpdated;

		private readonly ContainersCache _cache = new ContainersCache();
		private CalendarLayoutStrategy _layoutStrategy;
		private CalendarViewGeneratorHost _host;

		internal void RegisterItemsHost(CalendarViewGeneratorHost pHost)
		{
			_host = pHost;
			_cache.Host = pHost;
			ContainerManager.Host = pHost;
		}

		internal void DisconnectItemsHost()
			=> RegisterItemsHost(null);

		internal int FirstVisibleIndexBase { get; private set; }
		internal int LastVisibleIndexBase { get; private set; }
		internal int FirstCacheIndexBase { get; private set; }
		internal int LastCacheIndexBase { get; private set; }

		[NotImplemented]
		internal PanelScrollingDirection PanningDirectionBase { get; } = PanelScrollingDirection.None;

		internal DependencyObject ContainerFromIndex(int index)
			=> _cache.GetOrCreate(index).container;

		private Size base_MeasureOverride(Size availableSize)
		{
			if (_host is null || _layoutStrategy is null)
			{
				return default;
			}

			// GOTO TO TODAY: EstimateElementIndex

			_layoutStrategy.BeginMeasure();
			ShouldInterceptInvalidate = true;
			try
			{
				var index = -1;
				var count = _host.Count;
				var layout = new LayoutReference{RelativeLocation = ReferenceIdentity.Myself};
				var window = new Rect(default, availableSize);

				while (
					++index < count
					&& _layoutStrategy.ShouldContinueFillingUpSpace(ElementType.ItemContainer, index, layout, window))
				{
					var (container, isNew) = _cache.GetOrCreate(index);
					if (isNew || !Children.Contains(container)) // TODO: Our Children are being altered, we cannot trust the isNew! :@
					{
						Children.Add((UIElement)container);
					}
					var itemsSize = _layoutStrategy.GetElementMeasureSize(ElementType.ItemContainer, index, window);
					var itemBounds = _layoutStrategy.GetElementBounds(ElementType.ItemContainer, index, itemsSize, layout, window);

					container.Measure(itemsSize);
					container.GetVirtualizationInformation().MeasureSize = itemsSize;

					layout.RelativeLocation = ReferenceIdentity.AfterMe;
					layout.ReferenceBounds = itemBounds;
				}

				StartIndex = 0;
				FirstVisibleIndexBase = 0;
				LastVisibleIndexBase = index;
			}
			finally
			{
				ShouldInterceptInvalidate = false;
				_layoutStrategy.EndMeasure();
			}
			VisibleIndicesUpdated?.Invoke(this, null);

			//foreach (var item in _host)
			//{

			//}

			//var s = _layoutStrategy.GetDesiredViewportSize();


			//return s;

			_layoutStrategy.EstimatePanelExtent(
				default /* not used by CalendarLayoutStrategyImpl */,
				default /* not used by CalendarLayoutStrategyImpl */,
				default /* not used by CalendarLayoutStrategyImpl */,
				out var desiredSize);

			return desiredSize;
		}

		private Size base_ArrangeOverride(Size finalSize)
		{
			var layout = new LayoutReference(); // Empty layout which will actually drive the ShouldContinueFillingUpSpace to always return true
			var window = new Rect(default, finalSize);

			foreach (var child in Children)
			{
				var index = _cache.IndexFromContainer(child);
				var bounds = _layoutStrategy.GetElementBounds(ElementType.ItemContainer, index, child.DesiredSize, layout, window);

				child.Arrange(bounds);
				child.GetVirtualizationInformation().Bounds = bounds;
			}

			return finalSize;
		}

		[NotImplemented]
		internal void ScrollItemintoView(int index, ScrollIntoViewAlignment alignment, double offset, bool forceSynchronous)
		{
		}

		[NotImplemented]
		internal void ScrollItemIntoView(int index, ScrollIntoViewAlignment alignment, double offset, bool forceSynchronous)
		{
		}

		private Size GetViewportSize()
		{
			return new Size(300,300);
		}

		internal Size GetDesiredViewportSize()
		{
			return _layoutStrategy.GetDesiredViewportSize();
		}

		[NotImplemented]
		internal void GetTargetIndexFromNavigationAction(
			int focusedIndex,
			ElementType elementType,
			KeyNavigationAction action,
			object o,
			int i,
			out uint newFocusedIndexUint,
			out ElementType newFocusedType,
			out bool actionValidForSourceIndex)
		{
			newFocusedIndexUint = (uint)focusedIndex;
			newFocusedType = elementType;
			actionValidForSourceIndex = true;
		}

		internal IItemContainerMapping GetItemContainerMapping()
		{
			throw new NotImplementedException();
		}

		internal ILayoutStrategy LayoutStrategy => _layoutStrategy;

		internal double CacheLengthBase { get; set; }

		internal ContainerManager ContainerManager { get; } = new ContainerManager();

		private void SetLayoutStrategyBase(CalendarLayoutStrategy spLayoutStrategy)
		{
			_layoutStrategy = spLayoutStrategy;
			spLayoutStrategy.LayoutDataInfoProvider = this;
		}

		private void CacheFirstVisibleElementBeforeOrientationChange()
		{
		}

		private void ProcessOrientationChange()
		{
		}

		/// <inheritdoc />
		int ILayoutDataInfoProvider.GetTotalItemCount()
			=> ContainerManager.TotalItemsCount;

		/// <inheritdoc />
		int ILayoutDataInfoProvider.GetTotalGroupCount()
			=> ContainerManager.TotalGroupCount;
	}

	internal class ContainerManager
	{
		// Required properties from WinUI code
		public int StartOfContainerVisualSection() => 0;

		public int TotalItemsCount => Host?.Count ?? 0;

		public int TotalGroupCount = 0;

		// Uno only
		public CalendarViewGeneratorHost Host { get; set; }
	}
}
