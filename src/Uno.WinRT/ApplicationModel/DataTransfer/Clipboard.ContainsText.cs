#if !IS_UNIT_TESTS

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		/// <summary>
		/// Whether the clipboard holds text, answered from the clipboard's description where the
		/// platform can tell without reading it. Reading is observable to the user -- Android raises
		/// a "pasted from your clipboard" notice -- so callers that only need to decide whether a
		/// Paste affordance is enabled must ask this rather than <see cref="GetContent"/>.
		/// </summary>
		/// <remarks>
		/// Compiled for every variant, unlike the rest of the clipboard: Uno.UI builds once and calls
		/// this on all of them, so a variant missing it fails at runtime rather than at compile time.
		/// </remarks>
		internal static bool ContainsText()
		{
			bool? containsText = null;
			TryGetContainsText(ref containsText);

			return containsText ?? GetContent()?.Contains(StandardDataFormats.Text) ?? false;
		}

		/// <summary>
		/// Set by platforms that can answer <see cref="ContainsText"/> without reading the clipboard.
		/// Left untouched elsewhere, which falls back to reading it.
		/// </summary>
		static partial void TryGetContainsText(ref bool? containsText);
	}
}
#endif
