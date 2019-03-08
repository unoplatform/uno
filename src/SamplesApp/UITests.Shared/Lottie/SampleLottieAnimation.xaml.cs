using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Lottie
{
	[SampleControlInfo("Lottie", "Sample animation")]
	public sealed partial class SampleLottieAnimation : Page
	{
		public SampleLottieAnimation()
		{
			this.InitializeComponent();

			stretch.ItemsSource = Enum.GetValues(typeof(Stretch))
				.Cast<Stretch>()
				.Select(x => x.ToString());
			stretch.SelectedIndex = 3;

			play1.Tapped += (snd, evt) => player.PlayAsync(0, 1, false);
			playloop.Tapped += (snd, evt) => player.PlayAsync(0, 1, true);
			stop.Tapped += (snd, evt) => player.Stop();
			pause.Tapped += (snd, evt) => player.Pause();
			resume.Tapped += (snd, evt) => player.Resume();
		}
	}
}
