using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Windows.UI.Xaml.Controls;

namespace GenericApp.Views.Content.UITests.Clip
{
	[SampleControlInfo("Clip", viewModelType: typeof(ButtonTestsViewModel))]
	public sealed partial class ButtonClippingTestsControl : UserControl
	{
		public ButtonClippingTestsControl()
		{
			this.InitializeComponent();
		}
	}
}
