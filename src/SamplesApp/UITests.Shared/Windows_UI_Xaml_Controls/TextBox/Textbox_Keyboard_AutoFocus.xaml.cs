using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace GenericApp.Views.Content.UITests.TextBoxControl
{
	[SampleControlInfoAttribute("TextBox", "Textbox_Keyboard_AutoFocused", typeof(Uno.UI.Samples.Presentation.SamplePages.TextBoxViewModel))]
	public sealed partial class Textbox_Keyboard_AutoFocus : UserControl
    {
        public Textbox_Keyboard_AutoFocus()
        {
            this.InitializeComponent();

			var textbox = this.FindName("FocusedTextbox") as TextBox;
			textbox.Loaded += RequestTextboxFocus;
		}

#if XAMARIN_ANDROID
		private void RequestTextboxFocus(object sender, RoutedEventArgs e)
		{
			var textbox = sender as TextBox;
			textbox.RequestFocus(Android.Views.FocusSearchDirection.Up, null);
		}
#elif XAMARIN_IOS
		private void RequestTextboxFocus(object sender, RoutedEventArgs e)
		{
			var textbox = sender as TextBox;
			textbox.BecomeFirstResponder();
        }
#else
		private void RequestTextboxFocus(object sender, RoutedEventArgs e)
		{
			var textbox = sender as TextBox;
			textbox.Focus(FocusState.Programmatic);
		}
#endif
	}
}
