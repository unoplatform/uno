using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using UITests.Shared.Helpers;
using System.Threading.Tasks;

namespace Uno.UI.Samples.UITests.Image
{
	[Sample(Description = "ImageInStackPanel")]
	public sealed partial class ImageInStackPanel : UserControl, IWaitableSample
	{
		private readonly Task _samplePreparedTask;

		public ImageInStackPanel()
		{
			this.InitializeComponent();
			_samplePreparedTask = WaitableSampleImageHelpers.WaitAllImages(image1);
		}

		public Task SamplePreparedTask => _samplePreparedTask;
	}
}
