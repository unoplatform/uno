using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml.WindowTests
{
	[Sample("Windowing", IsManualTest = true)]
	public sealed partial class Window_Resizing_Flicker : UserControl
	{
		public Window_Resizing_Flicker()
		{
			this.InitializeComponent();
		}
	}
}
