using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Support.V7.Widget;
using Android.Views;
using Uno.Extensions;
using Uno.Logging;

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

		private int CacheHalfLength => Layout.CacheHalfLengthInViews;

		private Dictionary<UnoViewHolder, List<Action>> _onRecycled = new Dictionary<UnoViewHolder, List<Action>>(); //Used by Phase binding

		//Inclusive
		private int TrailingBufferTargetStart => Math.Max(0, TrailingBufferTargetEnd - CacheHalfLength);
		// Exclusive
		private int TrailingBufferTargetEnd => Layout?.GetFirstVisibleDisplayPosition() ?? -1;
		//Inclusive
		private int LeadingBufferTargetStart => Layout?.GetLastVisibleDisplayPosition() + 1 ?? -1;
		// Exclusive
		private int LeadingBufferTargetEnd => Math.Min(Layout.ItemCount, LeadingBufferTargetStart + CacheHalfLength);

		private int TrailingBufferTargetSize => TrailingBufferTargetEnd - TrailingBufferTargetStart;
		private int LeadingBufferTargetSize => LeadingBufferTargetEnd - LeadingBufferTargetStart;

		private int TrailingBufferStart => _trailingBuffer.Count > 0 ? _trailingBuffer[0].DisplayPosition : -1;
		// Exclusive
		private int TrailingBufferEnd => _trailingBuffer.Count > 0 ? _trailingBuffer[_trailingBuffer.Count - 1].DisplayPosition + 1 : -1;
		private int LeadingBufferStart => _leadingBuffer.Count > 0 ? _leadingBuffer[0].DisplayPosition : -1;
		// Exclusive
		private int LeadingBufferEnd => _leadingBuffer.Count > 0 ? _leadingBuffer[_leadingBuffer.Count - 1].DisplayPosition + 1 : -1;

		private const int IntermediateCacheLimit = 10;

		public int FirstCacheIndex => TrailingBufferStart;
		public int LastCacheIndex => LeadingBufferEnd;

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
				Layout.TryAttachView(record.Value.View);
				return record.Value.View;
			}

			if (record?.IsEmpty ?? false)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
				{
					this.Log().Info($"Found empty record for request for position {position}.");
				}
			}

			// Else get from intermediate cache if possible
			var view = GetViewFromIntermediateCache(recycler, position);
			if (view != null)
			{
				Layout.TryAttachView(view);
			}

			return view;
		}

		/// <summary>
		/// Update buffers by removing out-of-range views and adding views in range.
		/// </summary>
		public void UpdateBuffers(RecyclerView.Recycler recycler)
		{
			UnbufferViews(recycler);
			PrefetchViews(recycler);
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

		/// <summary>
		/// Remove all views not in the target range from the buffer.
		/// </summary>
		private void UnbufferViews(RecyclerView.Recycler recycler)
		{
			TrimEmpty();
			UnbufferTrailing();
			UnbufferLeading();
			TrimEmpty(); //Empty items may have been exposed by unbuffering

			// Prune empty records from the edges of the buffer
			void TrimEmpty()
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
			}

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
					CheckValidState();
					SendToIntermediateCache(recycler, record);
				}

				while (LeadingBufferEnd > LeadingBufferTargetEnd)
				{
					if (_leadingBuffer.Count == 0)
					{
						return;
					}
					var record = _leadingBuffer.RemoveFromBack();
					CheckValidState();
					SendToIntermediateCache(recycler, record);
				}
			}
		}

		/// <summary>
		/// Prefetch views in the target range that aren't yet in the buffer.
		/// </summary>
		private void PrefetchViews(RecyclerView.Recycler recycler)
		{
			PrefetchTrailing();
			PrefetchLeading();

			void PrefetchTrailing()
			{
				if (TrailingBufferTargetSize == 0)
				{
					return;
				}

				// Seed buffer; otherwise succeeding logic fails
				if (_trailingBuffer.Count == 0)
				{
					var record = PrefetchView(recycler, TrailingBufferTargetStart);
					_trailingBuffer.AddToBack(record);
					CheckValidState();
				}

				while (TrailingBufferStart > TrailingBufferTargetStart)
				{
					var record = PrefetchView(recycler, TrailingBufferStart - 1);
					_trailingBuffer.AddToFront(record);
					CheckValidState();
				}

				while (TrailingBufferEnd < TrailingBufferTargetEnd)
				{
					var record = PrefetchView(recycler, TrailingBufferEnd);
					_trailingBuffer.AddToBack(record);
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
					var record = PrefetchView(recycler, LeadingBufferTargetStart);
					_leadingBuffer.AddToBack(record);
					CheckValidState();
				}

				while (LeadingBufferStart > LeadingBufferTargetStart)
				{
					var record = PrefetchView(recycler, LeadingBufferStart - 1);
					_leadingBuffer.AddToFront(record);
					CheckValidState();
				}

				while (LeadingBufferEnd < LeadingBufferTargetEnd)
				{
					var record = PrefetchView(recycler, LeadingBufferEnd);
					_leadingBuffer.AddToBack(record);
					CheckValidState();
				}
			}
		}

		/// <summary>
		/// Prefetch a view for given <paramref name="displayPosition"/>.
		/// </summary>
		private ElementViewRecord PrefetchView(RecyclerView.Recycler recycler, int displayPosition)
		{
			var view = GetViewFromIntermediateCache(recycler, displayPosition);
			if (view == null)
			{
				view = recycler.GetViewForPosition(displayPosition);

				//
				Layout.AddView(view);
				Layout.TryDetachView(view);
			}
			var viewHolder = _owner.GetChildViewHolder(view) as UnoViewHolder;


			return new ElementViewRecord(displayPosition, viewHolder);

		}

		/// <summary>
		/// Retrieve view for <paramref name="displayPosition"/> from intermediate cache.
		/// </summary>
		private View GetViewFromIntermediateCache(RecyclerView.Recycler recycler, int displayPosition)
		{

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
					if (!IsInTargetRange(view.LayoutPosition))
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
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug("Discarding empty record.");
				}
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
			if (displayPosition == TrailingBufferEnd)
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
					Layout.TryDetachView(record.View);
					Layout.RemoveDetachedView(record.View);
				}
			}

			foreach (var record in _leadingBuffer)
			{
				if (record.View != null)
				{
					Layout.TryDetachView(record.View);
					Layout.RemoveDetachedView(record.View);
				}
			}

			foreach (var kvp in _intermediateCache)
			{
				foreach (var holder in kvp.Value)
				{
					Layout.TryDetachView(holder.ItemView);
					Layout.RemoveDetachedView(holder.ItemView);
				}
			}

			_trailingBuffer.Clear();
			_leadingBuffer.Clear();
			_intermediateCache.Clear();
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

		private bool IsInTargetRange(int position)
		{
			return (TrailingBufferTargetStart <= position && TrailingBufferTargetEnd > position) ||
				(LeadingBufferTargetStart <= position && LeadingBufferTargetEnd > position);
		}

		[Conditional("DEBUG")]
		private void CheckValidState()
		{
			if (_trailingBuffer.Count > 0)
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
			if (_leadingBuffer.Count > 0)
			{
				var expectedPosition = LeadingBufferStart;
				for (int i = 0; i < _leadingBuffer.Count; i++)
				{
					var record = _leadingBuffer[i];
					if (expectedPosition != record.DisplayPosition && !record.IsEmpty)
					{
						throw new InvalidOperationException($"Expected position {expectedPosition} but got position {record.DisplayPosition} in trailing buffer.");
					}
					expectedPosition++;
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
