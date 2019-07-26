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

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[SampleControlInfo("ContentPresenter", "ContentPresenter_Changing_ContentTemplate", description: "ContentPresenter where ContentTemplate can be toggled between non-null and null. Content view should be visible when null.")]
	public sealed partial class ContentPresenter_Changing_ContentTemplate : UserControl
	{
		public ContentPresenter_Changing_ContentTemplate()
		{
			this.InitializeComponent();
		}

		private DataTemplate _stashedTemplate;
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			var oldTemplate = TargetContentPresenter.ContentTemplate;
			TargetContentPresenter.ContentTemplate = _stashedTemplate;
			_stashedTemplate = oldTemplate;
		}
	}
}
