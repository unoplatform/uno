#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Uno;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.UI;

namespace Windows.UI.Xaml.Controls.Primitives
{
	// This file is aimed to implement methods that should be implemented by the ModernCollectionBasePanel which is not present in Uno

	partial class CalendarPanel : ILayoutDataInfoProvider
	{
		// The CalendarView has a minimum size of 296x350, any size under this one will trigger clipping
		// TODO: Is this size updated according to accessibility font scale factor?
		private static readonly Size _defaultHardCodedSize = new Size(296, 350 - 78); // 78 px for the header etc.

		// Minimum item/cell size to trigger a full measure pass.
		// Below this threshold, we only make sure to insert the first item in the Children collection to allow valid computation of the DetermineTheBiggestItemSize.
		private static readonly Size _minCellSize = new Size(10, 10);

		private class ContainersCache : IItemContainerMapping
		{
			private readonly List<CacheEntry> _entries = new List<CacheEntry>(31 + 7 * 2); // A month + one week before and after
			private CalendarViewGeneratorHost? _host;

			private int _generationStartIndex = -1;
			private int _generationCurrentIndex = -1;
			private int _generationEndIndex = -1;
			private GenerationState _generationState;
			private (int at, int count) _generationRecyclableBefore;
			private (int at, int count) _generationRecyclableAfter;

			internal CalendarViewGeneratorHost? Host
			{
				get => _host;
				set
				{
					_host = value;
					_entries.Clear();
				}
			}

			internal int StartIndex { get; private set; } = -1;

			internal int EndIndex { get; private set; } = -1;

			private bool IsInRange(int itemIndex)
				=> itemIndex >= StartIndex && itemIndex <= EndIndex;

			private int GetEntryIndex(int itemIndex)
				=> itemIndex - StartIndex;

			private enum GenerationState
			{
				Before,
				InRange,
				After
			}

			internal void BeginGeneration(int startIndex, int endIndex)
			{
				if (_host is null)
				{
					throw new InvalidOperationException("Host not set yet");
				}
				//Debug.Assert(_recyclableEntries is null);
				Debug.Assert(_generationStartIndex == -1);
				Debug.Assert(_generationCurrentIndex == -1);
				Debug.Assert(_generationEndIndex == -1);

				//_recyclableEntries = new Queue<CacheEntry>(_entries.Count);

				_generationStartIndex = startIndex;
				_generationCurrentIndex = startIndex;
				_generationEndIndex = endIndex;
				_generationState = GenerationState.Before;

				// Note: Start and End indexes are INCLUSIVE
				startIndex = Math.Max(StartIndex, startIndex);
				endIndex = Math.Min(EndIndex, endIndex);

				if (endIndex < 0)
				{
					return; // Cache is empty
				}

				var startEntryIndex = Math.Min(GetEntryIndex(startIndex), _entries.Count);
				var endEntryIndex = Math.Max(0, GetEntryIndex(endIndex) + 1);

				_generationRecyclableBefore = (0, startEntryIndex);
				_generationRecyclableAfter = (endEntryIndex, Math.Max(0, _entries.Count - endEntryIndex));

				Debug.Assert(
					(_generationRecyclableAfter.at == _entries.Count && _generationRecyclableAfter.count == 0) // Nothing to recycle at the end
					|| (_generationRecyclableAfter.at + _generationRecyclableAfter.count == _entries.Count)); // The last recycle item does exists!
			}

