using Foundation;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppKit;
using Windows.UI.Core;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
	public class SecureTextBoxViewDelegate : NSTextFieldDelegate
	{
		private readonly WeakReference<TextBox> _passwordBox;
		private readonly WeakReference<SecureTextBoxView> _securedTextBoxView;

		public SecureTextBoxViewDelegate(WeakReference<TextBox> passwordBox, WeakReference<SecureTextBoxView> securedTextBoxView)
		{
			_passwordBox = passwordBox;
			_securedTextBoxView = securedTextBoxView;
		}

		public bool IsKeyboardHiddenOnEnter { get; set; }

		public override bool TextShouldBeginEditing(NSControl control, NSText fieldEditor)
		{
			return !_passwordBox.GetTarget()?.IsReadOnly ?? false;
		}

		public override void EditingBegan(NSNotification notification)
		{
			_passwordBox.GetTarget()?.Focus(FocusState.Pointer);
		}

		public override void Changed(NSNotification notification)
		{
			_securedTextBoxView.GetTarget()?.OnChanged();
		}
	}
}
