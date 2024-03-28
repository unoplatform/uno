using Android.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Services
{
	/// <summary>
	/// Used on Android to relayout views selectively without relaying out the entire visual tree.
	/// </summary>
	internal static class LayoutManager
	{
		private static bool _IsArrangeRequested;
		private static readonly HashSet<FrameworkElement> _arrangeQueue = new HashSet<FrameworkElement>();
		public static void InvalidateArrange(FrameworkElement view)
		{
			if (typeof(LayoutManager).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(LayoutManager).Log().LogDebug($"Invalidating {view} with {GetAncestorCount(view)} ancestors in visual tree.");
			}
			if (!_IsArrangeRequested)
			{
				_IsArrangeRequested = true;
				_ = Uno.UI.Dispatching.NativeDispatcher.Main.RunAnimation(Arrange);
			}

			_arrangeQueue.Add(view);
		}

		private static void Arrange()
		{
			_IsArrangeRequested = false;
			var queue = GetSeniorViews();
			if (typeof(LayoutManager).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(LayoutManager).Log().LogDebug($"Starting arrange pass, {_arrangeQueue.Count} views in queue, {queue.Count} senior views will be arranged.");
			}
			_arrangeQueue.Clear();

			foreach (var view in queue)
			{
				view.Invalidate();
				if (typeof(LayoutManager).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(LayoutManager).Log().LogDebug($"Measuring {view} with {GetAncestorCount(view)} ancestors in visual tree.");
				}
				IFrameworkElementHelper.Measure(view, view.AssignedActualSize.Add(view.Margin));
			}
			foreach (var view in queue)
			{
				{
					typeof(LayoutManager).Log().LogDebug($"Laying out {view} with {GetAncestorCount(view)} ancestors in visual tree.");
				}
				view.Layout(
					view.Left,
					view.Top,
					view.Right,
					view.Bottom
				);
			}
		}

		/// <summary>
		/// Filter views which have an ancestor already in the queue.
		/// </summary>
		private static List<FrameworkElement> GetSeniorViews()
		{
			var queue = new List<FrameworkElement>();

			foreach (var view in _arrangeQueue)
			{
				if (HasNoAncestorInQueue(view))
				{
					queue.Add(view);
				}
			}

			return queue;
		}

		private static bool HasNoAncestorInQueue(FrameworkElement view)
		{
			var parent = (view as View).Parent;
			while (parent != null)
			{
				if (parent is FrameworkElement xamlParent && _arrangeQueue.Contains(xamlParent))
				{
					//Ancestor is also in queue, this view need not lay itself out
					return false;
				}
				if (!parent.IsLayoutRequested)
				{
					//Ancestor is not in queue and has not had RequestLayout or InvalidateLayout called, we need to lay out
					return true;
				}
				parent = parent.Parent;
			}
			return true;
		}

		private static int GetAncestorCount(View view)
		{
			var parent = view.Parent;
			int count = 0;
			while (parent != null)
			{
				count++;
				parent = parent.Parent;
			}
			return count;
		}
	}
}