			internal IEnumerable<CacheEntry> CompleteGeneration(int endIndex)
			{
				//Debug.Assert(_recyclableEntries is { });
				Debug.Assert(_generationCurrentIndex - 1 == endIndex); // endIndex is inclusive while _generationCurrentIndex is the next index to use

				// Since the _generationEndIndex is only an estimation, we might have some items that was not flagged as recyclable which were not used.
				var unexpectedUnusedEntries = (at: -1, count: Math.Max(0, _generationEndIndex - endIndex));
				if (unexpectedUnusedEntries.count > 0)
				{
					// If actually all entries have been recycled, some entries might be part of the recyclable head.
					unexpectedUnusedEntries.at = Math.Max(_generationRecyclableBefore.at + _generationRecyclableBefore.count, _generationRecyclableAfter.at - unexpectedUnusedEntries.count);
					unexpectedUnusedEntries.count = Math.Min(unexpectedUnusedEntries.count, _entries.Count - unexpectedUnusedEntries.at);
				}

				var unusedEntriesCount = _generationRecyclableBefore.count
					+ _generationRecyclableAfter.count
					+ unexpectedUnusedEntries.count;

				IEnumerable<CacheEntry> unusedEntries;
				if (unusedEntriesCount > 0)
				{
					var removedEntries = new CacheEntry[unusedEntriesCount];
					var removed = 0;

					// We need to process from the end to the begin in order to not alter indexes:
					// ..Recycled..Recyclable-Head..In-Range..Unexpected-Remaining-Items..Recyclable-Tail..Recycled..

					if (_generationRecyclableAfter.count > 0)
					{
						_entries.CopyTo(_generationRecyclableAfter.at, removedEntries, removed, _generationRecyclableAfter.count);
						_entries.RemoveRange(_generationRecyclableAfter.at, _generationRecyclableAfter.count); //TODO: Move to a second recycling stage instead of throwing them away.

						removed += _generationRecyclableAfter.count;
					}

					if (unexpectedUnusedEntries.count > 0)
					{
						_entries.CopyTo(unexpectedUnusedEntries.at, removedEntries, removed, unexpectedUnusedEntries.count);
						_entries.RemoveRange(unexpectedUnusedEntries.at, unexpectedUnusedEntries.count); //TODO: Move to a second recycling stage instead of throwing them away.

						removed += unexpectedUnusedEntries.count;
					}

					if (_generationRecyclableBefore.count > 0)
					{
						_entries.CopyTo(_generationRecyclableBefore.at, removedEntries, removed, _generationRecyclableBefore.count);
						_entries.RemoveRange(_generationRecyclableBefore.at, _generationRecyclableBefore.count); //TODO: Move to a second recycling stage instead of throwing them away.

						removed += _generationRecyclableBefore.count;
					}

					Debug.Assert(removed == unusedEntriesCount);

					unusedEntries = removedEntries;
				}
				else
				{
					Debug.Assert(unusedEntriesCount == 0);

					unusedEntries = Enumerable.Empty<CacheEntry>();
				}

				_entries.Sort(CacheEntryComparer.Instance);

				StartIndex = _entries[0].Index;
				EndIndex = _entries[_entries.Count - 1].Index;

				Debug.Assert(_generationStartIndex == StartIndex);
				Debug.Assert(endIndex == EndIndex);
				Debug.Assert(StartIndex + _entries.Count - 1 == EndIndex);
				Debug.Assert(_entries.Skip(1).Select((e, i) => _entries[i].Index + 1 == e.Index).AllTrue());

				_generationStartIndex = -1;
				_generationCurrentIndex = -1;
				_generationEndIndex = -1;

				return unusedEntries;
			}

			internal (UIElement container, bool isNew) GetOrCreate(int index)
			{
				Debug.Assert(_host is { });
				Debug.Assert(_generationStartIndex <= index);
				Debug.Assert(_generationCurrentIndex == index);
				// We do not validate Debug.Assert(_generationEndIndex >= index); as the generationEndIndex is only an estimate

				_generationCurrentIndex++;

				switch (_generationState)
				{
					case GenerationState.Before when index >= StartIndex:
						if (index > EndIndex)
						{
							_generationState = GenerationState.After;
							goto after;
						}
						else
						{
							_generationState = GenerationState.InRange;
							goto inRange;
						}
					case GenerationState.InRange when index > EndIndex
						|| GetEntryIndex(index) >= _generationRecyclableAfter.at + _generationRecyclableAfter.count: // Unfortunately we had already recycled that container, we need to create a new one!
						_generationState = GenerationState.After;
						goto after;

					case GenerationState.InRange:
					inRange:
					{
						var entryIndex = GetEntryIndex(index);
						var entry = _entries[entryIndex];

						if (entryIndex == _generationRecyclableAfter.at && _generationRecyclableAfter.count > 0)
						{
							// Finally a container which was eligible for recycling is still valid ... we saved it in extremis!
							_generationRecyclableAfter.at++;
							_generationRecyclableAfter.count--;
						}

						Debug.Assert(entry.Index == index);

						return (entry.Container, false);
					}

					case GenerationState.Before:
					case GenerationState.After:
					after:
					{
						var item = _host![index];

						CacheEntry entry;
						bool isNew;
						if (_generationRecyclableBefore.count > 0)
						{
							entry = _entries[_generationRecyclableBefore.at];
							isNew = false;

							_generationRecyclableBefore.at++;
							_generationRecyclableBefore.count--;
						}
						else if (_generationRecyclableAfter.count > 0)
						{
							entry = _entries[_generationRecyclableAfter.at + _generationRecyclableAfter.count - 1];
							isNew = false;

							_generationRecyclableAfter.count--;

							Debug.Assert(entry.Index > index);
						}
						else
						{
							var container = (UIElement)_host.GetContainerForItem(item, null);
							entry = new CacheEntry(container);
							isNew = true;

							_entries.Add(entry);
						}

						entry.Index = index;
						entry.Item = item;

						_host.PrepareItemContainer(entry.Container, item);

						return (entry.Container, isNew);
					}
				}

				throw new InvalidOperationException("Non reachable case.");
			}

