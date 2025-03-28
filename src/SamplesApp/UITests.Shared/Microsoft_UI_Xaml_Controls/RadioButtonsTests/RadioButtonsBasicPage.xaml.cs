using System.Linq;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.RadioButtonsTests
{
	[Sample("Buttons", "MUX")]
	public sealed partial class RadioButtonsBasicPage : Page
	{
		public RadioButtonsBasicPage()
		{
			this.InitializeComponent();
			TestRadioButtons.ItemsSource = Enumerable.Range(0, 30).Select(i => $"Item {i}").ToArray();
		}
	}
}
