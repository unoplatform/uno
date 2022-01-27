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

namespace UITests.Windows_UI_Xaml_Controls.TextBox
{
	[SampleControlInfo(nameof(TextBox), nameof(TextBox_WithPadding_Focus))]
	public sealed partial class TextBox_WithPadding_Focus : UserControl
	{
		public TextBox_WithPadding_Focus()
		{
			this.InitializeComponent();
		}
	}
}
