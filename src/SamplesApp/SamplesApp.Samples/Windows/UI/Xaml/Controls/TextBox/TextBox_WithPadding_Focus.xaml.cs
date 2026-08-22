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

namespace UITests.Windows_UI_Xaml_Controls.TextBox
{
	[Sample(nameof(TextBox), nameof(TextBox_WithPadding_Focus))]
	public sealed partial class TextBox_WithPadding_Focus : UserControl
	{
		public TextBox_WithPadding_Focus()
		{
			this.InitializeComponent();
		}
	}
}
