using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace GenericApp.Views.Content.UITests.ContentControlTestsControl
{
	[Sample("ContentControl", Name = "ContentControl_UnsetContent", ViewModelType = typeof(ContentControlTestViewModel))]
	public sealed partial class ContentControl_UnsetContent : UserControl
	{
		public ContentControl_UnsetContent()
		{
			this.InitializeComponent();
		}
	}
}
