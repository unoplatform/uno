using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox
{
	[SampleControlInfo("ComboBox", "ComboBox_Legacy", typeof(ListViewViewModel))]
    public sealed partial class ComboBox_Legacy : UserControl
    {
        public ComboBox_Legacy()
        {
            this.InitializeComponent();
        }
    }
}
