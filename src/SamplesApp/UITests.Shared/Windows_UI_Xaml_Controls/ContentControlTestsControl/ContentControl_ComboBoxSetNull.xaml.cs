using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	[SampleControlInfo("ContentControl", "ContentControl_ComboBoxSetNull", typeof(Presentation.SamplePages.ContentControlTestViewModel), isManualTest: true)]
	public sealed partial class ContentControl_ComboBoxSetNull : UserControl
	{
		public ContentControl_ComboBoxSetNull()
		{
			this.InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			MainContentControl.Content = null;
		}
	}
}