			/// <inheritdoc />
			public object? ItemFromContainer(DependencyObject container)
				=> container is UIElement elt ? _entries.Find(e => e.Container == elt)?.Container : default;

			/// <inheritdoc />
			public DependencyObject? ContainerFromItem(object item)
				=> _entries.Find(e => e.Item == item)?.Container;

			/// <inheritdoc />
			public int IndexFromContainer(DependencyObject container)
				=> container is UIElement elt ? _entries.Find(e => e.Container == elt)?.Index ?? default : default;

			/// <inheritdoc />
			public DependencyObject? ContainerFromIndex(int index)
				=> IsInRange(index) ? _entries[GetEntryIndex(index)].Container : default;
		}

		private class CacheEntry
		{
			public CacheEntry(UIElement container)
			{
				Container = container;
			}

			public UIElement Container { get; }

			public int Index { get; set; }

			public object? Item { get; set; }
		}

		private class CacheEntryComparer : IComparer<CacheEntry>
		{
			public static CacheEntryComparer Instance { get; } = new CacheEntryComparer();
			public int Compare(CacheEntry x, CacheEntry y) => x.Index.CompareTo(y.Index);
		}

		internal event VisibleIndicesUpdatedEventCallback VisibleIndicesUpdated;

		private readonly ContainersCache _cache = new ContainersCache();
		private CalendarLayoutStrategy? _layoutStrategy;
		private CalendarViewGeneratorHost? _host;
		private Rect _effectiveViewport;
		private Rect _lastLayoutedViewport = Rect.Empty;

		private void base_Initialize()
		{
			ContainerManager = new ContainerManager(this);
			VerticalAlignment = VerticalAlignment.Top;
			HorizontalAlignment = HorizontalAlignment.Left;
			EffectiveViewportChanged += OnEffectiveViewportChanged;
		}

		#region Private and internal API required by UWP code
		internal int FirstVisibleIndexBase { get; private set; }
		internal int LastVisibleIndexBase { get; private set; }
		internal int FirstCacheIndexBase { get; private set; }
		internal int LastCacheIndexBase { get; private set; }

		[NotImplemented]
		internal PanelScrollingDirection PanningDirectionBase { get; } = PanelScrollingDirection.None;

		internal ILayoutStrategy? LayoutStrategy => _layoutStrategy;

		internal double CacheLengthBase { get; set; }

		internal ContainerManager ContainerManager { get; private set; }

		internal void RegisterItemsHost(CalendarViewGeneratorHost? pHost)
		{
			_host = pHost;
			_cache.Host = pHost;
			Children.Clear();
			ContainerManager.Host = pHost;
		}

		internal void DisconnectItemsHost()
			=> RegisterItemsHost(null);

		internal DependencyObject? ContainerFromIndex(int index)
			=> _cache.ContainerFromIndex(index);

		internal void ScrollItemIntoView(int index, ScrollIntoViewAlignment alignment, double offset, bool forceSynchronous)
		{
			if (_layoutStrategy is null)
			{
				return;
			}

			_layoutStrategy.EstimateElementBounds(ElementType.ItemContainer, index, default, default, default, out var bounds);

			Owner?.ScrollViewer?.ChangeView(
				horizontalOffset: null,
				verticalOffset: bounds.Y + offset,
				zoomFactor: null);
		}

