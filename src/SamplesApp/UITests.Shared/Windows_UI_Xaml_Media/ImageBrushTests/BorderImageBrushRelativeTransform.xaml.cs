using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using UITests.Shared.Helpers;
using System.Threading.Tasks;

namespace GenericApp.Views.Content.UITests.ImageBrushTestControl
{
	[Sample("Brushes")]
	public sealed partial class BorderImageBrushRelativeTransform : UserControl, IWaitableSample
	{
		private readonly Task _samplePreparedTask;

		public BorderImageBrushRelativeTransform()
		{
			this.InitializeComponent();
			_samplePreparedTask = WaitableSampleImageHelpers.WaitAllImages(imageBrush1, imageBrush2);
		}

		public Task SamplePreparedTask => _samplePreparedTask;
	}
}
