using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	[Sample("ContentControl", Name = "ContentControl_ComboBoxSetNull", ViewModelType = typeof(Presentation.SamplePages.ContentControlTestViewModel), Description = "Shows a ComboBox and a Button. \n" +
		"On WASM, and any other platform, when the `remove` button is clicked, the application should not throw an exception.",
		IsManualTest = true)]
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
