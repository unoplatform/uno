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
using Windows.Media.Core;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement;

[SampleControlInfo("MediaPlayerElement", "ManualTest", ignoreInSnapshotTests: true, IsManualTest = true)]
public sealed partial class MediaPlayerElement_ManualTest : UserControl
{
	public MediaPlayerElement_ManualTest()
	{
		this.InitializeComponent();
	}

	private void Play_Click(object sender, RoutedEventArgs e)
	{
		mpe.MediaPlayer.Play();
	}
	private void Pause_Click(object sender, RoutedEventArgs e)
	{
		mpe.MediaPlayer.Pause();
	}
	private void Stop_Click(object sender, RoutedEventArgs e)
	{
#if !WINAPPSDK
		mpe.MediaPlayer.Stop();
#endif
	}
}
