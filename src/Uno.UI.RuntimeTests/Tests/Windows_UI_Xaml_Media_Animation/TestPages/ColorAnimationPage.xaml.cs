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
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation.TestPages
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ColorAnimationPage : Page
	{
		public ColorAnimationPage()
		{
			this.InitializeComponent();
			this.Loaded += (s, e) => VisualStateManager.GoToState(this, "State1", true);
		}

		private void SetPageState(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, (sender as Button).Content.ToString(), true);
		}
	}
}
