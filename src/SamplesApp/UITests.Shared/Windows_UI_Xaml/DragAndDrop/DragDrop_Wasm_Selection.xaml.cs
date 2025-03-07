using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop;

#if __WASM__
[Sample(
	Description = PageDescription,
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
#endif
public sealed partial class DragDrop_Wasm_Selection : UserControl
{
	private const string PageDescription =
		"This is wasm-only manual test verifying against #18854. " +
		"While selection covers both TextBlocks, try to drag the thumb of the Slider and ScrollBar, " +
		"there should be no drag preview."; // see linked issue for a gif of what should not happen.

	public DragDrop_Wasm_Selection()
	{
		this.InitializeComponent();
	}
}
