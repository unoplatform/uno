using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement;

[Sample("MediaPlayerElement", Description = "Test .avi video", IgnoreInSnapshotTests = true)]
public sealed partial class MediaPlayerElement_Avi_Extension : UserControl
{
	public MediaPlayerElement_Avi_Extension()
	{
		this.InitializeComponent();
	}
}
