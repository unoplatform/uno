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
	[Sample("ContentControl", "ContentControl_SetNull", typeof(Presentation.SamplePages.ContentControlTestViewModel))]
	public sealed partial class ContentControl_SetNull : UserControl
	{
		public ContentControl_SetNull()
		{
			this.InitializeComponent();
		}
	}
}
