using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.ImageTests.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Private.Infrastructure;

namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[SampleControlInfo("Image", "ImageSourceFileUri", Description = "Image using local file path as Source, set as Uri")]
	public sealed partial class ImageSourceFileUri : UserControl
	{
		public ImageSourceFileUri()
		{
			this.InitializeComponent();

			DataContext = new ImageFilePathModel(UnitTestDispatcherCompat.From(this));
		}
	}
}
