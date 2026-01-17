using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GenericApp.Views.Content.UITests.TextBoxControl
{
	[Sample("TextBox", "Textbox_Keyboard_AutoFocused", IgnoreInSnapshotTests: true /*Cursor blinks in TextBox*/)]
	public sealed partial class Textbox_Keyboard_AutoFocus : UserControl
	{
		public Textbox_Keyboard_AutoFocus()
		{
			this.InitializeComponent();

			var textbox = FindName("FocusedTextbox") as TextBox;
			textbox.Loaded += RequestTextboxFocus;
		}

#if __ANDROID__
		private void RequestTextboxFocus(object sender, RoutedEventArgs e)
		{
			var textbox = sender as TextBox;
			textbox.RequestFocus(Android.Views.FocusSearchDirection.Up, null);
		}
#elif __APPLE_UIKIT__
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
