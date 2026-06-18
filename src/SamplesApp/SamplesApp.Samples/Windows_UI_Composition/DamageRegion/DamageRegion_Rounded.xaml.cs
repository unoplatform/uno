using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Colors = Microsoft.UI.Colors;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	/// <summary>
	/// A rounded Border and an Ellipse repainting in place on a timer. Used to inspect (via the repaint
	/// overlay) that the damage region follows the actual rounded/curved shape rather than its bounding box.
	/// Manual-only — not in the byte-identity harness: on OpenGL/WebGL an antialiased curved edge rasterizes
	/// ~1px differently under a clipped (damage-region) present than under a full present, regardless of the
	/// damage shape (rectangular bounding-box damage shows the same difference). It is a pre-existing GPU
	/// clipped-vs-unclipped rasterization difference, not introduced by the curved damage region; software is
	/// exact.
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DamageRegion_Rounded", IsManualTest = true,
		Description = "A rounded Border and an Ellipse repainting in place, for damage-region shape validation.")]
	public sealed partial class DamageRegion_Rounded : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(400) };
		private int _steps;

		public DamageRegion_Rounded()
		{
			this.InitializeComponent();
			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			var toggle = (++_steps & 1) == 0;
			RoundedBorder.Background = new SolidColorBrush(toggle ? Colors.SeaGreen : Colors.DarkOrange);
			Circle.Fill = new SolidColorBrush(toggle ? Colors.IndianRed : Colors.SteelBlue);
		}
	}
}
