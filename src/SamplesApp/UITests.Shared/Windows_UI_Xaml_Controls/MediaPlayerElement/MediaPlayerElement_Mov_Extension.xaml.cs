using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement;

[Sample("MediaPlayerElement", Description = "Test .mov video", IgnoreInSnapshotTests = true)]
public sealed partial class MediaPlayerElement_Mov_Extension : UserControl
{
	public MediaPlayerElement_Mov_Extension()
	{
		this.InitializeComponent();
	}
}
