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

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock", Description = "Changing Foreground while synchronously setting TextBlock collapsed should show the new Foreground correctly when it becomes visible")]
	public sealed partial class TextBlock_Foreground_While_Collapsed : UserControl
	{
		public TextBlock_Foreground_While_Collapsed()
		{
			this.InitializeComponent();
		}

		private void ChangeText(object sender, RoutedEventArgs args)
		{

			if (FunnyTextBlock.Visibility == Visibility.Visible && (FunnyTextBlock.Foreground as SolidColorBrush).Color == Colors.Blue)
			{
				FunnyTextBlock.Foreground = new SolidColorBrush(Colors.Black);
			}
			else if (FunnyTextBlock.Visibility == Visibility.Visible)
			{
				FunnyTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
				FunnyTextBlock.Visibility = Visibility.Collapsed;
			}
			else
			{
				FunnyTextBlock.Visibility = Visibility.Visible;
			}
		}
	}
}
