#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance
{
	/// <summary>
	/// A large virtualized grid that can scroll continuously, next to two small always-animating elements.
	/// Auto-scroll dirties nearly the whole viewport each frame; the ticker and the spinner change a few
	/// hundred pixels while everything else stays put. The two cases exercise opposite ends of partial repaint.
	/// </summary>
	[Sample("Performance")]
	public sealed partial class Performance_RenderRegression : Page
	{
		private const double ScrollPixelsPerSecond = 2000;
		private const int ItemCount = 2837;

		private readonly DispatcherTimer _tickerTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

		private long _lastScrollTicks;
		private double _scrollOffset;
		private int _scrollDirection = 1;
		private int _tickerValue;

		public Performance_RenderRegression()
		{
			this.InitializeComponent();

			Repeater.ItemsSource = Enumerable.Range(0, ItemCount).Select(i => $"Item {i}").ToList();
			ApplyItemTemplate();

			TemplatePicker.SelectionChanged += (_, _) => ApplyItemTemplate();
			AutoScrollToggle.Checked += (_, _) => StartAutoScroll();
			AutoScrollToggle.Unchecked += (_, _) => StopAutoScroll();
			SpinnerToggle.Checked += (_, _) => Spinner.IsActive = true;
			SpinnerToggle.Unchecked += (_, _) => Spinner.IsActive = false;
			TickerToggle.Checked += (_, _) => _tickerTimer.Start();
			TickerToggle.Unchecked += (_, _) => _tickerTimer.Stop();

			_tickerTimer.Tick += (_, _) => Ticker.Text = (++_tickerValue).ToString();

			// Nothing keeps running once the page is gone: both the timer and the per-frame handler would
			// otherwise hold this page alive and keep asking for frames.
			Unloaded += (_, _) =>
			{
				_tickerTimer.Stop();
				StopAutoScroll();
			};
		}

		private void ApplyItemTemplate()
			=> Repeater.ItemTemplate = (DataTemplate)Resources[
				(TemplatePicker.SelectedItem as ComboBoxItem)?.Content as string ?? "FluentTile"];

		private void StartAutoScroll()
		{
			_lastScrollTicks = Stopwatch.GetTimestamp();
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
		}

		private void StopAutoScroll()
			=> Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;

		// Scrolling a fixed number of pixels per second (rather than per frame) keeps the scene moving at the
		// same speed whatever the frame rate, so runs stay comparable.
		private void OnRendering(object? sender, object e)
		{
			var now = Stopwatch.GetTimestamp();
			var elapsed = Stopwatch.GetElapsedTime(_lastScrollTicks, now).TotalSeconds;
			_lastScrollTicks = now;

			var scrollable = Scroller.ScrollableHeight;
			if (scrollable <= 1)
			{
				return;
			}

			_scrollOffset += _scrollDirection * ScrollPixelsPerSecond * elapsed;
			if (_scrollOffset >= scrollable)
			{
				_scrollOffset = scrollable;
				_scrollDirection = -1;
			}
			else if (_scrollOffset <= 0)
			{
				_scrollOffset = 0;
				_scrollDirection = 1;
			}

			Scroller.ChangeView(null, _scrollOffset, null, disableAnimation: true);
		}
	}
}
