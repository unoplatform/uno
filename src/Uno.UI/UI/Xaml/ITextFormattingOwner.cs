namespace Microsoft.UI.Xaml;

/// <summary>
/// Interface implemented by types that participate in the TextFormatting
/// inheritance system (UIElement and TextElement).
/// Used by the DependencyObjectStore to call <see cref="EnsureTextFormatting"/>
/// when reading text properties at DefaultValue precedence.
/// </summary>
internal interface ITextFormattingOwner
{
	/// <summary>
	/// Gets the TextFormatting storage for this element.
	/// May be <see langword="null"/> if not yet created.
	/// </summary>
	TextFormatting TextFormatting { get; }

	/// <summary>
	/// Ensures the TextFormatting storage exists and is up-to-date.
	/// Creates it if necessary, and calls <c>PullInheritedTextFormatting</c>
	/// if the cached values are stale and the property is at default.
	/// </summary>
	/// <param name="property">
	/// The specific text property being read, or <see langword="null"/>
	/// to check all properties.
	/// </param>
	/// <param name="forGetValue">
	/// <see langword="true"/> when called from a GetValue path
	/// (enables lazy pull if stale).
	/// </param>
	void EnsureTextFormatting(DependencyProperty property, bool forGetValue);
}
