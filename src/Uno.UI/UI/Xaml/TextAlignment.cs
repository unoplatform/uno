namespace Windows.UI.Xaml
{
	/// <summary>
	/// Specifies whether text is centered, left-aligned, or right-aligned.
	/// </summary>
	public enum TextAlignment
	{
		/// <summary>
		/// Text is centered within the container.
		/// </summary>
		Center = 0,
		/// <summary>
		/// Text is aligned to the left edge of the container.
		/// </summary>
		Left = 1,
		/// <summary>
		/// Text is aligned to the right edge of the container.
		/// </summary>
		Right = 2,
		/// <summary>
		/// Text is justified within the container.
		/// </summary>
		Justify = 3,
		/// <summary>
		/// Text alignment is inferred from the text content.
		/// </summary>
		DetectFromContent = 4,
	}
}