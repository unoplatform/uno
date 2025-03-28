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

namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[SampleControlInfo("Image", "EmptyImageFixedWidth", Description = "EmptyImageFixedWidth - the Aquamarine-coloured StarStackPanel below should stretch to the fixed Width given by the Image control, even though the Image is empty.")]
	public sealed partial class EmptyImageFixedWidth : UserControl
	{
		public EmptyImageFixedWidth()
		{
			this.InitializeComponent();
		}
	}
}
