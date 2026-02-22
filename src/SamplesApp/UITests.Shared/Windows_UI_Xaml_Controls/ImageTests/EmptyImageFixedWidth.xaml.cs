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

namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[Sample("Image", Name = "EmptyImageFixedWidth", Description = "EmptyImageFixedWidth - the Aquamarine-coloured StarStackPanel below should stretch to the fixed Width given by the Image control, even though the Image is empty.")]
	public sealed partial class EmptyImageFixedWidth : UserControl
	{
		public EmptyImageFixedWidth()
		{
			this.InitializeComponent();
		}
	}
}
