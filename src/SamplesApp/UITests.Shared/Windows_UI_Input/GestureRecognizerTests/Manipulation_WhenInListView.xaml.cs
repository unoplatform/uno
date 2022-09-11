using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.GestureRecognizerTests
{
	[Sample(
		"Gesture Recognizer", "ListView",
		Description = "Automated test which validates if vertical scrolling of ListView works properly even if items does handles some manipulations.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class Manipulation_WhenInListView : Page
	{
		public Manipulation_WhenInListView()
		{
			this.InitializeComponent();

			ItemsSupportsTranslateX.ItemsSource = new[] { "#FF0000", "#FF8000", "#FFFF00", "#008000", "#0000FF", "#A000C0" };
		}
	}
}
