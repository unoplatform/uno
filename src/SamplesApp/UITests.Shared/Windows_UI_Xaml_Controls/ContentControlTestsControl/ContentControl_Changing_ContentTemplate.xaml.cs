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

namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	[SampleControlInfo("ContentControl", "ContentControl_Changing_ContentTemplate", description: "ContentControl where ContentTemplate can be toggled between non-null and null. Content view should be visible when null.")]
	public sealed partial class ContentControl_Changing_ContentTemplate : UserControl
	{
		public ContentControl_Changing_ContentTemplate()
		{
			this.InitializeComponent();
		}

		private DataTemplate _stashedContentControlTemplate;
		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			var oldTemplate = TargetContentControl.ContentTemplate;
			TargetContentControl.ContentTemplate = _stashedContentControlTemplate;
			_stashedContentControlTemplate = oldTemplate;
		}
	}
}
