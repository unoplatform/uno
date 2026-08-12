using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	[Sample("Windows.UI.Composition", Name = "DamageRegion_ShadowChildShape", IsManualTest = true,
		Description = "A shadow caster whose child Shape overflows its Size, for damage-region shadow-silhouette validation.")]
	public sealed partial class DamageRegion_ShadowChildShape : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private double _top;
		private int _steps;

		public DamageRegion_ShadowChildShape()
		{
			this.InitializeComponent();
			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			_top += 30;
			Canvas.SetTop(Child, _top);
			if (++_steps >= 4)
			{
				_timer.Stop();
			}
		}
	}
}
