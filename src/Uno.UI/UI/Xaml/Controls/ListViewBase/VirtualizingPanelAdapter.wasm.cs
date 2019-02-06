using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Handles creation, recycling and binding of items for an <see cref="IVirtualizingPanel"/>.
	/// </summary>
	internal class VirtualizingPanelGenerator
	{
		private const int CacheLimit = 10;
		private const int NoTemplateItemId = -1;
		private readonly VirtualizingPanelLayout _owner;

		private readonly Dictionary<int, Stack<FrameworkElement>> _itemContainerCache = new Dictionary<int, Stack<FrameworkElement>>();

		private ItemsControl ItemsControl => _owner.ItemsControl;

		public VirtualizingPanelGenerator(VirtualizingPanelLayout owner)
		{
			_owner = owner;
		}

		public FrameworkElement DequeueViewForItem(int index)
		{
			var id = GetItemId(index);

			var container = TryDequeueCachedContainer(id);
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()} item={index} container={container} PreviousDC={container?.DataContext}");
			}
			if (container == null)
			{
				container = ItemsControl.GetContainerForIndex(index) as FrameworkElement;
			}

			ItemsControl.PrepareContainerForIndex(container, index);

			return container;
		}

		private FrameworkElement TryDequeueCachedContainer(int id)
		{
			if (_itemContainerCache.TryGetValue(id, out var cache))
			{
				if (cache.Count > 0)
				{
					var cachedView = cache.Pop();
					cachedView.Visibility = Visibility.Visible;
					return cachedView;
				}
			}

			return null;
		}

		public void RecycleViewForItem(FrameworkElement container, int index)
		{
			var id = GetItemId(index);

			if (!_owner.XamlParent.IsIndexItsOwnContainer(index))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"{GetMethodTag()} container={container} index={index}");
				}

				var cache = _itemContainerCache.FindOrCreate(id, () => new Stack<FrameworkElement>());

				if (cache.Count < CacheLimit)
				{
					cache.Push(container);
				}
				else
				{
					DiscardContainer(container);
				}
			}
			else
			{
				// Non-generated containers cannot be recycled as they
				// are placed at specific positions.

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"{GetMethodTag()} itemIsItsOwnContainer container={container} index={index}");
				}

				DiscardContainer(container);
			}
		}

		private static void DiscardContainer(FrameworkElement container)
		{
			// Cache is full, remove the view
			var parent = (container.Parent) as Panel;
			if (parent != null)
			{
				parent.Children.Remove(container);
			}
		}

		/// <summary>
		/// Hide cached views that are no longer displaying materialized items. Doing this in a single batch, rather than repeatedly toggling
		/// the visibility as items are recycled and then reused, significantly improves scrolling performance.
		/// </summary>
		public void UpdateVisibilities()
		{
			foreach (var cache in _itemContainerCache)
			{
				foreach (var view in cache.Value)
				{
					// This is a crude means of 'hiding' the view. We prefer not to unload it because recycling is cheaper if it stays in
					// the visual tree, but we should probably go to greater effort to conceal it from, eg, traversals of the visual tree.
					view.Visibility = Visibility.Collapsed;
				}
			}
		}

		private int GetItemId(int index)
		{
			var item = ItemsControl?.GetItemFromIndex(index);
			var template = ItemsControl?.ResolveItemTemplate(item);
			var id = template?.GetHashCode() ?? NoTemplateItemId;
			return id;
		}

		private string GetMethodTag([CallerMemberName] string caller = null)
			=> $"{nameof(VirtualizingPanelGenerator)}.{caller}()";
	}
}
