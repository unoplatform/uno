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
	[Sample("MediaPlayerElement", Name = "Multiple", Description = "Test for dynamic Multiple sources", IgnoreInSnapshotTests = true)]
	public sealed partial class MediaPlayerElement_Multiple : UserControl
	{
		public MediaPlayerElement_Multiple()
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
		private void UpdateButtonTwo_Click(object sender, RoutedEventArgs e)
		{
			var uri = SourceTwoTextBox.Text;

			if (!string.IsNullOrEmpty(uri))
			{
				MpeTwo.Source = MediaSource.CreateFromUri(new Uri(uri));
			}
			else
			{
				MpeTwo.Source = null;
			}
		}
	}
}
