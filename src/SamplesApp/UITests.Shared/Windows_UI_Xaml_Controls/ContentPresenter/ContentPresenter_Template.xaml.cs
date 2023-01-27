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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[SampleControlInfo("ContentPresenter", "ContentPresenter_Template")]
	public sealed partial class ContentPresenter_Template : UserControl
	{
		public ContentPresenter_Template()
		{
			this.InitializeComponent();

			DataContext = nameof(DataContext);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (rootContent.Content == null)
			{
				rootContent.Content = 42;
			}
			else
			{
				rootContent.Content = null;
			}

			if (rootContent2.Content == null)
			{
				rootContent2.Content = 42;
			}
			else
			{
				rootContent2.Content = null;
			}
		}
	}
}
