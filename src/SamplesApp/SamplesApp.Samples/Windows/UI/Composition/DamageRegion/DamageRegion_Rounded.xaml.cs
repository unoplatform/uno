using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
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
