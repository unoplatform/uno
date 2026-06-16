using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DirtyRectangles
{
	/// <summary>
	/// A small element that moves across a static background on a timer. Verifies that both
	/// the vacated and the new regions are repainted (no stale pixels) under dirty-rectangles.
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DirtyRectangles_MovedElement", IsManualTest = true,
		Description = "A small element moving across a static background, for dirty-rectangles validation.")]
	public sealed partial class DirtyRectangles_MovedElement : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private double _left;
		private int _steps;

		public DirtyRectangles_MovedElement()
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
			// taken after settling is comparable between full-frame and dirty-rectangles runs. If the
			// vacated (old) region isn't repainted under dirty rectangles, stale copies of the element
			// would remain at the intermediate positions and the comparison would fail.
			if (++_steps >= 5)
			{
				_timer.Stop();
			}
		}
	}
}
