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

namespace Uno.UI.Samples.Content.UITests.ContentControlTestsControl
{
	[Sample("ContentControl", "ContentControl_Changing_ContentTemplate", Description: "ContentControl where ContentTemplate can be toggled between non-null and null. Content view should be visible when null.")]
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
