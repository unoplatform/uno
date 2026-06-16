using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DirtyRectangles
{
	/// <summary>
	/// A large static background with a single small label that updates on a timer.
	/// Exercises the dirty-rectangles path: only the label region should change frame to frame.
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DirtyRectangles_SmallUpdate", IsManualTest = true,
		Description = "A static background with one small element updating on a timer, for dirty-rectangles validation.")]
	public sealed partial class DirtyRectangles_SmallUpdate : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private int _count;

		public DirtyRectangles_SmallUpdate()
		{
			this.InitializeComponent();

			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			Counter.Text = (++_count).ToString();

			// Settle at a deterministic final state so a screenshot taken after settling is comparable
			// between full-frame and dirty-rectangles runs.
			if (_count >= 5)
			{
				_timer.Stop();
			}
		}
	}
}
