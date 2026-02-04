using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Lottie
{
	[Sample("Lottie", Name = "Sample animation", IgnoreInSnapshotTests = true /* Fails for Android # */)]
	public sealed partial class SampleLottieAnimation : Page
	{
		public SampleLottieAnimation()
		{
			this.InitializeComponent();

			stretch.ItemsSource = Enum.GetNames<Stretch>();
			stretch.SelectedIndex = 2;

			play1.Tapped += (snd, evt) => _ = player.PlayAsync(from.Value, to.Value, false);
			playloop.Tapped += (snd, evt) => _ = player.PlayAsync(from.Value, to.Value, true);
			stop.Tapped += (snd, evt) => player.Stop();
			pause.Tapped += (snd, evt) => player.Pause();
			resume.Tapped += (snd, evt) => player.Resume();

			file.ItemsSource = new[]
			{
				"ms-appx:///Lottie/lottie-logo.json",
				"ms-appx:///Lottie/LightBulb.json",
				"ms-appx:///Lottie/uno.json",
				"ms-appx:///Lottie/4770-lady-and-dove.json",
				"ms-appx:///Lottie/4930-checkbox-animation.json",
				"ms-appx:///Lottie/this-file-does-not-exists.json",
				"embedded://./(assembly).Lottie.loading.json",
				"https://assets2.lottiefiles.com/datafiles/c56b08b89932c090a0948b53fd58427d/data.json"
			};

			file.SelectedIndex = 0;
		}
	}
}
