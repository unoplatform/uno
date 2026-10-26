using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.MonochromaticOverlayPresenterTests;

[Sample(
	"Pickers",
	Name = "MonochromaticOverlayPresenter_Showcase",
	Description = "Port of WinUI's MonochromaticOverlayPresenterPage: the DatePicker/TimePicker flyouts draw the value under their "
		+ "highlight band through a MonochromaticOverlayPresenter, which must follow the value while a column scrolls.",
	IgnoreInSnapshotTests = true)]
public sealed partial class MonochromaticOverlayPresenter_Showcase : Page
{
	public MonochromaticOverlayPresenter_Showcase()
	{
		this.InitializeComponent();
	}
}
