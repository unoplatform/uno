using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement;

[Sample("MediaPlayerElement", Description = "Test .mkv video", IgnoreInSnapshotTests = true)]
public sealed partial class MediaPlayerElement_Mkv_Extension : UserControl
{
	public MediaPlayerElement_Mkv_Extension()
	{
		this.InitializeComponent();
	}
}
