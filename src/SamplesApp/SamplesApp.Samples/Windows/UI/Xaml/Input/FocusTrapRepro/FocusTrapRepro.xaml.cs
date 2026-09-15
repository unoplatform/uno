using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Input.FocusTrapRepro
{
	[Sample("Focus", Name = "FocusTrapRepro", Description = "Tab/Shift+Tab cycling between Uno controls and the browser (WASM a11y focus).")]
	public sealed partial class FocusTrapRepro : Page
	{
		public FocusTrapRepro()
		{
			this.InitializeComponent();
		}
	}
}
