using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UITests.Windows_UI_Xaml_Controls.TextBox
{
	[Sample("TextBox", Description = "TextBox's content should not be affected by parent TextBlock styles")]
	public sealed partial class TextBox_ImplicitParentTextBlockStyle : UserControl
	{
		public TextBox_ImplicitParentTextBlockStyle()
		{
			InitializeComponent();
		}
	}
}
