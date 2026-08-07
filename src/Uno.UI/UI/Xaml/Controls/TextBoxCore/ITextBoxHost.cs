#nullable enable

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
}
