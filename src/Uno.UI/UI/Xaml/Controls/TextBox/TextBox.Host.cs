#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls;

partial class TextBox : ITextBoxHost
{
	private readonly TextBoxCore _core;

	// For the framework internals that need the engine rather than the control.
	internal TextBoxCore Core => _core;

	Control ITextBoxHost.Owner => this;

	string ITextBoxHost.TextValue
	{
		get => Text;
		set => Text = value;
	}

	bool ITextBoxHost.IsReadOnly => IsReadOnly;

	bool ITextBoxHost.AcceptsReturn => AcceptsReturn;

	TextWrapping ITextBoxHost.TextWrapping => TextWrapping;

	bool ITextBoxHost.IsSpellCheckEnabled => IsSpellCheckEnabled;

	// Until PasswordBox is reparented (item 11) it still derives from TextBox, so the type test remains
	// the only truthful answer here. It becomes `=> false` on TextBox / `=> true` on PasswordBox then.
	bool ITextBoxHost.IsPassword => this is PasswordBox;

	int ITextBoxHost.MaxLength => MaxLength;

	InputScope ITextBoxHost.InputScope => InputScope;

	TextAlignment ITextBoxHost.TextAlignment => TextAlignment;

	CharacterCasing ITextBoxHost.CharacterCasing => CharacterCasing;

	bool ITextBoxHost.IsTextPredictionEnabled => IsTextPredictionEnabled;

	SolidColorBrush ITextBoxHost.SelectionHighlightColor => SelectionHighlightColor;

	object? ITextBoxHost.Header => Header;

	DataTemplate? ITextBoxHost.HeaderTemplate => HeaderTemplate;

	object? ITextBoxHost.Description => Description;

	InputReturnType ITextBoxHost.InputReturnType => TextBoxExtensions.GetInputReturnType(this);

	bool ITextBoxHost.IsButtonEnabled => _isButtonEnabled;

	void ITextBoxHost.UpdateButtonStates() => UpdateButtonStates();

	void ITextBoxHost.UpdateVisualState(bool useTransitions) => UpdateVisualState(useTransitions);

	bool ITextBoxHost.RaiseBeforeValueChanging(string newValue)
	{
		var args = new TextBoxBeforeTextChangingEventArgs(newValue);
		BeforeTextChanging?.Invoke(this, args);
		return args.Cancel;
	}

	void ITextBoxHost.RaiseValueChanging() => TextChanging?.Invoke(this, new TextBoxTextChangingEventArgs());

	void ITextBoxHost.RaiseValueChanged(bool isUserModifyingText, bool hasPendingChanges)
		=> TextChanged?.Invoke(this, new TextChangedEventArgs(this, isUserModifyingText, hasPendingChanges));

	bool ITextBoxHost.RaiseSelectionChanging(int start, int length)
	{
		var args = new TextBoxSelectionChangingEventArgs(start, length);
		SelectionChanging?.Invoke(this, args);
		return !args.Cancel || args.SelectionStart + args.SelectionLength > Text.Length;
	}

	void ITextBoxHost.RaiseSelectionChanged() => OnSelectionChanged();

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

	void ITextBoxHost.RaiseValueAutomationEvents(string? oldValue, string? newValue)
	{
		// Notify automation peers of the text value change.
		// WinUI CTextBox::UpdateTextProperty fires both ValueProperty changed
		// and TextPatternOnTextChanged events inline when text changes.
		var peer = GetOrCreateAutomationPeer();
		if (peer is TextBoxAutomationPeer textPeer)
		{
			if (AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged))
			{
				textPeer.RaiseValuePropertyChangedEvent(
					oldValue ?? string.Empty,
					newValue ?? string.Empty);
			}

			if (AutomationPeer.ListenerExistsHelper(AutomationEvents.TextPatternOnTextChanged))
			{
				textPeer.RaiseAutomationEvent(AutomationEvents.TextPatternOnTextChanged);
			}
		}
	}

	void ITextBoxHost.UpdateValueBindingSourceOnValueChanged()
	{
		var focusManager = VisualTree.GetFocusManagerForElement(this);
		if (focusManager?.FocusedElement != this &&
			GetBindingExpression(TextProperty) is
			{
				ParentBinding:
				{
					IsXBind: false, // NOTE: we UpdateSource in OnTextChanged only when the binding is not an x:Bind. WinUI's generated code for x:Bind contains a simple LostFocus subscription and waits for the next LostFocus even when not focused, unlike regular Bindings.
					UpdateSourceTrigger: UpdateSourceTrigger.Default or UpdateSourceTrigger.LostFocus
				}
			} bindingExpression)
		{
			bindingExpression.UpdateSource(Text);
		}
	}

	void ITextBoxHost.UpdateValueBindingSourceOnLostFocus()
	{
		if (GetBindingExpression(TextProperty) is { ParentBinding.UpdateSourceTrigger: UpdateSourceTrigger.LostFocus or UpdateSourceTrigger.Default } bindingExpression)
		{
			// Manually update Source when losing focus because TextProperty's default UpdateSourceTrigger is Explicit
			bindingExpression.UpdateSource(Text);
		}
	}

#if !IS_UNIT_TESTS
	// Resolves to the internal RaisePaste(TextControlPasteEventArgs) — explicit interface members are not
	// reachable by simple name, so this does not recurse.
	void ITextBoxHost.RaisePaste(TextControlPasteEventArgs args) => RaisePaste(args);
#endif
}
