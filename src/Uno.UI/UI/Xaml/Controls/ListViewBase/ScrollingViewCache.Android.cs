using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AndroidX.RecyclerView.Widget;
using Android.Views;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A <see cref="RecyclerView.ViewCacheExtension"/> implementation which temporarily 'detaches' views from the parent rather than removing them completely,
	/// significantly improving performance when scrolling an <see cref="ItemsStackPanel"/> or <see cref="ItemsWrapGrid"/>.
	/// </summary>
	internal class ScrollingViewCache : RecyclerView.ViewCacheExtension
	{
		private readonly Dictionary<int, List<UnoViewHolder>> _cachedViews = new Dictionary<int, List<UnoViewHolder>>();

		private readonly NativeListViewBase _owner;
		private VirtualizingPanelLayout Layout => _owner.NativeLayout as VirtualizingPanelLayout;
		private Dictionary<UnoViewHolder, List<Action>> _onRecycled = new Dictionary<UnoViewHolder, List<Action>>();

		private int MaxCacheSize { get; } = 10;

		public ScrollingViewCache(NativeListViewBase owner)
		{
			_owner = owner;

		}

		public override View GetViewForPositionAndType(RecyclerView.Recycler recycler, int position, int type)
		{
			UnoViewHolder result = null;
			List<UnoViewHolder> views;
			if (!_cachedViews.TryGetValue(type, out views))
			{
				return null;
			}

			//Remove views that are animating out
			views.RemoveAll(IsItemUnrecyclable);

			foreach (var holder in views)
			{
				//Look for an exact match
				if (holder.LayoutPosition == position)
				{
					result = holder;
					views.Remove(result);
					break;
				}
			}
			if (result == null && views.Count > 0)
			{
				//Get any match of correct type
				result = views[views.Count - 1];
				views.RemoveAt(views.Count - 1);
			}

			if (result != null)
			{
				Layout.AttachView(result.ItemView);
				recycler.BindViewToPosition(result.ItemView, position);
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Returning cached view for position={position} and view type={type}. {views.Count} cached views remaining.");
				}
				return result.ItemView;
			}
			return null;
		}

		private static bool IsItemUnrecyclable(UnoViewHolder obj)
		{
			return !obj.IsRecyclable;
		}

		public void DetachAndCacheView(View view, RecyclerView.Recycler recycler)
		{
			var holder = _owner.GetChildViewHolder(view) as UnoViewHolder;
			if (!holder.IsRecyclable)
			{
				//Item is probably animating, we throw it back to the recycler rather than just detaching it to prevent the following error: "java.lang.IllegalArgumentException: Tmp detached view should be removed from RecyclerView before it can be recycled"
				Layout.RemoveAndRecycleView(view, recycler);
				return;
			}

			Layout.DetachView(view);

			NotifyViewRecycled(holder);

			var views = _cachedViews.FindOrCreate(holder.ItemViewType, () => new List<UnoViewHolder>());

			if (views.Count == MaxCacheSize)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Cache already contains {MaxCacheSize} views, sending {view} to recycler.");
				}
				RecycleView(recycler, holder);
				return;
			}
			views.Add(holder);

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Caching view of type {view.GetType().Name}. {views.Count} views cached in total.");
			}
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

		public void EmptyAndRemove()
		{
			foreach (var holder in _cachedViews.Values.SelectMany(l => l))
			{
				Layout.RemoveDetachedView(holder.ItemView);
			}
			_cachedViews.Clear();
		}

		private void RecycleView(RecyclerView.Recycler recycler, UnoViewHolder holder)
		{
			Layout.RemoveDetachedView(holder.ItemView);
			recycler.RecycleView(holder.ItemView);
		}
	}
}
