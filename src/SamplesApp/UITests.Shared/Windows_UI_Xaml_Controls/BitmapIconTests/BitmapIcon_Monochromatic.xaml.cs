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
using System.Windows.Input;
using Uno.UI.Common;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Controls.BitmapIconTests;

[Sample("Icons")]
public sealed partial class BitmapIcon_Monochromatic : Page
{
	public BitmapIcon_Monochromatic()
	{
		this.InitializeComponent();
	}
}
