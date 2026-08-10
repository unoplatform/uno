#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls;

partial class PasswordBox : ITextBoxHost
{
	private readonly TextBoxCore _core;

	// For the framework internals that need the engine rather than the control.
	internal TextBoxCore Core => _core;

	// Set only by this control's pointer/visibility overrides and read only for visual state.
	private bool _isPointerOver;

	Control ITextBoxHost.Owner => this;

	string ITextBoxHost.TextValue
	{
		get => Password;
		set => Password = value;
	}

	bool ITextBoxHost.IsPassword => true;

	// WinUI's PasswordBox exposes none of these; the fixed answers are what its IDL implies.
	bool ITextBoxHost.IsReadOnly => false;

	bool ITextBoxHost.AcceptsReturn => false;

	TextWrapping ITextBoxHost.TextWrapping => TextWrapping.NoWrap;

	bool ITextBoxHost.IsSpellCheckEnabled => false;

	bool ITextBoxHost.IsTextPredictionEnabled => false;

	CharacterCasing ITextBoxHost.CharacterCasing => CharacterCasing.Normal;

	TextAlignment ITextBoxHost.TextAlignment => TextAlignment.Left;

	bool ITextBoxHost.IsPointerOver => _isPointerOver;

	int ITextBoxHost.MaxLength => MaxLength;

	InputScope ITextBoxHost.InputScope => InputScope;

	SolidColorBrush ITextBoxHost.SelectionHighlightColor => SelectionHighlightColor;

	object? ITextBoxHost.Header => Header;

	DataTemplate? ITextBoxHost.HeaderTemplate => HeaderTemplate;

	object? ITextBoxHost.Description => Description;

	InputReturnType ITextBoxHost.InputReturnType => TextBoxExtensions.GetInputReturnType(this);

	// The reveal button, not a delete button — which is why this stays control-owned.
	bool ITextBoxHost.IsButtonEnabled => _isButtonEnabled;

	char ITextBoxHost.PasswordChar
		=> string.IsNullOrEmpty(PasswordChar) ? DefaultPasswordChar[0] : PasswordChar[0];

	void ITextBoxHost.UpdateButtonStates() => UpdateButtonStates();

	void ITextBoxHost.UpdateVisualState(bool useTransitions) => UpdateVisualState(useTransitions);

	// WinUI gives PasswordBox no cancellable pre-change hook, so nothing can veto password input and,
	// more to the point, no app handler ever sees the cleartext password.
	bool ITextBoxHost.RaiseBeforeValueChanging(string newValue) => false;

	// PasswordChanging is a [NotImplemented] stub (out of scope), so there is nothing to raise.
	void ITextBoxHost.RaiseValueChanging() { }

	// PasswordChanged carries only RoutedEventArgs — neither flag has an equivalent.
	void ITextBoxHost.RaiseValueChanged(bool isUserModifyingText, bool hasPendingChanges)
		=> PasswordChanged?.Invoke(this, new RoutedEventArgs(this));

	// WinUI exposes SelectionChanging/SelectionChanged on TextBox only, so a selection change always proceeds.
	bool ITextBoxHost.RaiseSelectionChanging(int start, int length) => true;

	void ITextBoxHost.RaiseSelectionChanged() { }

#if __SKIA__
	bool ITextBoxHost.RaiseContextMenuOpening(double cursorLeft, double cursorTop)
	{
		var args = new ContextMenuEventArgs(cursorLeft, cursorTop);
		ContextMenuOpening?.Invoke(this, args);
		return args.Handled;
	}
#else
	bool ITextBoxHost.RaiseContextMenuOpening(double cursorLeft, double cursorTop) => false;
#endif

	// Deliberately empty, and this is not a regression: the shared implementation only ever raised these
	// when the peer was a TextBoxAutomationPeer, and a password box's peer never was. PasswordBoxAutomationPeer
	// still reports the masked value on demand via IValueProvider; it just does not push a change event.
	// Notifying listeners of password-length changes would be new behaviour, so it is not done here.
	void ITextBoxHost.RaiseValueAutomationEvents(string? oldValue, string? newValue) { }

	void ITextBoxHost.UpdateValueBindingSourceOnValueChanged()
	{
		var focusManager = VisualTree.GetFocusManagerForElement(this);
		if (focusManager?.FocusedElement != this &&
			GetBindingExpression(PasswordProperty) is
			{
				ParentBinding:
				{
					IsXBind: false,
					UpdateSourceTrigger: UpdateSourceTrigger.Default or UpdateSourceTrigger.LostFocus
				}
			} bindingExpression)
		{
			bindingExpression.UpdateSource(Password);
		}
	}

	void ITextBoxHost.UpdateValueBindingSourceOnLostFocus()
	{
		if (GetBindingExpression(PasswordProperty) is { ParentBinding.UpdateSourceTrigger: UpdateSourceTrigger.LostFocus or UpdateSourceTrigger.Default } bindingExpression)
		{
			bindingExpression.UpdateSource(Password);
		}
	}

#if __SKIA__
	// The core never starts an IME session for a password box (see TextBoxCore.StartImeSession), and WinUI
	// exposes the composition events on TextBox only, so these are unreachable no-ops.
	void ITextBoxHost.RaiseTextCompositionStarted(TextCompositionStartedEventArgs args) { }

	void ITextBoxHost.RaiseTextCompositionChanged(TextCompositionChangedEventArgs args) { }

	void ITextBoxHost.RaiseTextCompositionEnded(TextCompositionEndedEventArgs args) { }
#endif

#if !IS_UNIT_TESTS
	void ITextBoxHost.RaisePaste(TextControlPasteEventArgs args) => RaisePaste(args);
#endif
}
