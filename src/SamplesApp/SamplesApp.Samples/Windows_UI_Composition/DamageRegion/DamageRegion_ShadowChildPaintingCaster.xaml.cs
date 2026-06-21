using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	/// <summary>
	/// A self-painting (opaque-background) shadow caster whose child overflows the caster and moves. The shadow
	/// silhouette is the union of the caster and its descendants, so a moving child changes the shadow even
	/// though the caster paints its own content and stays put. Verifies the caster re-damages its shadow region
	/// on subtree changes — otherwise a stale shadow trails the child — while staying identical to a full repaint.
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DamageRegion_ShadowChildPaintingCaster", IsManualTest = true,
		Description = "A self-painting shadow caster whose overflowing child moves, for damage-region descendant-shadow validation.")]
	public sealed partial class DamageRegion_ShadowChildPaintingCaster : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
		private double _left;
		private int _steps;

		public DamageRegion_ShadowChildPaintingCaster()
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
