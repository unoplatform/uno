using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	[Sample("Windows.UI.Composition", Name = "DamageRegion_Opacity", IsManualTest = true,
		Description = "An element repeatedly fading to Opacity 0 over static content, for damage-region validation.")]
	public sealed partial class DamageRegion_Opacity : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private int _steps;

		public DamageRegion_Opacity()
		{
			this.InitializeComponent();

			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			Fader.Opacity = Fader.Opacity == 0 ? 1 : 0;

			if (++_steps >= 6)
			{
				_timer.Stop();
			}
		}
	}
}
