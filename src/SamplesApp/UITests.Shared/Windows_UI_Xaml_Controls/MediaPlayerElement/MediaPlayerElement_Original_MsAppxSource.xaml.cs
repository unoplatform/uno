using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement
{
	[Sample("MediaPlayerElement", "Original using a local ms-appx source", ignoreInSnapshotTests: true, description: "Video using a local ms-appx source for video and poster")]
	public sealed partial class MediaPlayerElement_Original_MsAppxSource : UserControl
	{
		public MediaPlayerElement_Original_MsAppxSource()
		{
			this.InitializeComponent();
		}
	}
}
