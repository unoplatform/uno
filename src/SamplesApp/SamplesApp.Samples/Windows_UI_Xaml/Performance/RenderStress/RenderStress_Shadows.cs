#nullable enable

using System.Numerics;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Elevated borders casting a <see cref="ThemeShadow"/> onto a receiver — the shadow blur pass is the case the
	/// rendering-contract review measured as the WebGPU backend's slowest primitive, so it's the sharpest comparator.
	/// </summary>
	[Sample("Performance", Name = "RenderStress_Shadows", Description = "Renderer stress: N elevated ThemeShadow casters redrawn every frame (shadow blur = the review's slowest WebGPU case). Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RenderStress_Shadows : RenderStressBase
	{
		protected override string ScenarioName => "Shadows (ThemeShadow blur)";
		protected override int DefaultCount => 150;

		protected override void Populate(Canvas host, int count, double width, double height)
		{
			var rng = new Rng(6);
			var shadow = new ThemeShadow();

			// A transparent receiver spanning the host catches every caster's projected shadow.
			var receiver = new Border { Width = width, Height = height, Background = new SolidColorBrush(Colors.Transparent) };
			Canvas.SetLeft(receiver, 0);
			Canvas.SetTop(receiver, 0);
			host.Children.Add(receiver);
			shadow.Receivers.Add(receiver);

			for (var i = 0; i < count; i++)
			{
				var b = new Border
				{
					Width = rng.Range(40, 80),
					Height = rng.Range(40, 80),
					CornerRadius = new CornerRadius(8),
					Background = new SolidColorBrush(rng.Color()),
					Shadow = shadow,
				};
				Canvas.SetLeft(b, rng.Range(0, width - 80));
				Canvas.SetTop(b, rng.Range(0, height - 80));
				b.Translation = new Vector3(0, 0, (float)rng.Range(16, 48));
				host.Children.Add(b);
			}
		}
	}
}
