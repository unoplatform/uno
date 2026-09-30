using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.Performance
{
	[Sample("Performance", IsManualTest = true, Description = "Maximize the window and make sure that the frame time (right value) in the FPS indicator stays low and the image stays sharp. Make sure to test on different DPIs.")]
	public sealed partial class Performance_ImageUpscaling : Page
	{
		public Performance_ImageUpscaling()
		{
			this.InitializeComponent();

			Loaded += (s, e) =>
			{
				colorStoryboard.Begin();
			};

			Unloaded += (s, e) =>
			{
				colorStoryboard.Stop();
			};
		}
	}
}
