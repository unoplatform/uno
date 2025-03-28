using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement;

[Sample("MediaPlayerElement", Description = "Test .flv video", IgnoreInSnapshotTests = true)]
public sealed partial class MediaPlayerElement_Flv_Extension : UserControl
{
	public MediaPlayerElement_Flv_Extension()
	{
		this.InitializeComponent();
	}
}
