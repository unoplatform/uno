using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using UITests.Shared.Helpers;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.UITests.ImageBrushTestControl;

[Sample("Brushes")]
public sealed partial class Ellipse : UserControl, IWaitableSample
{
	private readonly Task _samplePreparedTask;

	public Ellipse()
	{
		this.InitializeComponent();
		_samplePreparedTask = WaitableSampleImageHelpers.WaitAllImages(imageBrush1, imageBrush2);
	}

	public Task SamplePreparedTask => _samplePreparedTask;
}
