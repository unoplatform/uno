using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.FocusTests
{
	[Sample("Focus", IsManualTest = true)]
	public sealed partial class Focus_WASM_Tab : UserControl
	{
		public Focus_WASM_Tab()
		{
			this.InitializeComponent();

			Microsoft.UI.Xaml.Input.FocusManager.GotFocus += FocusManager_GotFocus;
		}

		private void FocusManager_GotFocus(object sender, Microsoft.UI.Xaml.Input.FocusManagerGotFocusEventArgs e)
		{
			this.TxtCurrentFocused.Text = (e.NewFocusedElement as FrameworkElement)?.Name ?? "<none>";
		}
	}
}
