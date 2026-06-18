using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	/// <summary>
	/// A small element casting a drop shadow that moves across a static background on a timer. Verifies that
	/// the shadow extent (which paints beyond the element's own size) is included in the damage region, so no
	/// stale shadow pixels remain, and output stays identical to a full-frame repaint.
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DamageRegion_Shadow", IsManualTest = true,
		Description = "A shadow-casting element moving across a static background, for damage-region validation.")]
	public sealed partial class DamageRegion_Shadow : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private double _left;
		private int _steps;

		public DamageRegion_Shadow()
		{
			this.InitializeComponent();

			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			_left += 50;
			Canvas.SetLeft(Mover, _left);

			// Move a fixed number of steps then settle at a deterministic final position, so a screenshot
			// taken after settling is comparable between full-frame and damage-region runs. If the shadow's
			// vacated region isn't repainted, stale shadow copies would remain at intermediate positions.
			if (++_steps >= 5)
			{
				_timer.Stop();
			}
		}
	}
}
