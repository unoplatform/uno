using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DirtyRectangles
{
	/// <summary>
	/// A backdrop-blur acrylic panel over a small element that moves behind it and settles. Stresses
	/// the dirty-rectangles damage region for backdrop effect brushes (which sample beyond their bounds).
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DirtyRectangles_Acrylic", IsManualTest = true,
		Description = "An acrylic backdrop-blur panel over a moving element, for dirty-rectangles effect-brush validation.")]
	public sealed partial class DirtyRectangles_Acrylic : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private double _left;
		private int _steps;

		public DirtyRectangles_Acrylic()
		{
			this.InitializeComponent();

			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			_left += 90;
			Canvas.SetLeft(Mover, _left);

			// Move behind the centered acrylic panel and settle at a deterministic position.
			if (++_steps >= 5)
			{
				_timer.Stop();
			}
		}
	}
}
