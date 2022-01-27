using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AndroidX.RecyclerView.Widget;
using Android.Views;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Cache implementation that maintains a buffer of views to either side of the visible viewport.
	/// </summary>
	internal class BufferViewCache : RecyclerView.ViewCacheExtension
	{
		/// <summary>
		/// Buffer on the offset-zero side of the visible viewport.
		/// </summary>
		private readonly Deque<ElementViewRecord> _trailingBuffer = new Deque<ElementViewRecord>();
		/// <summary>
		/// Buffer on the offset-max side of the visible viewport.
		/// </summary>
		private readonly Deque<ElementViewRecord> _leadingBuffer = new Deque<ElementViewRecord>();
		/// <summary>
		/// 'Overflow' cache of views not associated with either buffer.
		/// </summary>
		private readonly Dictionary<int, List<UnoViewHolder>> _intermediateCache = new Dictionary<int, List<UnoViewHolder>>();
		private readonly NativeListViewBase _owner;
		private VirtualizingPanelLayout Layout => _owner.NativeLayout as VirtualizingPanelLayout;
		private int NumberOfItems => _owner.XamlParent.NumberOfItems;

		private int CacheHalfLength => Layout.CacheHalfLengthInViews;

		private Dictionary<UnoViewHolder, List<Action>> _onRecycled = new Dictionary<UnoViewHolder, List<Action>>(); //Used by Phase binding

		private int? _reorderingItem;

		//Inclusive
		private int TrailingBufferTargetStart => Math.Max(ConvertIndexToDisplayPosition(0), TrailingBufferTargetEnd - CacheHalfLength);
		// Exclusive
		private int TrailingBufferTargetEnd => Layout?.GetFirstVisibleDisplayPosition() ?? -1;
		//Inclusive
		private int LeadingBufferTargetStart => Layout?.GetLastVisibleDisplayPosition() + 1 ?? -1;
		// Exclusive
		private int LeadingBufferTargetEnd => Math.Min(ConvertIndexToDisplayPosition(NumberOfItems), LeadingBufferTargetStart + CacheHalfLength);

		private int TrailingBufferTargetSize => Math.Max(0, TrailingBufferTargetEnd - TrailingBufferTargetStart);
		private int LeadingBufferTargetSize => Math.Max(0, LeadingBufferTargetEnd - LeadingBufferTargetStart);

		private int TrailingBufferStart => _trailingBuffer.Count > 0 ? _trailingBuffer[0].DisplayPosition : -1;
		// Exclusive
		private int TrailingBufferEnd => _trailingBuffer.Count > 0 ? _trailingBuffer[_trailingBuffer.Count - 1].DisplayPosition + 1 : -1;
		private int LeadingBufferStart => _leadingBuffer.Count > 0 ? _leadingBuffer[0].DisplayPosition : -1;
		// Exclusive
		private int LeadingBufferEnd => _leadingBuffer.Count > 0 ? _leadingBuffer[_leadingBuffer.Count - 1].DisplayPosition + 1 : -1;

		private bool _isInitiallyPopulated;
		/// <summary>
		/// Don't return views from the intermediate cache if true: used when the specific intent is to populate the cache.
		/// </summary>
		private bool _shouldBlockIntermediateCache;

		private int IntermediateCacheLimit
		{
			get
			{
				const int DefaultIntermediateCacheLimit = 10;
				return Math.Max(DefaultIntermediateCacheLimit, UnpopulatedCacheSize());
				int UnpopulatedCacheSize() => CacheHalfLength * 2 - _trailingBuffer.Count - _leadingBuffer.Count + 1;
			}
		}

		public int FirstCacheIndex => TrailingBufferStart;
		public int LastCacheIndex => LeadingBufferEnd;

		// Used for debugging
		private int _initialChildCount;
		private int _initialItemViewCount;

		internal IEnumerable<SelectorItem> CachedItemViews =>
			_trailingBuffer
				.Concat(_leadingBuffer)
				.Select(vr => vr.View)
				.Concat(_intermediateCache
					.Values
					.SelectMany(l => l.Select(vh => vh.ItemView))
				)
				.OfType<SelectorItem>();


		public BufferViewCache(NativeListViewBase owner)
		{
			_owner = owner;
		}

		public override View GetViewForPositionAndType(RecyclerView.Recycler recycler, int position, int type)
		{
			// Try to get view from buffer
			var record = DequeueRecordFromBuffer(position);
			if (record?.View != null)
			{
				if (record.Value.ItemViewType == type)
				{
					Layout.TryAttachView(record.Value.View);
					Layout.UpdateSelection(record.Value.View);
					return record.Value.View;
				}
				else
				{
					// View is no longer valid for this position, but still potentially reusable.
					SendToIntermediateCache(recycler, record.Value);
				}
			}

			if (record?.IsEmpty ?? false)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Information))
				{
					this.Log().Info($"Found empty record for request for position {position}.");
				}
			}

			// Else get from intermediate cache if possible
			var view = GetViewFromIntermediateCache(recycler, position);

			if (view != null)
			{
				Layout.TryAttachView(view);
				Layout.UpdateSelection(view);
			}

			return view;
		}

		/// <summary>
		/// Update buffers by removing out-of-range views and adding views in range.
		/// </summary>
		public void UpdateBuffers(RecyclerView.Recycler recycler, RecyclerView.State state)
		{
			_initialChildCount = Layout.ChildCount;
			_initialItemViewCount = Layout.ItemViewCount;
			UnbufferViews(recycler);
			PrefetchViews(recycler, state);
			TrimIntermediateCache(recycler);
		}

		/// <summary>
		/// Detach view that was visible and send it to the cache.
		/// </summary>
		public void DetachAndCacheView(View view, RecyclerView.Recycler recycler)
		{
			var holder = _owner.GetChildViewHolder(view) as UnoViewHolder;
			var record = new ElementViewRecord(holder.LayoutPosition, holder);
			Layout.TryDetachView(view);
			SendToIntermediateCache(recycler, record);
		}

		public void SetReorderingItem(int itemIndex) => _reorderingItem = itemIndex;

		public void RemoveReorderingItem() => _reorderingItem = null;

		/// <summary>
		/// Remove all views not in the target range from the buffer.
		/// </summary>
		private void UnbufferViews(RecyclerView.Recycler recycler)
		{
			TrimEmpty();
			UnbufferTrailing();
			UnbufferLeading();
			TrimEmpty(); //Empty items may have been exposed by unbuffering
			CheckValidSteadyState();

			void UnbufferTrailing()
			{
				if (TrailingBufferTargetSize == 0)
				{
					while (_trailingBuffer.Count > 0)
					{
						var record = _trailingBuffer.RemoveFromBack();
						CheckValidState();
						SendToIntermediateCache(recycler, record);
					}
					return;
				}

				while (TrailingBufferStart < TrailingBufferTargetStart)
				{
					if (_trailingBuffer.Count == 0)
					{
						return;
					}
					var record = _trailingBuffer.RemoveFromFront();
					TrimEmpty();
					CheckValidState();
					SendToIntermediateCache(recycler, record);
				}


				while (TrailingBufferEnd > TrailingBufferTargetEnd)
				{
					if (_trailingBuffer.Count == 0)
					{
						return;
					}
					var record = _trailingBuffer.RemoveFromBack();
					TrimEmpty();
					CheckValidState();
					SendToIntermediateCache(recycler, record);
				}
			}

			void UnbufferLeading()
			{
				if (LeadingBufferTargetSize == 0)
				{
					while (_leadingBuffer.Count > 0)
					{
						var record = _leadingBuffer.RemoveFromBack();
						CheckValidState();
						SendToIntermediateCache(recycler, record);
					}
					return;
				}

				while (LeadingBufferStart < LeadingBufferTargetStart)
				{
					if (_leadingBuffer.Count == 0)
					{
						return;
					}
					var record = _leadingBuffer.RemoveFromFront();
					TrimEmpty();
					CheckValidState();
					CheckValidSteadyState();
					SendToIntermediateCache(recycler, record);
				}

				while (LeadingBufferEnd > LeadingBufferTargetEnd)
				{
					if (_leadingBuffer.Count == 0)
					{
						return;
					}
					var record = _leadingBuffer.RemoveFromBack();
					TrimEmpty();
					CheckValidState();
					CheckValidSteadyState();
					SendToIntermediateCache(recycler, record);
				}
			}
		}

		/// <summary>
		/// Prune empty records from the edges of the buffer (as subsequent retrieval logic expects).
		/// </summary>
		private void TrimEmpty()
		{
			while (_trailingBuffer.Count > 0 && _trailingBuffer[0].IsEmpty)
			{
				_trailingBuffer.RemoveFromFront();
				CheckValidState();
			}

			while (_trailingBuffer.Count > 0 && _trailingBuffer[_trailingBuffer.Count - 1].IsEmpty)
			{
				_trailingBuffer.RemoveFromBack();
				CheckValidState();
			}

			while (_leadingBuffer.Count > 0 && _leadingBuffer[0].IsEmpty)
			{
				_leadingBuffer.RemoveFromFront();
				CheckValidState();
			}

			while (_leadingBuffer.Count > 0 && _leadingBuffer[_leadingBuffer.Count - 1].IsEmpty)
			{
				_leadingBuffer.RemoveFromBack();
				CheckValidState();
			}
			CheckValidSteadyState();
		}

		/// <summary>
		/// Prefetch views in the target range that aren't yet in the buffer.
		/// </summary>
		private void PrefetchViews(RecyclerView.Recycler recycler, RecyclerView.State state)
		{
			if (Layout.ItemCount == 0 || CacheHalfLength == 0)
			{
				return;
			}
			PrefetchTrailing();
			PrefetchLeading();
			if (!_isInitiallyPopulated
				// The leading buffer may be empty if the previous prefetch ended early, eg because it found an unrecyclable item
				&& LeadingBufferEnd > -1)
			{
				PrefetchExtra();
				_isInitiallyPopulated = true;
			}

			CheckValidSteadyState();

			void PrefetchTrailing()
			{
				if (TrailingBufferTargetSize == 0)
				{
					return;
				}

				// Seed buffer; otherwise succeeding logic fails
				if (_trailingBuffer.Count == 0)
				{
					if (PrefetchView(recycler, state, TrailingBufferTargetStart) is { } record)
					{
						_trailingBuffer.AddToBack(record);
					}
					else
					{
						// List is animating, etc, so forgo populating the buffer at this time
						return;
					}
					CheckValidState();
				}

				while (TrailingBufferStart > TrailingBufferTargetStart)
				{
					if (PrefetchView(recycler, state, TrailingBufferStart - 1) is { } record)
					{
						_trailingBuffer.AddToFront(record);
					}
					else
					{
						// List is animating, etc, so forgo populating the buffer at this time
						return;
					}
					CheckValidState();
				}

				while (TrailingBufferEnd < TrailingBufferTargetEnd)
				{
					if (PrefetchView(recycler, state, TrailingBufferEnd) is { } record)
					{
						_trailingBuffer.AddToBack(record);
					}
					else
					{
						// List is animating, etc, so forgo populating the buffer at this time
						return;
					}
					CheckValidState();
				}
			}

			void PrefetchLeading()
			{
				if (LeadingBufferTargetSize == 0)
				{
					return;
				}

				// Seed buffer
				if (_leadingBuffer.Count == 0)
				{
					if (PrefetchView(recycler, state, LeadingBufferTargetStart) is { } record)
					{
						_leadingBuffer.AddToBack(record);
					}
					else
					{
						// List is animating, etc, so forgo populating the buffer at this time
						return;
					}
					CheckValidState();
				}

				while (LeadingBufferStart > LeadingBufferTargetStart)
				{
					if (PrefetchView(recycler, state, LeadingBufferStart - 1) is { } record)
					{
						_leadingBuffer.AddToFront(record);
					}
					else
					{
						// List is animating, etc, so forgo populating the buffer at this time
						return;
					}
					CheckValidState();
				}

				while (LeadingBufferEnd < LeadingBufferTargetEnd)
				{
					if (PrefetchView(recycler, state, LeadingBufferEnd) is { } record)
					{
						_leadingBuffer.AddToBack(record);
					}
					else
					{
						// List is animating, etc, so forgo populating the buffer at this time
						return;
					}
					CheckValidState();
				}
			}

			// Initially pre-cache a half-width of items directly to the intermediate cache. This is an optimization so that new views 
			// aren't created to fill the trailing buffer when the user first scrolls.
			void PrefetchExtra()
			{
				if (TrailingBufferTargetSize > 0)
				{
					// Only want to perform this step when scroll position is at start of list
					return;
				}

				if (LeadingBufferTargetSize == 0)
				{
					// No need for extra items
					return;
				}
				var targetEnd = Math.Min(NumberOfItems, LeadingBufferEnd + CacheHalfLength + 1);
				try
				{
					_shouldBlockIntermediateCache = true;
					for (int i = LeadingBufferEnd; i < targetEnd; i++)
					{
						if (PrefetchView(recycler, state, i) is { } record)
						{
							SendToIntermediateCache(recycler, record);
						}
						else
						{
							// List is animating, etc, so forgo populating the buffer at this time
							return;
						}
					}
				}
				finally
				{
					_shouldBlockIntermediateCache = false;
				}
			}
		}

		/// <summary>
		/// Prefetch a view for given <paramref name="displayPosition"/>.
		/// </summary>
		/// <returns>
		/// A record of the position and associated view holder, or null if the item returned by the recycler is not in a recyclable state.
		/// </returns>
		private ElementViewRecord? PrefetchView(RecyclerView.Recycler recycler, RecyclerView.State state, int displayPosition)
		{
			if (displayPosition < 0)
			{
				throw new ArgumentException($"{nameof(displayPosition)} must be greater than 0.");
			}
			var view = GetViewFromIntermediateCache(recycler, displayPosition);
			var viewHolder = default(UnoViewHolder);
			if (view == null)
			{
				view = recycler.GetViewForPosition(displayPosition, state);

				viewHolder = _owner.GetChildViewHolder(view);

				if (!viewHolder.IsRecyclable)
				{
					// This typically means that the item is being animated. In this case we shouldn't stash it away for future use.

					// Return the view to the recycler, otherwise it may subsequently cause an error
					recycler.RecycleView(view);

					return null;
				}

				// Add->Detach allows view to be efficiently re-displayed
				Layout.AddView(view);
				Layout.TryDetachView(view);
			}
			viewHolder ??= _owner.GetChildViewHolder(view);

			if (!(view is SelectorItem))
			{
				throw new InvalidOperationException($"{nameof(PrefetchView)} received {view?.GetType()} in place of {nameof(SelectorItem)}.");
			}

			return new ElementViewRecord(displayPosition, viewHolder);

		}

		/// <summary>
		/// Retrieve view for <paramref name="displayPosition"/> from intermediate cache.
		/// </summary>
		private View GetViewFromIntermediateCache(RecyclerView.Recycler recycler, int displayPosition)
		{
			if (_shouldBlockIntermediateCache)
			{
				return null;
			}
			UnoViewHolder result = null;
			List<UnoViewHolder> views;
			var type = _owner.CurrentAdapter.GetItemViewType(displayPosition);
			if (!_intermediateCache.TryGetValue(type, out views))
			{
				return null;
			}

			//Remove views that are animating out
			views.RemoveAll(RemoveUnrecyclable);

			foreach (var holder in views)
			{
				//Look for an exact match
				if (holder.LayoutPosition == displayPosition)
				{
					result = holder;
					views.Remove(result);
					_owner.CurrentAdapter.RegisterPhaseBinding(result); //Restart phase binding, since this won't be rebound by adapter
					break;
				}
			}
			if (result == null && views.Count > 0)
			{
				// Get any match of correct type except views that could be reused without rebinding
				for (int i = views.Count - 1; i >= 0; i--)
				{
					var view = views[i];
					if (!IsImmediatelyReusable(view.LayoutPosition))
					{
						result = view;
						views.RemoveAt(i);
						break;
					}
				}
			}

			if (result != null)
			{
				recycler.BindViewToPosition(result.ItemView, displayPosition);
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Returning cached view for position={displayPosition} and view type={type}. {views.Count} cached views remaining.");
				}
				return result.ItemView;
			}
			return null;

			bool RemoveUnrecyclable(UnoViewHolder holderInner)
			{
				var isUnrecyclable = !holderInner.IsRecyclable;
				if (isUnrecyclable)
				{
					Layout.RemoveAndRecycleView(holderInner.ItemView, recycler);
				}
				return isUnrecyclable;
			}
		}

		/// <summary>
		/// Send view that was unbuffered or scrolled out of view to the intermediate cache.
		/// </summary>
		private void SendToIntermediateCache(RecyclerView.Recycler recycler, ElementViewRecord viewRecord)
		{
			if (viewRecord.IsEmpty)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("Discarding empty record.");
				}
				return;
			}

			var isGenerated = (viewRecord.View as ContentControl)?.IsGeneratedContainer ?? false;
			if (!isGenerated)
			{
				// If it's not a generated container then it must be an item that returned true for IsItemItsOwnContainerOverride (eg an
				// explicitly-defined ListViewItem), and shouldn't be recycled for a different item.
				Layout.RemoveView(viewRecord.View);
				return;
			}

			NotifyViewRecycled(viewRecord.ViewHolder);

			// Send to intermediate cache
			var views = _intermediateCache.FindOrCreate(viewRecord.ViewHolder.ItemViewType, () => new List<UnoViewHolder>());
			if (!viewRecord.ViewHolder.IsRecyclable)
			{
				//Item is probably animating, we throw it back to the recycler rather than just detaching it to prevent the following error: "java.lang.IllegalArgumentException: Tmp detached view should be removed from RecyclerView before it can be recycled"
				Layout.RemoveAndRecycleView(viewRecord.View, recycler);
				return;
			}

			Layout.TryDetachView(viewRecord.View);

			views.Add(viewRecord.ViewHolder);
		}

		/// <summary>
		/// Reduce intermediate cache down to its size limit.
		/// </summary>
		private void TrimIntermediateCache(RecyclerView.Recycler recycler)
		{
			foreach (var kvp in _intermediateCache)
			{
				var views = kvp.Value;
				if (views.Count > IntermediateCacheLimit)
				{
					views.RemoveRange(IntermediateCacheLimit, views.Count - IntermediateCacheLimit);
				}
			}
		}

		/// <summary>
		/// Retrieve view record from buffer if available.
		/// </summary>
		private ElementViewRecord? DequeueRecordFromBuffer(int displayPosition)
		{
			CheckValidSteadyState();
			_initialChildCount = Layout.ChildCount;
			_initialItemViewCount = Layout.ItemViewCount;
			try
			{
				if (displayPosition == TrailingBufferEnd - 1)
				{
					return _trailingBuffer.RemoveFromBack();
				}

				if (displayPosition == LeadingBufferStart)
				{
					return _leadingBuffer.RemoveFromFront();
				}

				if (displayPosition >= TrailingBufferStart && displayPosition < TrailingBufferEnd)
				{
					var index = displayPosition - TrailingBufferStart;
					try
					{
						return _trailingBuffer[index];
					}
					finally
					{
						_trailingBuffer[index] = ElementViewRecord.Empty;
					}
				}

				if ((displayPosition >= LeadingBufferStart && displayPosition < LeadingBufferEnd))
				{
					var index = displayPosition - LeadingBufferStart;
					try
					{
						return _leadingBuffer[index];
					}
					finally
					{
						_leadingBuffer[index] = ElementViewRecord.Empty;
					}
				}
			}
			finally
			{
				TrimEmpty();
				CheckValidSteadyState();
			}

			return null;
		}

		/// <summary>
		/// Remove all views from the window and clear all local caches
		/// </summary>
		internal void EmptyAndRemove()
		{
			foreach (var record in _trailingBuffer)
			{
				if (record.View != null)
				{
					CleanUpView(record.View);
				}
			}

			foreach (var record in _leadingBuffer)
			{
				if (record.View != null)
				{
					CleanUpView(record.View);
				}
			}

			foreach (var kvp in _intermediateCache)
			{
				foreach (var holder in kvp.Value)
				{
					CleanUpView(holder.ItemView);
				}
			}

			_trailingBuffer.Clear();
			_leadingBuffer.Clear();
			_intermediateCache.Clear();
			_isInitiallyPopulated = false;

			void CleanUpView(View viewToClean)
			{
				if ((viewToClean as FrameworkElement).Parent != null)
				{
					Layout.TryDetachView(viewToClean);
					Layout.RemoveDetachedView(viewToClean);
				}
				else
				{
					// If Parent is null, the cached view was probably already removed during unloading, and detaching it is unnecessary (and indeed illegal).
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"Skipping detach of view {viewToClean.GetHashCode()}");
					}
				}
				_owner.XamlParent.CleanUpContainer(viewToClean as ContentControl);
			}
		}

		/// <summary>
		/// Remove detached views completely when list is unloaded. This prevents errors where the view's attached window info isn't 
		/// properly updated. (Which is very bad.)
		/// </summary>
		internal void OnUnloaded()
		{
			foreach (var record in _trailingBuffer)
			{
				UnloadView(record.ViewHolder);
			}

			foreach (var record in _leadingBuffer)
			{
				UnloadView(record.ViewHolder);
			}

			foreach (var holder in _intermediateCache.Values.SelectMany(l => l))
			{
				UnloadView(holder);
			}

			void UnloadView(UnoViewHolder holder)
			{
				if (holder?.ItemView == null)
				{
					return;
				}
				if (!holder.IsDetached)
				{
					return;
				}

				Layout.TryAttachView(holder.ItemView);
				Layout.RemoveView(holder.ItemView);
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Removed cached view {holder.ItemView.GetHashCode()}");
				}
			}
		}

		internal void OnLoaded()
		{

		}

		private void NotifyViewRecycled(UnoViewHolder holder)
		{
			if (_onRecycled.TryGetValue(holder, out var actions))
			{
				actions.ForEach(a => a());
				_onRecycled.Remove(holder);
			}
		}

		/// <summary>
		/// Queues an action to be executed when the provided viewHolder is being recycled.
		/// </summary>
		internal void OnRecycled(UnoViewHolder unoViewHolder, Action action)
		{
			if (!_onRecycled.TryGetValue(unoViewHolder, out var actions))
			{
				_onRecycled[unoViewHolder] = actions = new List<Action>();
			}

			actions.Add(action);
		}

		private bool IsImmediatelyReusable(int position)
		{
			if (position == _reorderingItem)
			{
				// View being reordered shouldn't be reused for other items
				return true;
			}

			// View is in the buffer target range, may be reusable
			return (TrailingBufferTargetStart <= position && TrailingBufferTargetEnd > position) ||
				(LeadingBufferTargetStart <= position && LeadingBufferTargetEnd > position);
		}

		private int ConvertIndexToDisplayPosition(int index) => _owner.XamlParent.ConvertIndexToDisplayPosition(index);

		/// <summary>
		/// Check that the buffers are in a consistent state. This is safe to call during certain intermediate operations.
		/// </summary>
		[Conditional("DEBUG")]
		private void CheckValidState()
		{
			if (_trailingBuffer.Count > 0 && !_trailingBuffer[0].IsEmpty)
			{
				var expectedPosition = TrailingBufferStart;
				for (int i = 0; i < _trailingBuffer.Count; i++)
				{
					var record = _trailingBuffer[i];
					if (expectedPosition != record.DisplayPosition && !record.IsEmpty)
					{
						throw new InvalidOperationException($"Expected position {expectedPosition} but got position {record.DisplayPosition} in trailing buffer.");
					}
					expectedPosition++;
				}
			}
			if (_leadingBuffer.Count > 0 && !_leadingBuffer[0].IsEmpty)
			{
				var expectedPosition = LeadingBufferStart;
				for (int i = 0; i < _leadingBuffer.Count; i++)
				{
					var record = _leadingBuffer[i];
					if (expectedPosition != record.DisplayPosition && !record.IsEmpty)
					{
						throw new InvalidOperationException($"Expected position {expectedPosition} but got position {record.DisplayPosition} in leading buffer.");
					}
					expectedPosition++;
				}
			}

			if (Layout.ChildCount != _initialChildCount)
			{
				throw new InvalidOperationException($"Owner ChildCount has changed from {_initialChildCount} to {Layout.ChildCount}. Cache update should not modify owner.");
			}
			if (Layout.ItemViewCount != _initialItemViewCount)
			{
				throw new InvalidOperationException($"Owner ItemViewCount has changed from {_initialItemViewCount} to {Layout.ItemViewCount}. Cache update should not modify owner.");
			}
		}

		/// <summary>
		/// Check that the buffers are in a consistent state. This should only be called after intermediate operations have completed.
		/// </summary>
		[Conditional("DEBUG")]
		private void CheckValidSteadyState()
		{
			if (_leadingBuffer.Count > 0)
			{
				if (_leadingBuffer[0].IsEmpty)
				{
					throw new InvalidOperationException();
				}
				if (_leadingBuffer.Last().IsEmpty)
				{
					throw new InvalidOperationException();
				}
			}

			if (_trailingBuffer.Count > 0)
			{
				if (_trailingBuffer[0].IsEmpty)
				{
					throw new InvalidOperationException();
				}
				if (_trailingBuffer.Last().IsEmpty)
				{
					throw new InvalidOperationException();
				}
			}
		}

		private struct ElementViewRecord
		{
			public static ElementViewRecord Empty => new ElementViewRecord(isEmpty: true);
			public ElementViewRecord(int displayPosition, UnoViewHolder viewHolder)
			{
				DisplayPosition = displayPosition;
				ViewHolder = viewHolder;
				IsEmpty = false;
			}

			private ElementViewRecord(bool isEmpty)
			{
				DisplayPosition = -1;
				ViewHolder = null;
				IsEmpty = isEmpty;
			}

			public int DisplayPosition { get; }
			public UnoViewHolder ViewHolder { get; }
			public View View => ViewHolder?.ItemView;
			public int ItemViewType => ViewHolder?.ItemViewType ?? int.MinValue;

			public bool IsEmpty { get; }

			public override string ToString()
			{
				if (IsEmpty)
				{
					return "(Empty)";
				}
				else
				{
					return $"({View} {ViewHolder}";
				}
			}
		}
	}
}
