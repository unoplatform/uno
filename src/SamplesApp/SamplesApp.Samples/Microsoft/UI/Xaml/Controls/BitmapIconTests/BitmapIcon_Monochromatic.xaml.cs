using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using System.Windows.Input;
using Uno.UI.Common;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Controls.BitmapIconTests;

[Sample("Icons")]
public sealed partial class BitmapIcon_Monochromatic : Page
{
	public BitmapIcon_Monochromatic()
	{
		this.InitializeComponent();
	}
}
