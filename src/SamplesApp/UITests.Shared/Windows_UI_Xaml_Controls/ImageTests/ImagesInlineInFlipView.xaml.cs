using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using UITests.Shared.Helpers;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.UITests.ImageTestsControl;

[Sample("Image", Description = "ImagesInlineInFlipView - the image view will be loaded after the Source has been set.")]
public sealed partial class ImagesInlineInFlipView : UserControl, IWaitableSample
{
	private readonly Task _samplePreparedTask;

	public ImagesInlineInFlipView()
	{
		this.InitializeComponent();
		_samplePreparedTask = WaitableSampleImageHelpers.WaitAllImages(image1, image2);
	}

	public Task SamplePreparedTask => _samplePreparedTask;
}
