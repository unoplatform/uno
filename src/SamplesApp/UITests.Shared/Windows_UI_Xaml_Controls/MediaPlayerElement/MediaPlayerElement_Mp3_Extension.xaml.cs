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
	[Sample("MediaPlayerElement", "Using .mp3 (Audio only)", Description: "MediaPlayerElement test using .mp3 (Audio only) with PosterSource", IgnoreInSnapshotTests = true)]
	public sealed partial class MediaPlayerElement_Mp3_Extension : Page
	{
		public MediaPlayerElement_Mp3_Extension()
		{
			this.InitializeComponent();
		}
	}
}
