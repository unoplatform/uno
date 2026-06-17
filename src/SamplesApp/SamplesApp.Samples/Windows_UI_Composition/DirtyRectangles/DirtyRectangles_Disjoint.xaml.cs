using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DirtyRectangles
{
	/// <summary>
	/// Two elements far apart (top and bottom), both updated on a timer, with a large empty gap between them.
	/// Verifies that the per-frame dirty region is a disjoint union of the two changed areas — the gap is not
	/// repainted — while staying pixel-identical to a full repaint.
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DirtyRectangles_Disjoint", IsManualTest = true,
		Description = "Two far-apart elements updating together, for dirty-rectangles disjoint-region validation.")]
	public sealed partial class DirtyRectangles_Disjoint : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(400) };
		private int _steps;

		public DirtyRectangles_Disjoint()
		{
			this.InitializeComponent();

			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			_steps++;
			var label = _steps.ToString(CultureInfo.InvariantCulture);
			TopText.Text = "top " + label;
			BottomText.Text = "bottom " + label;

			// Settle at a deterministic final state so an after-settle screenshot is comparable between
			// full-frame and dirty-rectangles runs. The two text updates dirty only their own regions; if the
			// gap between them were (incorrectly) repainted, output would still match — the disjoint-region win
			// is verified visually via the repaint overlay, this sample guards correctness.
			if (_steps >= 5)
			{
				_timer.Stop();
			}
		}
	}
}
