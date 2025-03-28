using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using UITests.Shared.Helpers;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.UITests.ImageBrushTestControl;

[Sample("Brushes", Description = "Effect of ImageBrush.Stretch on the visual result")]
public sealed partial class ImageBrushAlignmentXY2 : UserControl, IWaitableSample
{
	private readonly Task _samplePreparedTask;

	public ImageBrushAlignmentXY2()
	{
		this.InitializeComponent();
		_samplePreparedTask = WaitableSampleImageHelpers.WaitAllImages(imageBrush1, imageBrush2, imageBrush3, imageBrush4);
	}

	public Task SamplePreparedTask => _samplePreparedTask;
}
