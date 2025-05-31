﻿#nullable enable

using System;
using Windows.System;
using Microsoft.UI.Xaml.Documents;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Helpers;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	internal class TextBoxView
	{
		private readonly IOverlayTextBoxViewExtension? _overlayTextBoxViewExtension;

		private readonly ManagedWeakReference _textBox;
		private bool _isPasswordRevealed;
		private readonly bool _isSkiaTextBox = !FeatureConfiguration.TextBox.UseOverlayOnSkia;
		private static readonly bool _useInvisibleNativeTextView = OperatingSystem.IsBrowser() || DeviceTargetHelper.IsUIKit();

		// On Windows, \u25CF is used as password character.
		// However, this character can't be retrieved on Android (doesn't exist in any system font) and on some browser/OS combinations.
		// We use \u2022 instead, which is already the one normally used by Android and all the major browsers.
		// See https://github.com/mozilla/gecko-dev/blob/1d4c27f9f166ce6e967fb0e8c8d6e0795dbbd12e/widget/android/nsLookAndFeel.cpp#L441
		internal static readonly char PasswordChar = OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser() ? '\u2022' : '\u25CF';

		public TextBoxView(TextBox textBox)
		{
			_textBox = WeakReferencePool.RentWeakReference(this, textBox);
			IsPasswordBox = textBox is PasswordBox;

			DisplayBlock = new TextBlock { MinWidth = InlineCollection.CaretThickness, IsTextBoxDisplay = true };
			SetFlowDirectionAndTextAlignment();

			if ((!_isSkiaTextBox || _useInvisibleNativeTextView) && !ApiExtensibility.CreateInstance(this, out _overlayTextBoxViewExtension))
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(
						"No TextBoxView implementation is available " +
						"for this Skia target. Functionality will be limited.");
				}
			}
		}

		internal bool IsPasswordBox { get; }

		public (int start, int length) SelectionBeforeKeyDown =>
			(_overlayTextBoxViewExtension?.GetSelectionStartBeforeKeyDown() ?? 0, _overlayTextBoxViewExtension?.GetSelectionLengthBeforeKeyDown() ?? 0);

		internal IOverlayTextBoxViewExtension? Extension => _overlayTextBoxViewExtension;

		public TextBox? TextBox => !_textBox.IsDisposed ? _textBox.Target as TextBox : null;

		internal int GetSelectionStart() => _overlayTextBoxViewExtension?.GetSelectionStart() ?? 0;

		internal int GetSelectionLength() => _overlayTextBoxViewExtension?.GetSelectionLength() ?? 0;

		public TextBlock DisplayBlock { get; }

		internal void SetTextNative(string text)
		{
			UpdateDisplayBlockText(text);

			_overlayTextBoxViewExtension?.SetText(text);
		}

		internal void Select(int start, int length)
		{
			_overlayTextBoxViewExtension?.Select(start, length);
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
			_overlayTextBoxViewExtension?.UpdateProperties();
		}

		internal void OnSelectionHighlightColorChanged(SolidColorBrush brush)
		{
			DisplayBlock.SelectionHighlightColor = brush;
			_overlayTextBoxViewExtension?.UpdateProperties();
		}

		internal void OnFocusStateChanged(FocusState focusState)
		{
			if (_isSkiaTextBox && _useInvisibleNativeTextView)
			{
				// We don't care about actual entry here, just making
				// the password manager autocompletion button appear.
				if (focusState != FocusState.Unfocused)
				{
					_overlayTextBoxViewExtension?.StartEntry();
				}
				else
				{
					_overlayTextBoxViewExtension?.EndEntry();
				}
			}
			else if (!_isSkiaTextBox)
			{
				if (focusState != FocusState.Unfocused)
				{
					DisplayBlock.Opacity = 0;
					_overlayTextBoxViewExtension?.StartEntry();

					var selectionStart = this.GetSelectionStart();

					if (selectionStart == 0)
					{
						int cursorPosition = selectionStart + TextBox?.Text?.Length ?? 0;

						_overlayTextBoxViewExtension?.Select(cursorPosition, 0);
					}
				}
				else
				{
					_overlayTextBoxViewExtension?.EndEntry();
					DisplayBlock.Opacity = 1;
				}
			}
		}

		internal void UpdateTheme() => _overlayTextBoxViewExtension?.UpdateProperties();

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
			_overlayTextBoxViewExtension?.SetPasswordRevealState(revealState);
			if (TextBox is { } textBox)
			{
				UpdateDisplayBlockText(textBox.Text);
			}
		}

		internal void UpdateTextFromNative(string newText)
		{
			if (TextBox is { } textBox)
			{
				var oldText = textBox.Text; // preexisting text
				var oldSelection = SelectionBeforeKeyDown; // On Gtk, SelectionBeforeKeyDown just points to Selection, which is updated by SetTextNative, so we need to read it before SetTextNative.
				var modifiedText = textBox.ProcessTextInput(newText); // new text after BeforeTextChanging, TextChanging, DP callback, etc
				UpdateDisplayBlockText(modifiedText);
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

		public void UpdateMaxLength() => _overlayTextBoxViewExtension?.UpdateNativeView();

		private void UpdateDisplayBlockText(string text)
		{
			// TODO: Inheritance hierarchy is wrong in Uno. PasswordBox shouldn't inherit TextBox.
			// This needs to be moved to PasswordBox if it's separated from TextBox.
			if (IsPasswordBox && !_isPasswordRevealed)
			{
				// TODO: PasswordChar isn't currently implemented. It should be used here when implemented.
				DisplayBlock.Text = new string(PasswordChar, text.Length);
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

		internal void UpdateProperties() => _overlayTextBoxViewExtension?.UpdateProperties();
	}
}
