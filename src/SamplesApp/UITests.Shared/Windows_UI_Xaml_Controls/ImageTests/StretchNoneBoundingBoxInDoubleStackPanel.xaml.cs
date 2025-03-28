using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using UITests.Shared.Helpers;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.UITests.ImageTestsControl;

[Sample("Image", Description = "StretchNoneBoundingBoxInDoubleStackPanel")]
public sealed partial class StretchNoneBoundingBoxInDoubleStackPanel : UserControl, IWaitableSample
{
	private readonly Task _samplePreparedTask;

	public StretchNoneBoundingBoxInDoubleStackPanel()
	{
		this.InitializeComponent();
		_samplePreparedTask = WaitableSampleImageHelpers.WaitAllImages(image1);
	}

	public Task SamplePreparedTask => _samplePreparedTask;
}
