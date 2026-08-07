#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Shared text-input implementation owned by <see cref="TextBox"/> and <see cref="PasswordBox"/>.
/// See <see cref="ITextBoxHost"/> for why this is composed rather than inherited.
/// </summary>
internal sealed partial class TextBoxCore
{
	private readonly ITextBoxHost _host;

	internal TextBoxCore(ITextBoxHost host) => _host = host;

	/// <summary>
	/// Mirrors <c>CTextBoxBase::IsEmpty</c>, which WinUI leaves pure-virtual and each control answers
	/// from its own text property.
	/// </summary>
	internal bool IsEmpty => string.IsNullOrEmpty(_host.TextValue);
}
