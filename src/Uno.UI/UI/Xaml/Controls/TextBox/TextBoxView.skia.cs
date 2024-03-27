#nullable enable

using System;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;
using Microsoft.UI.Xaml.Media;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	internal class TextBoxView
	{
		private readonly IOverlayTextBoxViewExtension? _textBoxExtension;

		private readonly WeakReference<TextBox> _textBox;
		private readonly bool _isPasswordBox;
		private bool _isPasswordRevealed;
		private readonly bool _isSkiaTextBox = !FeatureConfiguration.TextBox.UseOverlayOnSkia;

		public TextBoxView(TextBox textBox)
		{
			_textBox = new WeakReference<TextBox>(textBox);
			_isPasswordBox = textBox is PasswordBox;

			DisplayBlock = new TextBlock();
			SetFlowDirectionAndTextAlignment();

			if (!_isSkiaTextBox && !ApiExtensibility.CreateInstance(this, out _textBoxExtension))
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(
						"No TextBoxView implementation is available " +
						"for this Skia target. Functionality will be limited.");
				}
			}
		}

		public (int start, int length) SelectionBeforeKeyDown =>
			(_textBoxExtension?.GetSelectionStartBeforeKeyDown() ?? 0, _textBoxExtension?.GetSelectionLengthBeforeKeyDown() ?? 0);

		internal IOverlayTextBoxViewExtension? Extension => _textBoxExtension;

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

		internal int GetSelectionStart() => _textBoxExtension?.GetSelectionStart() ?? 0;

		internal int GetSelectionLength() => _textBoxExtension?.GetSelectionLength() ?? 0;

		public TextBlock DisplayBlock { get; }

		internal void SetTextNative(string text)
		{
			UpdateDisplayBlockText(text);

			_textBoxExtension?.SetText(text);
		}

		internal void Select(int start, int length)
		{
			_textBoxExtension?.Select(start, length);
		}

		internal void SetFlowDirectionAndTextAlignment()
		{
			if (TextBox is not { } textBox)
			{
				return;
			}

			var flowDirection = textBox.FlowDirection;
			var textAlignment = textBox.TextAlignment;
			if (flowDirection == FlowDirection.RightToLeft)
			{
				textAlignment = textAlignment switch
				{
					TextAlignment.Left => TextAlignment.Right,
					TextAlignment.Right => TextAlignment.Left,
					_ => textAlignment,
				};
			}

			DisplayBlock.FlowDirection = flowDirection;
			DisplayBlock.TextAlignment = textAlignment;
		}

		internal void SetWrapping()
		{
			if (TextBox is { } textBox)
			{
				DisplayBlock.TextWrapping = textBox.TextWrapping;
			}
		}

		internal void OnForegroundChanged(Brush brush)
		{
			DisplayBlock.Foreground = brush;
			_textBoxExtension?.UpdateProperties();
		}

		internal void OnSelectionHighlightColorChanged(SolidColorBrush brush)
		{
			DisplayBlock.SelectionHighlightColor = brush;
			_textBoxExtension?.UpdateProperties();
		}

		internal void OnFocusStateChanged(FocusState focusState)
		{
			if (_isSkiaTextBox)
			{
				return;
			}

			if (focusState != FocusState.Unfocused)
			{
				DisplayBlock.Opacity = 0;
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
				DisplayBlock.Opacity = 1;
			}
		}

		internal void UpdateFont()
		{
			if (TextBox is { } textBox)
			{
				DisplayBlock.FontFamily = textBox.FontFamily;
				DisplayBlock.FontSize = textBox.FontSize;
				DisplayBlock.FontStyle = textBox.FontStyle;
				DisplayBlock.FontStretch = textBox.FontStretch;
				DisplayBlock.FontWeight = textBox.FontWeight;
			}
			// TODO: Propagate font family to the native InputWidget via _textBoxExtension.
		}

		internal void SetPasswordRevealState(PasswordRevealState revealState)
		{
			_isPasswordRevealed = revealState == PasswordRevealState.Revealed;
			_textBoxExtension?.SetPasswordRevealState(revealState);
			if (TextBox is { } textBox)
			{
				UpdateDisplayBlockText(textBox.Text);
			}
		}

		internal void UpdateTextFromNative(string newText)
		{
			if (TextBox is { } textBox)
			{
				var text = textBox.ProcessTextInput(newText);
				UpdateDisplayBlockText(text);
				if (text != newText)
				{
					SetTextNative(text);
				}
			}
		}

		public void UpdateMaxLength() => _textBoxExtension?.UpdateNativeView();

		private void UpdateDisplayBlockText(string text)
		{
			// TODO: Inheritance hierarchy is wrong in Uno. PasswordBox shouldn't inherit TextBox.
			// This needs to be moved to PasswordBox if it's separated from TextBox.
			if (_isPasswordBox && !_isPasswordRevealed)
			{
				// TODO: PasswordChar isn't currently implemented. It should be used here when implemented.
				DisplayBlock.Text = new string('●', text.Length);
			}
			else
			{
				DisplayBlock.Text = text;
			}

			if (_isSkiaTextBox)
			{
				TextBox?.ContentElement?.InvalidateMeasure();
				TextBox?.UpdateLayout();
			}
		}
	}
}
