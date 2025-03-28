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

namespace Uno.UI.Samples.UITests.ImageBrushTestControl
{
	[SampleControlInfo("Brushes", "ImageBrushWithLateSource")]
	public sealed partial class ImageBrushWithLateSource : UserControl
	{
		public ImageBrushWithLateSource()
		{
			this.InitializeComponent();

			DataContext = new ImageWithLateSourceViewModel(UnitTestDispatcherCompat.From(this));
		}
	}
}
