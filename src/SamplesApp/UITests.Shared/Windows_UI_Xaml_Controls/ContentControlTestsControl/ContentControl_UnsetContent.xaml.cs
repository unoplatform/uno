using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace GenericApp.Views.Content.UITests.ContentControlTestsControl
{
	[SampleControlInfo("ContentControl", "ContentControl_UnsetContent", typeof(ContentControlTestViewModel))]
	public sealed partial class ContentControl_UnsetContent : UserControl
	{
		public ContentControl_UnsetContent()
		{
			this.InitializeComponent();
		}
	}
}
