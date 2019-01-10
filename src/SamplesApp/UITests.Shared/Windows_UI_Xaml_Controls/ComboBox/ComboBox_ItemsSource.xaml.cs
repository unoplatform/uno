using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox
{
	[SampleControlInfo("ComboBox", "ComboBox_ItemsSource", typeof(ListViewViewModel))]
	public sealed partial class ComboBox_ItemsSource : UserControl
    {
        public ComboBox_ItemsSource()
        {
            this.InitializeComponent();
        }
    }
}
