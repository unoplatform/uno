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
		}
	}
}
