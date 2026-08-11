#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Contract through which <see cref="TextBoxCore"/> reaches the control hosting it, so the shared
/// text-input implementation can serve both <see cref="TextBox"/> and <see cref="PasswordBox"/>.
/// </summary>
/// <remarks>
/// <para>
/// Both controls derive directly from <see cref="Control"/> and share the implementation by
/// composition rather than through a common base class:
/// </para>
/// <code>
/// Control
///  ├── TextBox      : Control, ITextBoxHost  ─┐  owns a TextBoxCore
///  └── PasswordBox  : Control, ITextBoxHost  ─┤
///                                             │
///                     internal sealed TextBoxCore
///                     selection · caret · undo history · IME · pointers · grippers
///                     clipboard · selection flyout · proofing menu · TextBoxView
/// </code>
/// <para>
/// WinUI's own implementation does use inheritance — <c>CPasswordBox : CTextBoxBase</c>, registered
/// as <c>Microsoft.UI.Xaml.Internal.TextBoxBase</c> — but its projected surface does not:
/// <c>runtimeclass PasswordBox : Control</c>. C++/WinRT keeps the implementation hierarchy separate
/// from the projected one; C# has only one hierarchy, so the two goals conflict. Composition is the
/// one that preserves what app developers observe: <c>PasswordBox</c>'s base stays
/// <see cref="Control"/>, <see cref="TextBox"/>'s base is untouched, and nothing new becomes public.
/// An intermediate base class would also have to be <c>public</c> — CS0060 rejects a public type
/// deriving from an internal one.
/// </para>
/// <para>
/// The core never reads a derived control's dependency property directly; it asks the host, mirroring
/// <c>CTextBoxBase</c>'s <c>Tx*</c>/<c>Is*</c> virtuals.
/// </para>
/// </remarks>
internal interface ITextBoxHost
{
	/// <summary>
	/// The hosting control itself, for the framework APIs the core cannot inherit — visual states,
	/// template children, routed-event handlers, the dispatcher.
	/// </summary>
	Control Owner { get; }

	/// <summary>
	/// The edited text: <see cref="TextBox.Text"/> for a text box, <c>PasswordBox.Password</c> for a
	/// password box. The core works exclusively through this, so the cleartext password is never
	/// reachable through a <see cref="TextBox"/>-shaped API.
	/// </summary>
	string TextValue { get; set; }

	// Input-mode state the core reads but does not own. A password box answers these with fixed values
	// rather than dependency properties, which is why they are asked of the host instead of read directly.
	bool IsReadOnly { get; }

	bool AcceptsReturn { get; }

	TextWrapping TextWrapping { get; }

	bool IsSpellCheckEnabled { get; }

	/// <summary>
	/// Whether the hosted value is a password. Replaces the <c>this is PasswordBox</c> tests the shared
	/// implementation used to carry, and is what suppresses clipboard access and IME composition.
	/// </summary>
	bool IsPassword { get; }

	bool IsPointerOver { get; }

	int MaxLength { get; }

	InputScope InputScope { get; }

	TextAlignment TextAlignment { get; }

	CharacterCasing CharacterCasing { get; }

	bool IsTextPredictionEnabled { get; }

	SolidColorBrush SelectionHighlightColor { get; }

	object? Header { get; }

	DataTemplate? HeaderTemplate { get; }

	object? Description { get; }

	FlyoutBase? SelectionFlyout { get; }

#if __SKIA__
	/// <summary>
	/// Written by the core as the clipboard and read-only state change; the control owns the dependency
	/// property because the core is not a <see cref="DependencyObject"/> and has no <c>SetValue</c>.
	/// </summary>
	bool CanPasteClipboardContent { get; set; }
#endif

	InputReturnType InputReturnType { get; }

