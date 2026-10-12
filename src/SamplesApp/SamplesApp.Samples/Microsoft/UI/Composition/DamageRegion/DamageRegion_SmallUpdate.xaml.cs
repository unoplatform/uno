using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	[Sample("Windows.UI.Composition", Name = "DamageRegion_SmallUpdate", IsManualTest = true,
		Description = "A static background with one small element updating on a timer, for damage-region validation.")]
	public sealed partial class DamageRegion_SmallUpdate : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private int _count;

		public DamageRegion_SmallUpdate()
		{
			this.InitializeComponent();

			_timer.Tick += OnTick;
			Loaded += (_, _) => _timer.Start();
			Unloaded += (_, _) => _timer.Stop();
		}

		private void OnTick(object sender, object e)
		{
			Counter.Text = (++_count).ToString();

			if (_count >= 5)
			{
				_timer.Stop();
			}
		}
	}
}
