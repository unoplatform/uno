using Android.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Android.Widget;
using System.Linq;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Uno.UI.Controls
{
	/// <summary>
	/// A secondary pool for the AbsListView inheriting lists, to allow reuse 
	/// of views even if the control gets reloaded.
	/// </summary>

	internal class AbsListViewSecondaryPool : Java.Lang.Object, ISecondaryViewPool, AbsListView.IRecyclerListener
	{
		private readonly Func<int, int> _itemTypeSelector;
		private readonly HashSet<View>[] _pools;
		private readonly HashSet<View> _recycler = new HashSet<View>();
		private Dictionary<View, List<Action>> _onRecycled = new Dictionary<View, List<Action>>();

		public AbsListViewSecondaryPool(Func<int, int> itemTypeSelector, int viewTypeCount)
		{
			if (viewTypeCount < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(viewTypeCount));
			}

			_itemTypeSelector = itemTypeSelector;

			// Increase the available templates by one, as zero may 
			// be used as a null type.
			_pools = new HashSet<View>[viewTypeCount + 1];

			for (int i = 0; i < _pools.Length; i++)
			{
				_pools[i] = new HashSet<View>();
			}
		}

		/// <summary>
		/// Queues an action to be executed when the provided viewHolder is being recycled.
		/// </summary>
		public void RegisterForRecycled(View container, Action action)
		{
			if (!_onRecycled.TryGetValue(container, out var actions))
			{
				_onRecycled[container] = actions = new List<Action>();
			}

			actions.Add(action);
		}

		View ISecondaryViewPool.GetView(int position)
		{
			int itemType = GetItemType(position);

			var availableView = _pools[itemType]
				.Except(_recycler)
				.Where(v => !v.HasParent())
				.FirstOrDefault();

			if (availableView != null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Reusing unassigned view {0}", availableView);
				}
			}

			return availableView;
		}

		private int GetItemType(int position)
		{
			var itemType = _itemTypeSelector(position);

			if (itemType < 0 || itemType >= _pools.Length)
			{
				throw new InvalidOperationException($"The itemType {itemType} is invalid");
			}

			return itemType;
		}

		void AbsListView.IRecyclerListener.OnMovedToScrapHeap(View view)
		{
			_recycler.Add(view);

			if (_onRecycled.TryGetValue(view, out var actions))
			{
				actions.ForEach(a => a());
				_onRecycled.Remove(view);
			}
		}

		void ISecondaryViewPool.SetActiveView(int position, View view)
		{
			_recycler.Remove(view);

			_pools[GetItemType(position)].Add(view);
		}

		/// <summary>
		/// To be called from ViewGroup.RemoveDetachedView, which is
		/// called by the internal RecycleBin.
		/// </summary>
		/// <param name="view"></param>
		public void RemoveFromRecycler(View view)
		{
			_recycler.Remove(view);
		}

		public View[] GetAllViews()
		{
			return _pools.SelectMany(s => s.Select(v => v)).ToArray();
		}
	}
}
