#nullable enable

using System;
using Windows.System;
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
			DisplayBlock = new TextBlock();
			SetFlowDirectionAndTextAlignment();

			_textBox = new WeakReference<TextBox>(textBox);
			_isPasswordBox = textBox is PasswordBox;
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
			SetDisplayBlockText(text);

			_textBoxExtension?.SetText(text);
		}

		internal void Select(int start, int length)
		{
			_textBoxExtension?.Select(start, length);
		}

		internal void SetFlowDirectionAndTextAlignment()
		{
			if (_textBox?.GetTarget() is not { } textBox)
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
			if (_textBox?.GetTarget() is { } textBox)
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
			var textBox = _textBox?.GetTarget();
			if (textBox != null)
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
		}

		internal void UpdateTextFromNative(string newText)
		{
			var textBox = _textBox?.GetTarget();
			if (textBox != null)
			{
				var oldText = textBox.Text; // preexisting text
				var oldSelection = SelectionBeforeKeyDown; // On Gtk, SelectionBeforeKeyDown just points to Selection, which is updated by SetTextNative, so we need to read it before SetTextNative.
				var modifiedText = textBox.ProcessTextInput(newText); // new text after BeforeTextChanging, TextChanging, DP callback, etc
				SetDisplayBlockText(modifiedText);
				if (modifiedText != newText)
				{
					SetTextNative(modifiedText);
					if (modifiedText == oldText)
					{
						// The native textbox received new input -> sent it to uno -> uno changed it back to the original value
						// In that case, SetTextNative will reset the selection and we need to reapply it.
						// You would think that this would break the selection direction (i.e. start to end or end to start), but in fact
						// the direction also breaks on WinUI itself (i.e. the new direction will always be start to end).
						DispatcherQueue.Main.TryEnqueue(() =>
						{
							// Enqueuing instead of synchronously selecting is problematic on Gtk, which fires the Changed event and then
							// changes the selection later. This will still fail (on Gtk) when pasting text or selecting some text then typing (replacing).
							Select(oldSelection.start, oldSelection.length);
						});
					}
				}
			}
		}

		public void UpdateMaxLength() => _textBoxExtension?.UpdateNativeView();

		private void SetDisplayBlockText(string text)
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