	/// <summary>
	/// Whether the control's chrome button may be enabled at all. The two controls disagree on what the
	/// button is — a delete button for <see cref="TextBox"/>, a reveal button for a password box — so this
	/// stays control-owned rather than moving into the core.
	/// </summary>
	bool IsButtonEnabled { get; }

	// Routed back through the control so a subclass override is still honoured; both are overridable API.
	void UpdateButtonStates();

	void UpdateVisualState(bool useTransitions = true);

	// Change notifications. The core owns the reentrancy guards and calls these from inside them, so each
	// one must be a single delegated invoke — nothing here may re-enter the core.
	//
	// Only primitives cross this boundary: TextBoxTextChangingEventArgs, TextBoxBeforeTextChangingEventArgs
	// and PasswordBoxPasswordChangingEventArgs are all sealed with no common base, so each control builds
	// its own. TextControlPasteEventArgs is the one type both controls genuinely share.

	/// <summary>
	/// Raises the cancellable pre-change hook and reports whether a handler vetoed the change.
	/// <see cref="TextBox"/> raises <see cref="TextBox.BeforeTextChanging"/>; a password box has no
	/// equivalent in WinUI and always returns <c>false</c> — which is what keeps the cleartext password
	/// from ever reaching an app handler.
	/// </summary>
	bool RaiseBeforeValueChanging(string newValue);

	/// <summary>
	/// Raises the non-cancellable pre-change notification: <c>TextChanging</c> or <c>PasswordChanging</c>.
	/// Takes no payload because <c>IsContentChanging</c> is always <c>true</c>.
	/// </summary>
	void RaiseValueChanging();

	/// <summary>
	/// Raises the asynchronous post-change notification: <c>TextChanged</c> or <c>PasswordChanged</c>.
	/// A password box ignores both flags, since <c>PasswordChanged</c> carries only <c>RoutedEventArgs</c>.
	/// </summary>
	void RaiseValueChanged(bool isUserModifyingText, bool hasPendingChanges);

	/// <summary>
	/// Raises the cancellable pre-selection-change hook and reports whether the change should proceed.
	/// WinUI exposes this on <see cref="TextBox"/> only, so a password box always proceeds.
	/// </summary>
	bool RaiseSelectionChanging(int start, int length);

	void RaiseSelectionChanged();

	/// <summary>
	/// Raises the control's <c>ContextMenuOpening</c> event synchronously and reports whether a handler
	/// marked it handled. Takes coordinates rather than args because the two controls' event types differ.
	/// </summary>
	bool RaiseContextMenuOpening(double cursorLeft, double cursorTop);

	/// <summary>
	/// Pushes the current value to a two-way binding when focus is lost. The trigger check needs the
	/// control's dependency property and binding expression, so it cannot live in the core.
	/// </summary>
	void UpdateValueBindingSourceOnLostFocus();

	/// <summary>
	/// Raises the automation-peer notifications for a value change. Called from the middle of the core's
	/// change sequence — the engine steps around it are order-sensitive, so this must not be reordered.
	/// A password box raises the masked equivalents from its own peer.
	/// </summary>
	void RaiseValueAutomationEvents(string? oldValue, string? newValue);

	/// <summary>
	/// Pushes the current value to a two-way binding as the value changes. Distinct from
	/// <see cref="UpdateValueBindingSourceOnLostFocus"/>: this one additionally skips <c>x:Bind</c>.
	/// </summary>
	void UpdateValueBindingSourceOnValueChanged();

#if __SKIA__
	// IME composition. WinUI exposes these on TextBox only, and the core deliberately does not route IME
	// composition to a password box (see StartImeSession), so a password box implements them as no-ops.
	void RaiseTextCompositionStarted(TextCompositionStartedEventArgs args);

	void RaiseTextCompositionChanged(TextCompositionChangedEventArgs args);

	void RaiseTextCompositionEnded(TextCompositionEndedEventArgs args);
#endif

#if !IS_UNIT_TESTS
	void RaisePaste(TextControlPasteEventArgs args);
#endif
}