		private Size GetViewportSize()
		{
			return _lastLayoutedViewport.Size.AtLeast(_defaultHardCodedSize).FiniteOrDefault(_defaultHardCodedSize);
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
		#endregion

		#region Panel / base class (i.e. ModernCollectionBasePanel) implementation (Measure/Arrange)
		private Size base_MeasureOverride(Size availableSize)
		{
			if (_host is null || _layoutStrategy is null)
			{
				return default;
			}

			_layoutStrategy.BeginMeasure();
			ShouldInterceptInvalidate = true;
			var index = -1;
			try
			{
				var size = _effectiveViewport.Size.AtLeast(availableSize).AtLeast(_defaultHardCodedSize).FiniteOrDefault(_defaultHardCodedSize);
				var position = _effectiveViewport.Location.FiniteOrDefault(default);
				var viewport = new Rect(position, size);

				// Gets the index of the first element to render and the actual viewport to use
				_layoutStrategy.EstimateElementIndex(ElementType.ItemContainer, default, default, viewport, out var renderWindow, out var startIndex);
				renderWindow.Size = viewport.Size; // The actualViewport contains only position information

				var expectedItemsCount = LastVisibleIndex - FirstVisibleIndex;
				_cache.BeginGeneration(startIndex, startIndex + expectedItemsCount);

				index = startIndex;
				int firstVisibleIndex = -1, lastVisibleIndex = -1;
				var count = _host.Count;
				var layout = new LayoutReference { RelativeLocation = ReferenceIdentity.Myself };
				var remainingWindowToFill = renderWindow;

				while (
					index < count
					&&
						// _layoutStrategy.ShouldContinueFillingUpSpace only considers items on a single line, 
						// so we enumerate until we reach the bottom of the viewport,
						(layout.ReferenceBounds.Bottom < renderWindow.Bottom
						// then we ask to the _layoutStrategy to get items to fill the last line
						|| _layoutStrategy.ShouldContinueFillingUpSpace(ElementType.ItemContainer, index, layout, remainingWindowToFill))
					)
				{
					var (container, isNew) = _cache.GetOrCreate(index);
					if (isNew)
					{
						Children.Add(container);
					}

					var itemSize = _layoutStrategy.GetElementMeasureSize(ElementType.ItemContainer, index, renderWindow); // Note: It's actually the same for all items
					var itemBounds = _layoutStrategy.GetElementBounds(ElementType.ItemContainer, index, itemSize, layout, renderWindow);

					if (itemSize.Width < _minCellSize.Width && itemSize.Height < _minCellSize.Height)
					{
						// We don't have any valid cell size yet (This measure pass has been caused by DetermineTheBiggestItemSize),
						// so we stop right after having inserted the first child in the Children collection.
						index++;
						return default;
					}

					container.Measure(itemSize);
					container.GetVirtualizationInformation().MeasureSize = itemSize;

					var isVisible = viewport.Contains(itemBounds.Location);
					if (firstVisibleIndex == -1 && isVisible)
					{
						firstVisibleIndex = index;
						lastVisibleIndex = index;
					}
					else if (isVisible)
					{
						lastVisibleIndex = index;
					}

					layout.RelativeLocation = ReferenceIdentity.AfterMe;
					layout.ReferenceBounds = itemBounds;
					remainingWindowToFill.Y = Math.Min(renderWindow.Bottom, itemBounds.Y);
					remainingWindowToFill.Height = Math.Max(0, renderWindow.Bottom - itemBounds.Bottom);

					index++;
				}
				
				StartIndex = 0;
				FirstVisibleIndexBase = Math.Max(firstVisibleIndex, startIndex);
				LastVisibleIndexBase = Math.Max(FirstVisibleIndexBase, lastVisibleIndex);
				_lastLayoutedViewport = viewport;
			}
			finally
			{
				foreach (var unusedEntry in _cache.CompleteGeneration(index - 1))
				{
					Children.Remove(unusedEntry.Container);
				}

				Debug.Assert(_cache.StartIndex <= FirstVisibleIndex);
				Debug.Assert(_cache.EndIndex >= LastVisibleIndex);

				ShouldInterceptInvalidate = false;
				_layoutStrategy.EndMeasure();
			}
			VisibleIndicesUpdated?.Invoke(this, null);

			_layoutStrategy.EstimatePanelExtent(
				default /* not used by CalendarLayoutStrategyImpl */,
				default /* not used by CalendarLayoutStrategyImpl */,
				default /* not used by CalendarLayoutStrategyImpl */,
				out var desiredSize);

			return desiredSize;
		}

		private Size base_ArrangeOverride(Size finalSize)
		{
			if (_host is null || _layoutStrategy is null)
			{
				return default;
			}

			var layout = new LayoutReference(); // Empty layout which will actually drive the ShouldContinueFillingUpSpace to always return true
			var window = new Rect(default, finalSize);

			foreach (var child in Children)
			{
				var index = _cache.IndexFromContainer(child);
				var bounds = _layoutStrategy.GetElementBounds(ElementType.ItemContainer, index, child.DesiredSize, layout, window);

				//TODO _layoutStrategy.GetElementArrangeBounds()

				child.Arrange(bounds);
				child.GetVirtualizationInformation().Bounds = bounds;
			}

			return finalSize;
		}
		#endregion

		private static void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
		{
			if (sender is CalendarPanel that)
			{
				that._effectiveViewport = args.EffectiveViewport;

				if (that._host is null || that._layoutStrategy is null)
				{
					return;
				}

				if (Math.Abs(that._effectiveViewport.Y - that._lastLayoutedViewport.Y) > 100)
				{
					that.InvalidateMeasure();
				}
			}
		}
	}

	internal class ContainerManager
	{
		// Required properties from WinUI code
		public int StartOfContainerVisualSection() => _owner.FirstVisibleIndex;

		public int TotalItemsCount => Host?.Count ?? 0;

		public int TotalGroupCount = 0;

		// Uno only
		private readonly CalendarPanel _owner;

		public CalendarViewGeneratorHost? Host { get; set; }

		public ContainerManager(CalendarPanel owner)
		{
			_owner = owner;
		}
	}
}
