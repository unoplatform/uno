using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement
{
	[Sample("MediaPlayerElement", "Sources", description: "Test for dynamic sources", IgnoreInSnapshotTests = true)]
	public sealed partial class MediaPlayerElement_Sources : UserControl
	{
		public MediaPlayerElement_Sources()
		{
			this.InitializeComponent();
		}

		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			var uri = SourceTextBox.Text;

			if (!string.IsNullOrEmpty(uri))
			{
				Mpe.Source = MediaSource.CreateFromUri(new Uri(uri));
			}
			else
			{
				Mpe.Source = null;
			}
		}
	}
}
