using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Colors = Microsoft.UI.Colors;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	/// <summary>
	/// Tall content scrolled to a fixed offset on a timer (via ChangeView). Scrolling shifts the whole
	/// viewport without repainting each item, so this verifies moved-but-not-repainted content is dirtied
	/// under damage-region, with output identical to a full-frame repaint.
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DamageRegion_Scroll", IsManualTest = true,
		Description = "Tall content scrolled to a fixed offset, for damage-region validation.")]
	public sealed partial class DamageRegion_Scroll : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(400) };
		private double _offset;
		private int _steps;

		public DamageRegion_Scroll()
		{
			this.InitializeComponent();

			// Distinct per-row colors so any stale (un-dirtied) strip after a scroll would be obvious.
			var colors = new[]
			{
				Colors.IndianRed, Colors.SteelBlue, Colors.SeaGreen, Colors.Goldenrod,
				Colors.MediumPurple, Colors.Teal, Colors.Chocolate, Colors.SlateGray,
			};
			for (var i = 0; i < 40; i++)
			{
				Content.Children.Add(new Border
				{
					Height = 50,
					Background = new SolidColorBrush(colors[i % colors.Length]),
					Child = new TextBlock { Text = $"Row {i}", Margin = new Thickness(8) },
				});
			}

			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			_offset += 130;
			Scroller.ChangeView(null, _offset, null, disableAnimation: true);

			// Scroll a fixed number of steps to a deterministic final offset, then settle so a post-settle
			// screenshot is comparable between full-frame and damage-region runs.
			if (++_steps >= 5)
			{
				_timer.Stop();
			}
		}
	}
}
