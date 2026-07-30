using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	[Sample("Windows.UI.Composition", Name = "DamageRegion_MovedElement", IsManualTest = true,
		Description = "A small element moving across a static background, for damage-region validation.")]
	public sealed partial class DamageRegion_MovedElement : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private double _left;
		private int _steps;

		public DamageRegion_MovedElement()
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

			if (++_steps >= 5)
			{
				_timer.Stop();
			}
		}
	}
}
