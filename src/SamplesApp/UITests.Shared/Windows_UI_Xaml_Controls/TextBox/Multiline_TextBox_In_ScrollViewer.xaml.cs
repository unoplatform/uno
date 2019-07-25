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

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[SampleControlInfoAttribute("TextBox", "Multiline_TextBox_In_ScrollViewer", typeof(Uno.UI.Samples.Presentation.SamplePages.TextBoxViewModel))]
	public sealed partial class Multiline_TextBox_In_ScrollViewer : UserControl
	{
		public Multiline_TextBox_In_ScrollViewer()
		{
			this.InitializeComponent();
		}
	}
}
