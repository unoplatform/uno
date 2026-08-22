using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace UITests.Windows_UI_Xaml_Controls.Repeater;

/// <summary>
/// Self-contained auto-scroll behavior for an ItemsRepeater inside a ScrollViewer, modelled on a
/// typical chat conversation panel. When enabled it scrolls to the bottom as items are added, but
/// respects the user's scroll position (won't fight a scroll-up) and skips while the pointer is
/// over the chat. Used by <see cref="ItemsRepeaterChatConversation"/> to reproduce the dynamic
/// layout churn (UpdateLayout + ChangeView on add) that a static list lacks.
/// </summary>
public static class ChatAutoScroll
{
	private const double ScrollTolerance = 100.0;

	private static readonly ConditionalWeakTable<ItemsRepeater, ScrollState> _states = new();

	public static readonly DependencyProperty IsEnabledProperty =
		DependencyProperty.RegisterAttached(
			"IsEnabled",
			typeof(bool),
			typeof(ChatAutoScroll),
			new PropertyMetadata(false, OnIsEnabledChanged));

	public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

	public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

	private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not ItemsRepeater repeater)
		{
			return;
		}

		if ((bool)e.NewValue)
		{
			AttachBehavior(repeater);
		}
		else
		{
			DetachBehavior(repeater);
		}
	}

	private static void AttachBehavior(ItemsRepeater repeater)
	{
		var state = new ScrollState(repeater);
		_states.AddOrUpdate(repeater, state);

		repeater.Unloaded += state.OnUnloaded;

		state.ItemsSourceCallbackToken =
			repeater.RegisterPropertyChangedCallback(ItemsRepeater.ItemsSourceProperty, state.OnItemsSourceChanged);

		if (repeater.ItemsSource is INotifyCollectionChanged incc)
		{
			state.SubscribeCollection(incc);
		}

		if (repeater.IsLoaded)
		{
			state.FindAndAttachScrollViewer();
		}
		else
		{
			repeater.Loaded += state.OnLoaded;
		}
	}

	private static void DetachBehavior(ItemsRepeater repeater)
	{
		if (_states.TryGetValue(repeater, out var state))
		{
			state.Detach();
			_states.Remove(repeater);
		}
	}

	private static ScrollViewer FindAncestorScrollViewer(DependencyObject element)
	{
		var parent = VisualTreeHelper.GetParent(element);
		while (parent is not null)
		{
			if (parent is ScrollViewer sv)
			{
				return sv;
			}

			parent = VisualTreeHelper.GetParent(parent);
		}

		return null;
	}

	private sealed class ScrollState
	{
		private readonly ItemsRepeater _repeater;
		private ScrollViewer _scrollViewer;
		private INotifyCollectionChanged _currentCollection;
		private bool _isAtBottom = true;
		private bool _isMouseOver;
		private bool _scrollToBottomPending;

		public long ItemsSourceCallbackToken { get; set; }

		public ScrollState(ItemsRepeater repeater) => _repeater = repeater;

		public void OnLoaded(object sender, RoutedEventArgs e)
		{
			_repeater.Loaded -= OnLoaded;
			FindAndAttachScrollViewer();
		}

		public void OnUnloaded(object sender, RoutedEventArgs e) => DetachBehavior(_repeater);

		public void FindAndAttachScrollViewer()
		{
			_scrollViewer = FindAncestorScrollViewer(_repeater);
			if (_scrollViewer is not null)
			{
				_scrollViewer.ViewChanged += OnViewChanged;
				_scrollViewer.PointerEntered += OnPointerEntered;
				_scrollViewer.PointerExited += OnPointerExited;
				UpdateIsAtBottom();
			}
		}

		public void OnItemsSourceChanged(DependencyObject sender, DependencyProperty dp)
		{
			UnsubscribeCollection();

			if (_repeater.ItemsSource is INotifyCollectionChanged incc)
			{
				SubscribeCollection(incc);
			}
		}

		public void SubscribeCollection(INotifyCollectionChanged collection)
		{
			_currentCollection = collection;
			collection.CollectionChanged += OnCollectionChanged;
		}

		private void UnsubscribeCollection()
		{
			if (_currentCollection is not null)
			{
				_currentCollection.CollectionChanged -= OnCollectionChanged;
				_currentCollection = null;
			}
		}

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// Only Add/Reset bring new content to the bottom. Replace/Remove must not override
			// the user's scroll position.
			var shouldScroll = e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset;
			if (!shouldScroll)
			{
				return;
			}

			// Pointer over the chat → don't interrupt the user's reading/scroll position.
			if (_isMouseOver || !_isAtBottom || _scrollToBottomPending)
			{
				return;
			}

			_scrollToBottomPending = true;

			// Defer to let layout settle after the new item is measured.
			if (!_repeater.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
			{
				_scrollToBottomPending = false;
				ScrollToBottomCore();
			}))
			{
				_scrollToBottomPending = false;
			}
		}

		private void OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e) => UpdateIsAtBottom();

		private void OnPointerEntered(object sender, PointerRoutedEventArgs e) => _isMouseOver = true;

		private void OnPointerExited(object sender, PointerRoutedEventArgs e) => _isMouseOver = false;

		private void UpdateIsAtBottom()
		{
			if (_scrollViewer is null)
			{
				return;
			}

			var scrollableHeight = _scrollViewer.ScrollableHeight;
			var verticalOffset = _scrollViewer.VerticalOffset;
			_isAtBottom = scrollableHeight <= 0 || (scrollableHeight - verticalOffset) <= ScrollTolerance;
		}

		private void ScrollToBottomCore()
		{
			if (_scrollViewer is null)
			{
				return;
			}

			// Force a layout pass so newly-measured items are reflected in ScrollableHeight
			// before we issue ChangeView.
			try
			{
				_scrollViewer.UpdateLayout();
			}
			catch (InvalidOperationException)
			{
				// A layout cycle was detected; the next collection-changed event retries.
				return;
			}

			var scrollableHeight = _scrollViewer.ScrollableHeight;
			if (scrollableHeight <= 0)
			{
				return;
			}

			_scrollViewer.ChangeView(null, scrollableHeight, null, disableAnimation: true);
		}

		public void Detach()
		{
			UnsubscribeCollection();

			_repeater.UnregisterPropertyChangedCallback(ItemsRepeater.ItemsSourceProperty, ItemsSourceCallbackToken);
			_repeater.Loaded -= OnLoaded;
			_repeater.Unloaded -= OnUnloaded;

			if (_scrollViewer is not null)
			{
				_scrollViewer.ViewChanged -= OnViewChanged;
				_scrollViewer.PointerEntered -= OnPointerEntered;
				_scrollViewer.PointerExited -= OnPointerExited;
				_scrollViewer = null;
			}
		}
	}
}
