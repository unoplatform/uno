#nullable enable

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
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	internal class TextBoxView
	{
		private readonly IOverlayTextBoxViewExtension? _overlayTextBoxViewExtension;

		private readonly ManagedWeakReference _host;
		private bool _isPasswordRevealed;
		private static readonly bool _useInvisibleNativeTextView = OperatingSystem.IsBrowser() || DeviceTargetHelper.IsUIKit();

		public TextBoxView(ITextBoxViewHost host)
		{
			_host = WeakReferencePool.RentWeakReference(this, host);
			IsPasswordBox = host is PasswordBox;

			DisplayBlock = new TextBlock
			{
				MinWidth = TextBlock.CaretThickness,
				Style = null, // Prevent inheriting TextBlock styles
				// TODO Uno: OwningTextBox is still typed as TextBox; RichEditBox hosting is generalized in a later phase.
				OwningTextBox = host as TextBox,
				IsSpellCheckEnabled = host.IsSpellCheckEnabled
			};

			// The DisplayBlock is an internal rendering detail; its text content
			// is already exposed via the TextBox's IValueProvider.  Keep it out of
			// the Content/Control UIA views so it doesn't create a spurious child
			AutomationProperties.SetAccessibilityView(DisplayBlock, AccessibilityView.Raw);

			SetFlowDirection();
			SetTextAlignment();

			if (_useInvisibleNativeTextView && !ApiExtensibility.CreateInstance(this, out _overlayTextBoxViewExtension))
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

		internal ITextBoxViewHost? Host => _host.TryGetTarget<ITextBoxViewHost>(out var host) ? host : null;

		public TextBox? TextBox => Host as TextBox;

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

		internal void SetFlowDirection()
		{
			if (Host is not { } host)
			{
				return;
			}
			DisplayBlock.FlowDirection = host.FlowDirection;
		}

		internal void SetWrapping()
		{
			if (Host is { } host)
			{
				DisplayBlock.TextWrapping = host.TextWrapping;
			}
		}

		/// <remarks>
		/// Note that we are intentionally *not* setting the Foreground of DisplayBlock here, as it is inherited from
		/// ContentElement in TextBox template. If it was set explicitly, the inheritance would no longer apply.
		/// </remarks>
		internal void OnForegroundChanged(Brush brush) => _overlayTextBoxViewExtension?.UpdateProperties();

		internal void OnSelectionHighlightColorChanged(SolidColorBrush brush)
		{
			DisplayBlock.SelectionHighlightColor = brush;
			_overlayTextBoxViewExtension?.UpdateProperties();
		}

		internal void OnFocusStateChanged(FocusState focusState)
		{
			if (_useInvisibleNativeTextView)
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
		}

		internal void UpdateTheme() => _overlayTextBoxViewExtension?.UpdateProperties();

		internal void UpdateFont()
		{
			if (Host is { } host)
			{
				DisplayBlock.FontFamily = host.FontFamily;
				DisplayBlock.FontSize = host.FontSize;
				DisplayBlock.FontStyle = host.FontStyle;
				DisplayBlock.FontStretch = host.FontStretch;
				DisplayBlock.FontWeight = host.FontWeight;
			}
			// TODO: Propagate font family to the native InputWidget via _textBoxExtension.
		}

		internal void SetPasswordRevealState(PasswordRevealState revealState)
		{
			_isPasswordRevealed = revealState == PasswordRevealState.Revealed;
			_overlayTextBoxViewExtension?.SetPasswordRevealState(revealState);
			if (Host is { } host)
			{
				UpdateDisplayBlockText(host.Text);
			}
		}

		internal void UpdateTextFromNative(string newText)
		{
			if (Host is { } host)
			{
				var oldText = host.Text; // preexisting text
				var oldSelection = SelectionBeforeKeyDown; // On Gtk, SelectionBeforeKeyDown just points to Selection, which is updated by SetTextNative, so we need to read it before SetTextNative.
				var modifiedText = host.ProcessTextInput(newText); // new text after BeforeTextChanging, TextChanging, DP callback, etc
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

		internal void UpdateDisplayBlockText(string text)
		{
			// TODO: Inheritance hierarchy is wrong in Uno. PasswordBox shouldn't inherit TextBox.
			// This needs to be moved to PasswordBox if it's separated from TextBox.
			if (IsPasswordBox && !_isPasswordRevealed)
			{
				var passwordChar = GetPasswordChar();
				DisplayBlock.Text = new string(passwordChar, text.Length);
			}
			else
			{
				DisplayBlock.Text = text;
			}

			Host?.ContentElement?.InvalidateMeasure();
			Host?.UpdateLayout();
		}

		internal char GetPasswordChar()
		{
			if (TextBox is PasswordBox passwordBox && !string.IsNullOrEmpty(passwordBox.PasswordChar))
			{
				// Use the first character of the PasswordChar property
				return passwordBox.PasswordChar[0];
			}

			// Fallback to the platform-specific default
			return PasswordBox.DefaultPasswordChar[0];
		}

		internal void UpdateProperties() => _overlayTextBoxViewExtension?.UpdateProperties();

		internal void UpdatePasswordMasking()
		{
			// For Skia, we can update the display block text directly
			if (Host is { } host)
			{
				UpdateDisplayBlockText(host.Text);
			}
		}

		internal void SetTextAlignment()
		{
			if (Host is { } host)
			{
				DisplayBlock.TextAlignment = host.TextAlignment;
			}
		}
	}
}
