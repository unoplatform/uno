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
	[SampleControlInfo("MediaPlayerElement", "Using .mp3 (Audio only)", description: "MediaPlayerElement test using .mp3 (Audio only) with PosterSource", IgnoreInSnapshotTests = true)]
	public sealed partial class MediaPlayerElement_Mp3_Extension : Page
	{
		public MediaPlayerElement_Mp3_Extension()
		{
			this.InitializeComponent();
		}
	}
}
