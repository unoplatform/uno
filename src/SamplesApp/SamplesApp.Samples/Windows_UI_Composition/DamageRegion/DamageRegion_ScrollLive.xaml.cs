#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	/// <summary>
	/// A real ListView with nested item templates, auto-scrolled continuously (smooth animation, ping-pong).
	/// Diagnostic-only: run with UNO_DAMAGE_REGION_LOG=true and watch the console to see whether a live scroll
	/// stays a tight sub-region or coalesces to a full-frame repaint.
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DamageRegion_ScrollLive", IsManualTest = true,
		Description = "Continuously auto-scrolling ListView, for live damage-region inspection.")]
	public sealed partial class DamageRegion_ScrollLive : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
		private ScrollViewer? _sv;
		private double _offset;
		private double _dir = 1;

		public DamageRegion_ScrollLive()
		{
			this.InitializeComponent();

			List.ItemsSource = Enumerable.Range(0, 200)
				.Select(i => new { Title = $"Item {i}", Subtitle = $"Subtitle for row {i}" })
				.ToList();

			_timer.Tick += OnTick;
			Loaded += (_, _) =>
			{
				_sv = FindScrollViewer(List);
				_timer.Start();
			};
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object? sender, object e)
		{
			if (_sv is null)
			{
				_sv = FindScrollViewer(List);
				return;
			}

			var max = _sv.ScrollableHeight;
			if (max <= 0)
			{
				return;
			}

			_offset += _dir * 12;
			if (_offset >= max) { _offset = max; _dir = -1; }
			else if (_offset <= 0) { _offset = 0; _dir = 1; }

			_sv.ChangeView(null, _offset, null, disableAnimation: true);
		}

		private static ScrollViewer? FindScrollViewer(DependencyObject root)
		{
			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				if (child is ScrollViewer sv)
				{
					return sv;
				}

				if (FindScrollViewer(child) is { } found)
				{
					return found;
				}
			}

			return null;
		}
	}
}
