using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	[Sample("Windows.UI.Composition", Name = "DamageRegion_ShadowChild", IsManualTest = true,
		Description = "A shadow caster whose child moves, for damage-region descendant-shadow validation.")]
	public sealed partial class DamageRegion_ShadowChild : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private double _left;
		private int _steps;

		public DamageRegion_ShadowChild()
		{
			this.InitializeComponent();
			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			_left += 40;
			Canvas.SetLeft(Child, _left);
			if (++_steps >= 4)
			{
				_timer.Stop();
			}
		}
	}
}
