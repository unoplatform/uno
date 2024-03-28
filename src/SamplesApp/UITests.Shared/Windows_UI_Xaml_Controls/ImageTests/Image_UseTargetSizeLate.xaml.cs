using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[SampleControlInfo("Image")]
	public sealed partial class Image_UseTargetSizeLate : UserControl
	{
		public Image_UseTargetSizeLate()
		{
			this.InitializeComponent();
		}

		private void LoadNewImage(object sender, RoutedEventArgs args)
		{
			var child = new ContentControl
			{
				Name = "secondControl",
				Content = "dummy",
				Background = new SolidColorBrush(Colors.Blue),
				Width = 100,
				Height = 100,
				Template = firstControl.Template
			};

			hostPanel.Children.Add(child);
		}
	}
}
