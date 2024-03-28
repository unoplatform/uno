using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.FocusTests
{
	[Sample("Focus", IsManualTest = true)]
	public sealed partial class Focus_WASM_Tab : UserControl
	{
		public Focus_WASM_Tab()
		{
			this.InitializeComponent();

			Windows.UI.Xaml.Input.FocusManager.GotFocus += FocusManager_GotFocus;
		}

		private void FocusManager_GotFocus(object sender, Windows.UI.Xaml.Input.FocusManagerGotFocusEventArgs e)
		{
			this.TxtCurrentFocused.Text = (e.NewFocusedElement as FrameworkElement)?.Name ?? "<none>";
		}
	}
}
