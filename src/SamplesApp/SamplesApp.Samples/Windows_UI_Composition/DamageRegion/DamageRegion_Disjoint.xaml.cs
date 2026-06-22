using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	[Sample("Windows.UI.Composition", Name = "DamageRegion_Disjoint", IsManualTest = true,
		Description = "Two far-apart elements updating together, for damage-region disjoint-region validation.")]
	public sealed partial class DamageRegion_Disjoint : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(400) };
		private int _steps;

		public DamageRegion_Disjoint()
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

			if (_steps >= 5)
			{
				_timer.Stop();
			}
		}
	}
}
