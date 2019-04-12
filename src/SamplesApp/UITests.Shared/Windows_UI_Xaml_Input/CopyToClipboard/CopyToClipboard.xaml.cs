using UITests.Shared.Windows_UI_Xaml_Input.Models;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Xaml_Input.CopyToClipboard
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	
	[SampleControlInfo("CopyToClipboard", "CopyToClipboard", typeof(CopyToClipboardViewModel))]
	public sealed partial class CopyToClipboard : UserControl
    {
        public CopyToClipboard()
        {
            this.InitializeComponent();
        }
    }
}
