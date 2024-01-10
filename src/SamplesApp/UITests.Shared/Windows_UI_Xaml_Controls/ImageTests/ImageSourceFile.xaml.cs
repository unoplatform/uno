using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.ImageTests.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;


namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[SampleControlInfo("Image", "ImageSourceFile", ignoreInSnapshotTests: true /*Local file path shown, including app folder, which is different for each run*/)]
	public sealed partial class ImageSourceFile : UserControl
	{
		public ImageSourceFile()
		{
			this.InitializeComponent();

			DataContext = new ImageFilePathModel(Dispatcher);
		}
	}
}
