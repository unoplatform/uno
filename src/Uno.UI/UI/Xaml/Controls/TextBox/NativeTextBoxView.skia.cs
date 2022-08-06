#nullable enable

using System;

using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal class NativeTextBoxView : ITextBoxView
	{
		private readonly ITextBoxViewExtension? _textBoxExtension;

		private readonly WeakReference<TextBox> _textBox;
		private readonly TextBlock _displayBlock = new TextBlock();
		private readonly bool _isPasswordBox;
		private bool _isPasswordRevealed;

		public NativeTextBoxView(TextBox textBox)
		{
			_textBox = new WeakReference<TextBox>(textBox);
			_isPasswordBox = textBox is PasswordBox;
			if (!ApiExtensibility.CreateInstance(this, out _textBoxExtension))
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(
						"No TextBoxView extension implementation is available " +
						"for this Skia target. Functionality will be limited.");
				}
			}
		}

		public TextBox? TextBox
		{
			get
			{
				if (_textBox.TryGetTarget(out var target))
				{
					return target;
				}
				return null;
			}
		}

		public int GetSelectionStart() => _textBoxExtension?.GetSelectionStart() ?? 0;

		public int GetSelectionLength() => _textBoxExtension?.GetSelectionLength() ?? 0;

		public UIElement Content => _displayBlock;

		public void SetText(string text)
		{
			// TODO: Inheritance hierarchy is wrong in Uno. PasswordBox shouldn't inherit TextBox.
			// This needs to be moved to PasswordBox if it's separated from TextBox.
			if (_isPasswordBox && !_isPasswordRevealed)
			{
				// TODO: PasswordChar isn't currently implemented. It should be used here when implemented.
				_displayBlock.Text = new string('•', text.Length);
			}
			else
			{
				_displayBlock.Text = text;
			}

			_textBoxExtension?.SetTextNative(text);
		}

		public void Select(int start, int length)
		{
			_textBoxExtension?.Select(start, length);
		}

		public void OnForegroundChanged(Brush brush)
		{
			_displayBlock.Foreground = brush;
			_textBoxExtension?.SetForeground(brush);
		}

		public void OnFocusStateChanged(FocusState focusState)
		{
			if (focusState != FocusState.Unfocused)
			{
				_displayBlock.Opacity = 0;
				_textBoxExtension?.StartEntry();

				var selectionStart = this.GetSelectionStart();

				if (selectionStart == 0)
				{
					int cursorPosition = selectionStart + TextBox?.Text?.Length ?? 0;

					_textBoxExtension?.Select(cursorPosition, 0);
				}
			}
			else
			{
				_textBoxExtension?.EndEntry();
				_displayBlock.Opacity = 1;
			}
		}

		public void SetIsPassword(bool isPassword)
		{
			_isPasswordRevealed = !isPassword;
			_textBoxExtension?.SetIsPassword(isPassword);
		}

		public void UpdateMaxLength() => _textBoxExtension?.UpdateNativeView();

		public void InvalidateLayout() => _textBoxExtension?.InvalidateLayout();

		internal void UpdateTextFromNative(string newText)
		{
			var textBox = _textBox?.GetTarget();
			if (textBox != null)
			{
				var text = textBox.ProcessTextInput(newText);
				if (text != newText)
				{
					SetText(text);
				}
			}
		}
	}
}
