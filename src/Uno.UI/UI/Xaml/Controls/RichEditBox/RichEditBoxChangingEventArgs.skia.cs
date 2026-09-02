#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	// Uno-specific functional implementations of the RichEditBox "changing" event args for Skia.
	// The parameterless internal constructor in the generated stubs is compiled out for Skia (see the
	// generated files), so these partials own the constructors and backing state.

	public partial class RichEditBoxTextChangingEventArgs
	{
		internal RichEditBoxTextChangingEventArgs(bool isContentChanging)
		{
			IsContentChanging = isContentChanging;
		}

		/// <summary>
		/// Gets a value that indicates whether the text content of the <see cref="RichEditBox"/> is
		/// changing.
		/// </summary>
		public bool IsContentChanging { get; }
	}

	public partial class RichEditBoxSelectionChangingEventArgs
	{
		internal RichEditBoxSelectionChangingEventArgs(int selectionStart, int selectionLength)
		{
			SelectionStart = selectionStart;
			SelectionLength = selectionLength;
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the selection change that raised this event
		/// should be cancelled. When set to <c>true</c> by a handler, the pending selection change is
		/// not applied.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>Gets the proposed starting position of the selection.</summary>
		public int SelectionStart { get; }

		/// <summary>Gets the proposed length of the selection.</summary>
		public int SelectionLength { get; }
	}
}
