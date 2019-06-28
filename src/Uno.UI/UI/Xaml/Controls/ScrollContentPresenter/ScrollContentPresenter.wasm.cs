using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Uno.UI.Xaml;
using Microsoft.Extensions.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter
	{
		private ScrollBarVisibility _verticalScrollBarVisibility;
		private ScrollBarVisibility _horizotalScrollBarVisibility;
		private ScrollMode _horizontalScrollMode1;
		private ScrollMode _verticalScrollMode1;

		private static readonly string[] HorizontalModeClasses = { "scrollmode-x-disabled", "scrollmode-x-enabled", "scrollmode-x-auto" };

		internal Size ScrollBarSize
		{
			get
			{
				var (clientSize, offsetSize) = WindowManagerInterop.GetClientViewSize(HtmlId);

				return new Size(offsetSize.Width - clientSize.Width, offsetSize.Height - clientSize.Height);
			}
		}

		public ScrollContentPresenter()
		{
			PointerReleased += ScrollViewer_PointerReleased;
			PointerPressed += ScrollViewer_PointerPressed;
			PointerCanceled += ScrollContentPresenter_PointerCanceled;
			PointerMoved += ScrollContentPresenter_PointerMoved;
			PointerEntered += ScrollContentPresenter_PointerEntered;
			PointerExited += ScrollContentPresenter_PointerExited;
			PointerWheelChanged += ScrollContentPresenter_PointerWheelChanged;
		}

		private void ScrollContentPresenter_PointerWheelChanged(object sender, Input.PointerRoutedEventArgs e)
			=> HandlePointerEvent(e);

		private void ScrollContentPresenter_PointerExited(object sender, Input.PointerRoutedEventArgs e)
			=> HandlePointerEvent(e);

		private void ScrollContentPresenter_PointerEntered(object sender, Input.PointerRoutedEventArgs e)
			=> HandlePointerEvent(e);

		private void ScrollContentPresenter_PointerMoved(object sender, Input.PointerRoutedEventArgs e)
			=> HandlePointerEvent(e);

		private void ScrollContentPresenter_PointerCanceled(object sender, Input.PointerRoutedEventArgs e)
			=> HandlePointerEvent(e);

		private void ScrollViewer_PointerPressed(object sender, Input.PointerRoutedEventArgs e)
			=> HandlePointerEvent(e);

		private void ScrollViewer_PointerReleased(object sender, Input.PointerRoutedEventArgs e)
			=> HandlePointerEvent(e);

		private void HandlePointerEvent(Input.PointerRoutedEventArgs e)
		{
			var (clientSize, offsetSize) = WindowManagerInterop.GetClientViewSize(HtmlId);

			bool hasHorizontalScroll = (offsetSize.Height - clientSize.Height) > 0;
			bool hasVerticalScroll = (offsetSize.Width - clientSize.Width) > 0;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{HtmlId}: {offsetSize} / {clientSize} / {e.GetCurrentPoint(this)}");
			}

			if (!hasVerticalScroll && !hasHorizontalScroll)
			{
				return;
			}

			// The events coming from the scrollbars are bubbled up
			// to the the parents, as those are not (yey) XAML elements.
			// This can cause issues for popups with scrollable content and
			// light dismiss patterns.
			var position = e.GetCurrentPoint(this).Position;
			var isInVerticalScrollbar = hasVerticalScroll && position.X >= clientSize.Width;
			var isInHorizontalScrollbar = hasHorizontalScroll && position.Y >= clientSize.Height;

			if (isInVerticalScrollbar || isInHorizontalScrollbar)
			{
				e.Handled = true;
			}
		}

		public ScrollMode HorizontalScrollMode
		{
			get => _horizontalScrollMode1;
			set
			{
				_horizontalScrollMode1 = value;
				SetClasses(HorizontalModeClasses, (int)value);
			}
		}

		private static readonly string[] VerticalModeClasses = { "scrollmode-y-disabled", "scrollmode-y-enabled", "scrollmode-y-auto" };

		public ScrollMode VerticalScrollMode
		{
			get => _verticalScrollMode1;
			set
			{
				_verticalScrollMode1 = value;
				SetClasses(VerticalModeClasses, (int)value);
			}
		}

		public float MinimumZoomScale { get; private set; }

		public float MaximumZoomScale { get; private set; }

		private static readonly string[] VerticalVisibilityClasses = { "scroll-y-auto", "scroll-y-disabled", "scroll-y-hidden", "scroll-y-visible" };

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get { return _verticalScrollBarVisibility; }
			set
			{
				_verticalScrollBarVisibility = value;
				SetClasses(VerticalVisibilityClasses, (int)value);
			}
		}
		private static readonly string[] HorizontalVisibilityClasses = { "scroll-x-auto", "scroll-x-disabled", "scroll-x-hidden", "scroll-x-visible" };

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get { return _horizotalScrollBarVisibility; }
			set
			{
				_horizotalScrollBarVisibility = value;
				SetClasses(HorizontalVisibilityClasses, (int)value);
			}
		}

		protected override Foundation.Size MeasureOverride(Foundation.Size size)
		{
			var child = Content as UIElement;
			if (child != null)
			{
				var slotSize = size;
				if (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Height = double.PositiveInfinity;
				}
				if (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Width = double.PositiveInfinity;
				}

				child.Measure(slotSize);

				return new Size(
					Math.Min(size.Width, child.DesiredSize.Width),
					Math.Min(size.Height, child.DesiredSize.Height)
				);
			}

			return new Foundation.Size(0, 0);
		}

		protected override Foundation.Size ArrangeOverride(Foundation.Size finalSize)
		{
			var child = Content as UIElement;
			if (child != null)
			{
				var slotSize = finalSize;

				var desiredChildSize = child.DesiredSize;

				if (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Height = Math.Max(desiredChildSize.Height, finalSize.Height);
				}
				if (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Width = Math.Max(desiredChildSize.Width, finalSize.Width);
				}

				child.Arrange(new Rect(new Point(0, 0), slotSize));
			}

			return finalSize;
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();
			RegisterEventHandler("scroll", (EventHandler)OnScroll);
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
			UnregisterEventHandler("scroll", (EventHandler)OnScroll);
		}

		private void OnScroll(object sender, EventArgs args)
		{
			var left = GetProperty("scrollLeft");
			var top = GetProperty("scrollTop");

			if (!double.TryParse(left, NumberStyles.Number, CultureInfo.InvariantCulture, out var horizontalOffset))
			{
				horizontalOffset = 0;
			}
			if (!double.TryParse(top, NumberStyles.Number, CultureInfo.InvariantCulture, out var verticalOffset))
			{
				verticalOffset = 0;
			}

			(TemplatedParent as ScrollViewer)?.OnScrollInternal(
				horizontalOffset,
				verticalOffset,
				isIntermediate: false
			);
		}

		internal override bool IsViewHit()
		{
			return true;
		}
	}
}
