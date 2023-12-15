using Uno.UI.Samples.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample("Pointers", IsManualTest = true, Description = "This sample tests a GTK issue where clicking on a native element (such as TextBox) would send incorrect pointer coordinates, leading to pointer events being fired on incorrect controls.")]
	public sealed partial class Pointer_Coordinates : Page
	{
		public Pointer_Coordinates()
		{
			InitializeComponent();
		}

		private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			eventBlock.Text += $"{sender}: PointerPressed {e.GetCurrentPoint(null).Position}\n";
		}
	}
}
