using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
	[Sample("AutoSuggestBox")]
    public sealed partial class AutoSuggestBox_BitmapIcon : Page
    {
        public AutoSuggestBox_BitmapIcon()
        {
            this.InitializeComponent();
        }

		Random r = new Random();

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			RootPanel.Width = r.Next(200, 400);
		}
	}
}
