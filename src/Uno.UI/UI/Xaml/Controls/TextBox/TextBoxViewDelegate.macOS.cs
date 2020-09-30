using Foundation;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppKit;
using Windows.UI.Core;
using System.Threading.Tasks;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	internal class TextBoxViewDelegate : NSTextFieldDelegate
	{
		private readonly WeakReference<TextBox> _textBox;
		private readonly WeakReference<TextBoxView> _textBoxView;

		public TextBoxViewDelegate(WeakReference<TextBox> textbox, WeakReference<TextBoxView> textBoxView)
		{
			_textBox = textbox;
			_textBoxView = textBoxView;
		}

		public bool IsKeyboardHiddenOnEnter { get; set; }

		public override bool DoCommandBySelector(NSControl control, NSTextView textView, Selector commandSelector)
		{
			switch (commandSelector.Name)
			{
				case "insertNewline:":
					if (_textBox.TryGetTarget(out var textBox) && textBox.AcceptsReturn)
					{
						textView.InsertText((NSString)"\n");
						return true;
					}

					break;
			}

			return false;
		}

		public override bool TextShouldBeginEditing(NSControl control, NSText fieldEditor)
		{
			return !_textBox.GetTarget()?.IsReadOnly ?? false;
		}

		public override void EditingBegan(NSNotification notification)
		{
			_textBox.GetTarget()?.Focus(FocusState.Pointer);
		}

		public override void Changed(NSNotification notification)
		{
			_textBoxView.GetTarget()?.OnChanged();
		}
	}
}
