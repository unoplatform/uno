#if XAMARIN
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Input;
using Windows.System;
#if XAMARIN_ANDROID
using Android.Views;
#elif __IOS__
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.DependencyObject;
#endif

namespace Uno.UI.Behaviors
{
	public class TextBoxBehavior
	{
#region Attached property: NextControl

		public static object GetNextControl(TextBox obj)
		{
			return obj.GetValue(NextControlProperty);
		}
		
		public static readonly DependencyProperty NextControlProperty =
			DependencyProperty.RegisterAttached("NextControl", typeof(object), typeof(TextBoxBehavior),
			new PropertyMetadata( null,  new PropertyChangedCallback(OnNextControlChanged)));

		private static void OnNextControlChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var textBox = sender as TextBox;

			if (textBox == null)
				return;

			textBox.RegisterLoadActions(() => RegisterTextChanged(textBox), () => UnregisterTextChanged(textBox));
		}

		private static void RegisterTextChanged(TextBox textBox)
		{
			textBox.TextChanged += OnTargetTextChanged;
			textBox.KeyUp += OnKeyUp;
		}

		private static void UnregisterTextChanged(TextBox textBox)
		{
			textBox.TextChanged -= OnTargetTextChanged;
			textBox.KeyUp -= OnKeyUp;
		}

		private static void OnTargetTextChanged(object sender, TextChangedEventArgs e)
		{
			var textBox = sender as TextBox;

			textBox?.SetValue(TextProperty, textBox.Text);
		}

		private static void OnKeyUp(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				var textBox = sender as TextBox;
				var nextControl = GetNextControl(textBox) as View;
#if XAMARIN_ANDROID
				nextControl?.RequestFocus();
#elif __IOS__ || __MACOS__
				nextControl?.BecomeFirstResponder();
#endif
				e.Handled = true;
			}
		}

#endregion

#region Attached property: Text

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.RegisterAttached("Text", typeof(string), typeof(TextBoxBehavior), new PropertyMetadata(default(string), OnTextChanged));

		private static void OnTextChanged(object d, DependencyPropertyChangedEventArgs e)
		{
			var textBox = d as TextBox;
			
			var newValueAsString = (string)e.NewValue;

			if (newValueAsString != null)
				textBox.Text = newValueAsString;
		} 

#endregion
	}
}
#endif
