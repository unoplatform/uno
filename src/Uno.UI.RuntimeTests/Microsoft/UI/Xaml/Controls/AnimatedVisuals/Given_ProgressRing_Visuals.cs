#if __SKIA__
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls.AnimatedVisuals;

[TestClass]
[RunsOnUIThread]
public partial class Given_ProgressRing_Visuals
{
	// The rotating group fuses Offset <40,40> and Scale <5,5> into TransformMatrix and animates
	// RotationAngleInDegrees separately (ProgressRingIndeterminate.cpp:161-168), so it only spins in place
	// if TransformMatrix is applied after the rotation. When the order is reversed the arcs orbit the
	// canvas origin and leave the visual entirely — measured as 0 drawn pixels at progress 0.25 and 0.5.
	[TestMethod]
	[DataRow(0.0)]
	[DataRow(0.25)]
	[DataRow(0.5)]
	[DataRow(0.75)]
	public async Task When_Indeterminate_Spins_Arcs_Stay_On_The_Ring(double progress)
	{
		var player = new AnimatedVisualPlayer
		{
			Source = new global::Microsoft.UI.Xaml.Controls.AnimatedVisuals.ProgressRingIndeterminate(),
			Width = 160,
			Height = 160,
			AutoPlay = false,
		};

		await UITestHelper.Load(player);
		Assert.IsTrue(player.IsAnimatedVisualLoaded, "animated visual did not load");

		player.SetProgress(progress);
		await WindowHelper.WaitForIdle();

		var bitmap = await UITestHelper.ScreenShot(player);
		await bitmap.Populate();

		// Only the rotating group is drawn in the accent colour; the static ring behind it is grey and
		// never moves, so counting every non-background pixel would stay put even if the arcs flew off.
		long sumX = 0, sumY = 0, count = 0;
		for (var x = 0; x < bitmap.Width; x++)
		{
			for (var y = 0; y < bitmap.Height; y++)
			{
				var p = bitmap.GetPixel(x, y);
				if (p.B > 120 && p.B > p.R + 40 && p.B > p.G + 20)
				{
					sumX += x;
					sumY += y;
					count++;
				}
			}
		}

		Assert.IsTrue(count > 20, $"progress {progress}: the rotating arcs drew {count} px - they orbited off the visual instead of spinning in place");

		// The arcs ride the ring, so their centroid sits out on the annulus, never at the middle.
		var centreX = bitmap.Width / 2.0;
		var centreY = bitmap.Height / 2.0;
		var radius = Math.Sqrt(Math.Pow(sumX / (double)count - centreX, 2) + Math.Pow(sumY / (double)count - centreY, 2));

		Assert.IsTrue(radius < centreX * 1.05, $"progress {progress}: arc centroid is {radius:F1}px from centre, beyond the ring radius {centreX:F1}px");
	}
}
#endif
