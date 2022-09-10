using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls
{
	[SampleControlInfo("ContentControl", "ContentControl_NoTemplateNoContent")]
	public sealed partial class ContentControlNoTemplateNoContent : Page
	{
		public ContentControlNoTemplateNoContent()
		{
			this.InitializeComponent();
		}

		public void bntContentClear_click(object sender, RoutedEventArgs e)
		{
			CContentControl.Content = null;
			CContentControl.ContentTemplate = null;
			CContentControl.DataContext = null;
		}
	}
}
