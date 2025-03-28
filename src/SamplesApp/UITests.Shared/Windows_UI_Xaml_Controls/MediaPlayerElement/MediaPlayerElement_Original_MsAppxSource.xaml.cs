using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement
{
	[SampleControlInfo("MediaPlayerElement", "Original using a local ms-appx source", ignoreInSnapshotTests: true, description: "Video using a local ms-appx source for video and poster")]
	public sealed partial class MediaPlayerElement_Original_MsAppxSource : UserControl
	{
		public MediaPlayerElement_Original_MsAppxSource()
		{
			this.InitializeComponent();
		}
	}
}
